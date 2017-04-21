using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using RestRunner.Helpers;
using RestRunner.Models;

namespace RestRunner.Services
{
    public class ImportExportService
    {
        public async Task ExportToFile(string filePath,
            IEnumerable<RestCommand> commands,
            IEnumerable<RestCommandChain> chains,
            IEnumerable<RestEnvironment> environments,
            RestEnvironment globals,
            string description)
        {
            var output = new ImportExportFormat
            {
                Description = description,
                Commands = commands,
                Chains = chains,
                Environments = environments,
                Globals = globals
            };
            File.WriteAllText(filePath, JsonConvert.SerializeObject(output, Formatting.Indented));
        }

        public async Task<ImportedData> ImportFile(string filePath)
        {
            //read in the file text
            string jsonText;
            using (var reader = File.OpenText(filePath))
            {
                jsonText = await reader.ReadToEndAsync();
            }

            //de-serialize the json
            ImportExportFormat importedData;
            try
            {
                importedData = JsonConvert.DeserializeObject<ImportExportFormat>(jsonText);
            }
            catch (Exception ex)
            {
                return new ImportedData {Errors = new List<string> {ex.Message}};
            }
            
            //convert to the result format
            var result = new ImportedData
            {
                Description = importedData.Description,
                Commands = importedData.Commands.ToList(),
                Chains = importedData.Chains.ToList(),
                Environments = importedData.Environments.ToList(),
                Globals = importedData.Globals,
                Errors = new List<string>()
            };
            return result;
        }

        public async Task<ImportedData> ImportPostmanFile(string filePath)
        {
            var result = new ImportedData()
            {
                Errors = new List<string>()
            };

            //read in the file text, which should be plain json
            string jsonText;
            using (var reader = File.OpenText(filePath))
            {
                jsonText = await reader.ReadToEndAsync();
            }

            //determine the format of the file
            PostmanImportFileType fileType;
            if (filePath.EndsWith(".postman_dump.json"))
                fileType = PostmanImportFileType.DumpV1;
            else if (filePath.EndsWith(".postman_environment.json"))
                fileType = PostmanImportFileType.EnvironmentV1;
            else if (filePath.EndsWith(".postman_collection.json"))
                fileType = (jsonText.Contains(@"https://schema.getpostman.com/json/collection/v2.0.0/collection.json")) ? PostmanImportFileType.CollectionV2 : PostmanImportFileType.CollectionV1;
            else
            {
                result.Errors.Add("Invalid file format.  Must be either a Postman dump or collection file.");
                return result;
            }

            //import the file, based on its type
            try
            {
                if (fileType == PostmanImportFileType.CollectionV1)
                {
                    var importData = JsonConvert.DeserializeObject<PostmanRequestCollectionV1>(jsonText);
                    result.Commands = importData.ToRestCommands();
                }
                else if (fileType == PostmanImportFileType.DumpV1)
                {
                    var importData = JsonConvert.DeserializeObject<PostmanDumpFormatV1>(jsonText);

                    IEnumerable<RestCommand> commands = new List<RestCommand>(); //seed list
                    result.Commands = importData.Collections.Aggregate(commands, (current, collection) => current.Concat(collection.ToRestCommands())).ToList();

                    result.Environments = importData.Environments.Select(e => e.ToRestEnvironment()).ToList();

                    var globalVariables = new ObservableCollection<CaptionedKeyValuePair>(importData.Globals);
                    result.Globals = new RestEnvironment("[Globals]", null, globalVariables);
                }
                else if (fileType == PostmanImportFileType.EnvironmentV1)
                {
                    var importData = JsonConvert.DeserializeObject<PostmanEnvironmentV1>(jsonText);
                    result.Environments = new List<RestEnvironment>() { importData.ToRestEnvironment() };
                }
                else
                    throw new NotImplementedException($"The {fileType} file type is not supported.");
            }
            catch (Exception)
            {
                result.Errors.Add("Invalid file format.  Must be either a Postman dump or collection file."); //if neither format loaded properly, then add an error
            }

            return result;
        }
    }

    public class ImportedData
    {
        public string Description { get; set; }
        public IList<RestCommand> Commands { get; set; }
        public IList<RestCommandChain> Chains { get; set; }
        public IList<RestEnvironment> Environments { get; set; }
        public RestEnvironment Globals { get; set; }
        public List<string> Errors { get; set; }
    }

    [Serializable]
    class ImportExportFormat
    {
        public int FormatVersion { get; set; } = 1;
        public string Description { get; set; }
        public IEnumerable<RestCommand> Commands { get; set; }
        public IEnumerable<RestCommandChain> Chains { get; set; }
        public IEnumerable<RestEnvironment> Environments { get; set; }
        public RestEnvironment Globals { get; set; }
    }

    #region Postman Import

    enum PostmanImportFileType
    {
        CollectionV1,
        CollectionV2,
        DumpV1,
        EnvironmentV1
    }

    public class PostmanRequestDataConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.StartArray)
            {
                var dataArray = (string[])serializer.Deserialize(reader, typeof(string[]));
                if (dataArray.Length > 0)
                    throw new NotImplementedException("The data property does not support arrays with data");

                return "";
            }
            else if (reader.TokenType == JsonToken.String)
            {
                return serializer.Deserialize(reader, objectType);
            }
            else if (reader.TokenType == JsonToken.Null)
            {
                return "";
            }
            else
            {
                throw new NotImplementedException($"The JsonToken type of {reader.TokenType} is not implemented for import");
            }
        }

        public override bool CanConvert(Type objectType) => true;
    }

    class PostmanRequestCollectionV1
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public bool HasRequests { get; set; }
        public IEnumerable<PostmanRequestV1> Requests { get; set; }

        public IList<RestCommand> ToRestCommands()
        {
            var result = new List<RestCommand>();
            var category = new RestCommandCategory(Name, "", null, null, Id);

            if (!Requests.Any())
                return result;

            result.AddRange(Requests.Select(req => req.ToRestCommand(category)));

            ConvertAuthHeaders(result, category);

            return result;
        }
        /// <summary>
        /// Convert any Authorization headers into username/passwords for the command.  If any one username/password is used
        /// for at least half of the commands, then make that the category default, and update the individual commands to
        /// reflect that.
        /// </summary>
        /// <param name="commands"></param>
        /// <param name="category"></param>
        private void ConvertAuthHeaders(IList<RestCommand> commands, RestCommandCategory category)
        {
            //get a list of all authorization headers, and how many times they were used
            var authHeaders = commands.Aggregate(new Dictionary<string, int>(), (foundHeaders, curCommand) =>
            {
                var authHeader = curCommand.Headers?.SingleOrDefault(h => h.Key == "Authorization")?.Value;
                if (authHeader != null)
                    foundHeaders[authHeader] = foundHeaders.ContainsKey(authHeader) ? foundHeaders[authHeader] + 1 : 1;

                return foundHeaders;
            });

            //find the auth header that was used the most, and if it was used at least half of the time, then use it as the category default
            int maxHeaderCount = authHeaders.Count > 0 ? authHeaders.Values.Max() : 0;
            bool setCategoryDefault = maxHeaderCount > (commands.Count / 2);

            //set the category credentials to the common auth if needed
            string categoryAuthHeaderValue = null;
            RestCredential categoryAuthCredential = null;
            if (setCategoryDefault)
            {
                categoryAuthHeaderValue = authHeaders.First(a => a.Value == maxHeaderCount).Key;
                categoryAuthCredential = BasicAuthConverter.BasicAuthToCredential(categoryAuthHeaderValue);
                if (categoryAuthCredential != null)
                {
                    category.Username = categoryAuthCredential.Username;
                    category.Password = categoryAuthCredential.Password;
                }
            }

            //update any command with different credentials than the new category default
            foreach (var command in commands)
            {
                //try to find an auth header
                var curAuthHeader = command.Headers.FirstOrDefault(h => h.Key == "Authorization" && h.Value.StartsWith("Basic "));
                var curAuthHeaderValue = curAuthHeader?.Value;

                //if there was no auth header, but a category default was set, explicitly state not to use credentials
                if (curAuthHeaderValue == null)
                {
                    if (setCategoryDefault)
                        command.CredentialName = "[No Credentials]";
                }
                //if there was an auth header
                else
                {
                    //set the username/password if it differs from the category default
                    if (curAuthHeaderValue != categoryAuthHeaderValue)
                    {
                        var curCredential = BasicAuthConverter.BasicAuthToCredential(curAuthHeaderValue);
                        command.Username = curCredential.Username;
                        command.Password = curCredential.Password;
                    }

                    //remove the authorization header, as you set the username/password seprately
                    command.Headers.Remove(curAuthHeader);
                }
            }
        }
    }

    class PostmanRequestV1
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public Guid CollectionId { get; set; }
        public string Description { get; set; }
        public string Url { get; set; }
        public string Method { get; set; }
        public string Headers { get; set; }
        [JsonConverter(typeof(PostmanRequestDataConverter))]
        public string Data { get; set; }

        public string RawModeData { get; set; }

        public RestCommand ToRestCommand(RestCommandCategory category)
        {
            var headerList = Headers.Split('\n');
            ObservableCollection<CaptionedKeyValuePair> headers = new ObservableCollection<CaptionedKeyValuePair>();
            foreach (var h in headerList.Where(h => !string.IsNullOrEmpty(h)))
            {
                var key = h.Substring(0, h.IndexOf(':'));
                var value = h.Substring(h.IndexOf(':') + 1).Trim();
                headers.Add(new CaptionedKeyValuePair(key, value));
            }

            //get the data from the proper property (RawModeData is a newer property, as Data used to hold both params data (which is an array) and raw data (which is a string))
            var data = "";
            if (!string.IsNullOrWhiteSpace(RawModeData))
                data = RawModeData;
            else if (!string.IsNullOrWhiteSpace(Data))
                data = Data;

            var result = new RestCommand(Url, data, (RestSharp.Method) Enum.Parse(typeof (RestSharp.Method), Method), Id)
            {
                Label = Name,
                Category = category,
                Description = Description,
                Headers = headers
            };
            return result;
        }
    }

    class PostmanEnvironmentV1
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public IList<KeyValuePair<string, string>> Values { get; set; }
        public bool IsDeleted { get; set; }

        public RestEnvironment ToRestEnvironment()
        {
            var variables = Values.Select(v => new CaptionedKeyValuePair(v.Key, v.Value));
            return new RestEnvironment(Name, null, new ObservableCollection<CaptionedKeyValuePair>(variables));
        }
    }

    class PostmanDumpFormatV1
    {
        public int Version { get; set; }
        public IEnumerable<PostmanRequestCollectionV1> Collections { get; set; }
        public IEnumerable<PostmanEnvironmentV1> Environments { get; set; }
        public IEnumerable<CaptionedKeyValuePair> Globals { get; set; }
    }

    #endregion Postman Import
}
