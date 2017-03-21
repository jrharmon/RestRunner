using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;
using System.Security.Policy;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestRunner.Helpers;
using RestRunner.Properties;
using RestRunner.ViewModels;
using RestSharp;

namespace RestRunner.Models
{
    class RestCommandException : Exception
    {
        public RestCommandException(string message)
            : base(message)
        {
            
        }
    }

    /// <summary>
    /// Information needed to create/send a request to a REST end-point, and retrieve the result.
    /// </summary>
    [Serializable]
    [DebuggerDisplay("ID: {Id}, Label: {Label}")]
    public class RestCommand : ObservableBase, ISerializable, IEquatable<RestCommand>
    {
        public static DateTime LastExecution { get; private set; } = DateTime.MinValue; //when starting the app, default so that any warning delays will be hit

        public RestCommand(string resourceUrl, string body, Method verb, Guid? id = null)
        {
            Id = id ?? Guid.NewGuid();
            Body = body;
            CaptureValues = new ObservableCollection<CaptionedKeyValuePair>();
            Category = RestCommandCategory.DefaultCategory;
            Headers = new ObservableCollection<CaptionedKeyValuePair>();
            Parameters = new ObservableCollection<RestParameter>();
            ResourceUrl = resourceUrl;
            Verb = verb;

            SetupDefaultIsOpens();
        }

        #region Properties

        public Guid Id { get; private set; }

        private string _body;
        public string Body
        {
            get { return _body; }
            set { Set(ref _body, value); }
        }

        /// <summary>
        /// A list of properties to capture the values of, and the actual property path (ex. {"ResponseResult", "ResponseData.ResponseResult"})
        /// </summary>
        private ObservableCollection<CaptionedKeyValuePair> _captureValues;
        public ObservableCollection<CaptionedKeyValuePair> CaptureValues
        {
            get { return _captureValues; }
            set { Set(ref _captureValues, value); }
        }

        private RestCommandCategory _category;
        public RestCommandCategory Category
        {
            get { return _category; }
            set
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(Category));

                Set(ref _category, value);
            }
        }

        /// <summary>
        /// The name of the credential to use in the current environment.  If the currency environment does
        /// not contain a credential with this name, then it will be ignored.  If it does contain a credential
        /// with this name, then any username/password will be ignored.  If set, this overrides the category
        /// credential.
        /// </summary>
        private string _credentialName;
        public string CredentialName
        {
            get { return _credentialName; }
            set { Set(ref _credentialName, value); }
        }

        private string _description;
        public string Description
        {
            get { return _description; }
            set { Set(ref _description, value); }
        }

        public bool HasAuthorization => !string.IsNullOrEmpty(CredentialName) || !string.IsNullOrEmpty(Username) || !string.IsNullOrEmpty(Password);

        private ObservableCollection<CaptionedKeyValuePair> _headers;
        public ObservableCollection<CaptionedKeyValuePair> Headers
        {
            get { return _headers; }
            set { Set(ref _headers, value); }
        }

        private bool _isCaptureValuesOpen;
        public bool IsCaptureValuesOpen
        {
            get { return _isCaptureValuesOpen; }
            set { Set(ref _isCaptureValuesOpen, value); }
        }

        private bool _isHeadersOpen;
        public bool IsHeadersOpen
        {
            get { return _isHeadersOpen; }
            set { Set(ref _isHeadersOpen, value); }
        }

        private bool _isParametersOpen;
        public bool IsParametersOpen
        {
            get { return _isParametersOpen; }
            set { Set(ref _isParametersOpen, value); }
        }

        private bool _isSettingsOpen;
        public bool IsSettingsOpen
        {
            get { return _isSettingsOpen; }
            set { Set(ref _isSettingsOpen, value); }
        }

        private string _label;
        public string Label
        {
            get { return _label; }
            set { Set(ref _label, value); }
        }

        private ObservableCollection<RestParameter> _parameters;
        public ObservableCollection<RestParameter> Parameters
        {
            get { return _parameters; }
            set { Set(ref _parameters, value); }
        }

        private string _password;
        public string Password
        {
            get { return _password; }
            set { Set(ref _password, value); }
        }

        private string _resourceUrl;
        public string ResourceUrl
        {
            get { return _resourceUrl; }
            set { Set(ref _resourceUrl, value); }
        }

        private string _username;
        public string Username
        {
            get { return _username; }
            set { Set(ref _username, value); }
        }

        private Method _verb;
        public Method Verb
        {
            get { return _verb; }
            set { Set(ref _verb, value); }
        }

        #endregion Properties

        #region Public Methods

        /// <summary>
        /// Create a completely new object, that is a copy of this one.
        /// </summary>
        /// <returns></returns>
        public RestCommand DeepCopy()
        {
            IFormatter formatter = new BinaryFormatter();
            using (MemoryStream s = new MemoryStream())
            {
                formatter.Serialize(s, this);
                s.Position = 0;
                RestCommand result = (RestCommand)formatter.Deserialize(s);
                result.Id = Guid.NewGuid();
                result.Category = Category;
                return result;
            }
        }

        public bool Equals(RestCommand other)
        {
            if (ReferenceEquals(null, other)) return false;

            return
                string.Equals(_body, other._body) &&
                _captureValues.SequenceEqual(other._captureValues) &&
                Equals(_category, other._category) &&
                string.Equals(_credentialName, other._credentialName) &&
                string.Equals(_description, other._description) &&
                _headers.SequenceEqual(other._headers) &&
                string.Equals(_label, other._label) &&
                _parameters.SequenceEqual(other._parameters) &&
                string.Equals(_password, other._password) &&
                string.Equals(_resourceUrl, other._resourceUrl) &&
                string.Equals(_username, other._username) &&
                _verb == other._verb;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj.GetType() == GetType() && Equals(obj as RestCommand);
        }

        public async Task<RestResult> Execute(Dictionary<string, string> runtimeParameterValues, CancellationToken cancelToken, RestEnvironment environment = null, RestEnvironment globalEnvironment = null)
        {
            var paramValues = runtimeParameterValues.Select(p => new CaptionedKeyValuePair(p.Key, p.Value));
            return await Execute(paramValues, cancelToken, environment, globalEnvironment);
        }

        public async Task<RestResult> Execute(IEnumerable<CaptionedKeyValuePair> runtimeParameterValues, CancellationToken cancelToken, RestEnvironment environment = null, RestEnvironment globalEnvironment = null)
        {
            LastExecution = DateTime.Now;

            //define any variables that would be needed in a catch block
            string requestBody = "";
            IRestClient client = null;
            IRestResponse response = null;
            RestRequest request = null;
            RestRequestAsyncHandle requestHandle = null;
            var inputHeaders = new StringBuilder();
            var duration = TimeSpan.Zero;
            var missingParameters = new List<string>();
            var capturedValues = new Dictionary<string, string>();

            try
            {
                //setup the actual parameter values for this run, and update the most-recently-used list for Command's parameters
                var paramValues = SetupParameterValues(globalEnvironment, environment, runtimeParameterValues, capturedValues);
                foreach (var param in Parameters.Where(p => p.IsPresetValuesUpdatedOnExecution))
                    param.UpdatePresetValues();

                //setup the base and resource URLs (since baseUrl can't be empty, if it comes in that way, then just split up resourceUrl in two)
                string resourceUrl;
                client = CreateClient(out resourceUrl, paramValues, missingParameters);

                //setup authentication
                client.Authenticator = CreateBasicAuthenticator(globalEnvironment, environment, paramValues, missingParameters);

                //create the request
                request = new RestRequest(resourceUrl, Verb);

                //set the body
                requestBody = ReplaceVariables(Body, paramValues, missingParameters);
                var contentType = Headers.ContainsKey("Content-Type")
                    ? Headers.First(h => h.Key == "Content-Type").Value
                    : "application/json";
                request.AddParameter(contentType, requestBody, ParameterType.RequestBody);

                //add HTTP Headers
                foreach (var header in Headers)
                {
                    request.AddHeader(ReplaceVariables(header.Key, paramValues, missingParameters),
                        ReplaceVariables(header.Value, paramValues, missingParameters));
                }

                // execute the request
                var startTime = DateTime.Now;
                var tcs = new TaskCompletionSource<IRestResponse>();
                requestHandle = client.ExecuteAsync(request, r => tcs.TrySetResult(r));
                cancelToken.Register(() => requestHandle.Abort());
                response = await tcs.Task;
                duration = DateTime.Now - startTime;

                //handle any errors
                if (response == null)
                    throw new RestCommandException("[REST Runner]: Response was null");
                if (response.ResponseStatus == ResponseStatus.Aborted)
                    throw new RestCommandException($"The command '{Label}' was aborted");
                if (response.ErrorException?.InnerException?.Message?.Contains("127.0.0.1:8888") ?? false)
                    throw new RestCommandException(
                        $"{response.ErrorMessage}{Environment.NewLine}It looks like Fiddler was closed.  Try opening it again.");
                if (response.ResponseStatus == ResponseStatus.Error)
                    throw new RestCommandException(response.ErrorMessage);

                //create the result object, once you know resultString is valid
                string resultString = response.Content;

                if (response.ContentType.Contains("json")) //some types may not say "application/json" exactly, such as "application/hal+json"
                {
                    resultString = response.Content.FormatJson();
                    CaptureValuesFromJson(resultString, capturedValues); //grab any capture values
                }

                var succeeded = (int) response.StatusCode < 300;
                ReadInputHeader(requestHandle?.WebRequest, inputHeaders);
                return new RestResult(requestBody, resultString, capturedValues, duration, response, inputHeaders.ToString(), succeeded, this);
            }
            catch (JsonReaderException ex)
            {
                var responseBody = $"{ex.Message}{Environment.NewLine}{response.Content}";
                ReadInputHeader(requestHandle?.WebRequest, inputHeaders);
                return new RestResult(requestBody, responseBody, new Dictionary<string, string>(), duration, response, inputHeaders.ToString(), false, this);
            }
            catch (Exception ex)
            {
                var responseBody = ex.Message;

                if (response?.ContentType?.Contains("application/json") ?? false)
                    responseBody = responseBody.FormatJson();

                if (missingParameters.Count > 0)
                    responseBody += $"{Environment.NewLine}{Environment.NewLine}[REST Runner]: The following parameters were missing when making this request: {string.Join(", ", missingParameters.Distinct())}";

                if (requestHandle?.WebRequest != null)
                    ReadInputHeader(requestHandle?.WebRequest, inputHeaders);
                else if (request != null)
                    inputHeaders.AppendLine($"{request.Method} {client?.BaseUrl}{request.Resource}"); //if you can't read in the real input headers, at least add the URL and method, to see what was attempted
                return new RestResult(requestBody, responseBody, new Dictionary<string, string>(), duration, response, inputHeaders.ToString(), false, this);
            }
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }

        #endregion Public Methods

        #region Private Methods

        private void CaptureValuesFromJson(string jsonText, Dictionary<string, string> capturedValues)
        {
            try
            {
                //grab any capture values
                var resultObject = JsonConvert.DeserializeObject<dynamic>(jsonText);
                foreach (var propertyPath in CaptureValues)
                {
                    var pathSegments = propertyPath.Value.Split('.');
                    dynamic curProperty = resultObject;
                    foreach (string segment in pathSegments)
                    {
                        if (curProperty == null)
                            break;

                        bool isList = curProperty.GetType().Name == "JArray";
                        curProperty = isList ? curProperty[int.Parse(segment)] : curProperty[segment];
                    }

                    capturedValues[propertyPath.Key] = curProperty != null ? curProperty.ToString() : null;
                }
            }
            catch (Exception)
            {
                //if the text is not valid json, then just ignore it
            }
        }

        /// <summary>
        /// Setup authentication, if needed (only use the username/password if there is no credential name)
        /// </summary>
        /// <returns></returns>
        private HttpBasicAuthenticator CreateBasicAuthenticator(RestEnvironment globalEnvironment, RestEnvironment environment, Dictionary<string, string> parameterValues, List<string> missingParameters = null)
        {
            //determine whether to use this commands credentials, or the ones from the category
            var useCommandAuth = HasAuthorization;

            //get the credential name, and default username/password
            var credentialName = useCommandAuth ? CredentialName : Category.CredentialName;
            var username = useCommandAuth ? Username : Category.Username;
            var password = useCommandAuth ? Password : Category.Password;

            //if the credential name is set, and is present in the environment (or the global one), then use it instead of the username/password fields
            if (!string.IsNullOrEmpty(credentialName))
            {
                if ((environment?.Credentials.Any(c => c.Name == credentialName) ?? false))
                {
                    var credential = environment.Credentials.First(c => c.Name == credentialName);
                    username = credential.Username;
                    password = credential.Password;
                }
                else if ((globalEnvironment?.Credentials.Any(c => c.Name == credentialName) ?? false))
                {
                    var credential = globalEnvironment.Credentials.First(c => c.Name == credentialName);
                    username = credential.Username;
                    password = credential.Password;
                }
            }

            //if there is no username/password data, then don't create the authenticator
            if ((string.IsNullOrEmpty(username)) && (string.IsNullOrEmpty(password)))
                return null;

            return new HttpBasicAuthenticator(
                    ReplaceVariables(username, parameterValues, missingParameters),
                    ReplaceVariables(password, parameterValues, missingParameters));
        }

        private IRestClient CreateClient(out string resourceUrl, Dictionary<string, string> parameterValues, List<string> missingParameters = null)
        {
            //setup the base and resource URLs (since baseUrl can't be empty, if it comes in that way, then just split up resourceUrl in two)
            var baseUrl = ReplaceVariables(Category.BaseUrl, parameterValues, missingParameters);
            resourceUrl = ReplaceVariables(ResourceUrl, parameterValues, missingParameters);
            resourceUrl = Uri.EscapeUriString(resourceUrl);

            try
            {
                if ((string.IsNullOrEmpty(baseUrl)) || (resourceUrl.StartsWith("http://")) || (resourceUrl.StartsWith("https://")))
                {
                    if ((resourceUrl.Length > 8) && (resourceUrl.IndexOf('/', 8) >= 0))
                    {
                        baseUrl = resourceUrl.Substring(0, resourceUrl.IndexOf('/', 8));
                        resourceUrl = resourceUrl.Substring(resourceUrl.IndexOf('/', 8) + 1);
                    }
                    else
                    {
                        baseUrl = resourceUrl;
                        resourceUrl = "";
                    }
                }
                if (Settings.Default.ReplaceLocalHostWithMachine)
                {
                    if (baseUrl.Contains("://localhost"))
                        baseUrl = baseUrl.Replace("://localhost", "://" + Environment.MachineName);
                    if (baseUrl.Contains("://127.0.0.1"))
                        baseUrl = baseUrl.Replace("://127.0.0.1", "://" + Environment.MachineName);
                }

                var client = new RestClient(baseUrl);
                client.UserAgent = "RestRunner/" + Application.ProductVersion;

                return client;
            }
            catch (Exception)
            {
                var url = baseUrl + (string.IsNullOrWhiteSpace(resourceUrl) ? "" : resourceUrl);
                throw new Exception("[REST Runner]: Request could not be attempted.  Invalid URL: " + url);
            }
        }

        //set the input headers display info for the RestResult
        private void ReadInputHeader(HttpWebRequest webRequest, StringBuilder inputHeaders)
        {
            if (webRequest == null)
                return;

            inputHeaders.AppendLine($"{webRequest.Method} {webRequest.Address.AbsoluteUri} HTTP/{webRequest.ProtocolVersion.Major}.{webRequest.ProtocolVersion.Minor}");
            foreach (string headerName in webRequest.Headers.Keys)
            {
                var headerDisplayName = headerName == "Proxy-Connection" ? "Connection" : headerName;
                inputHeaders.AppendLine($"{headerDisplayName}: {webRequest.Headers.Get(headerName)}");
            }
        }

        /// <summary>
        /// Update the text, with all variables replaced with their actual value text (the actual text sent when running the command)
        /// </summary>
        /// <param name="text">The text that will have any parameters replaced</param>
        /// <param name="parameterValues">The values to set an parameters to.  The key is the parameter name, and the value is the parameter value</param>
        /// <param name="missingParameters">If any parameters present in 'text' do not have a value in 'parameterValues', the parameter name will be added here.</param>
        /// <returns></returns>
        private string ReplaceVariables(string text, Dictionary<string, string> parameterValues, List<string> missingParameters = null)
        {
            if (text == null)
                return "";

            if (parameterValues.Count == 0)
                return text;

            string result = parameterValues.Aggregate(text, (current, parameter) => current.Replace($"%{parameter.Key}%", parameter.Value));

            //keep track of any parameters that did not have a value
            if (missingParameters != null)
            {
                var regex = new Regex(@"%(.*?)%");
                missingParameters.AddRange(from Match match in regex.Matches(result) select match.Value.Replace("%", ""));
                missingParameters.AddRange(parameterValues.Where(p => string.IsNullOrEmpty(p.Value)).Select(p => p.Key));
            }

            //handle generators
            result = GeneratorProcessor.Process(result);
            
            return result;
        }

        //set the defaul expanded status of these sections for the view to display
        private void SetupDefaultIsOpens()
        {
            IsCaptureValuesOpen = CaptureValues.Count > 0;
            IsHeadersOpen = Headers.Count > 0;
            IsParametersOpen = Parameters.Count > 0;
            IsSettingsOpen = string.IsNullOrWhiteSpace(ResourceUrl);
        }

        /// <summary>
        /// Setup the actual parameter values for this run, based off of the environment settings values, runtime values, and local parameter values if they are set
        /// </summary>
        private Dictionary<string, string> SetupParameterValues(RestEnvironment globalEnvironment, RestEnvironment environment, IEnumerable<CaptionedKeyValuePair> runtimeParameterValues, Dictionary<string, string> capturedValues)
        {
            var paramValues = new Dictionary<string, string>();
            if (globalEnvironment != null)
            {
                foreach (var param in globalEnvironment.Variables)
                    paramValues[param.Key] = param.Value;
            }
            if (environment != null)
            {
                foreach (var param in environment.Variables)
                    paramValues[param.Key] = param.Value;
            }
            foreach (var param in runtimeParameterValues)
                paramValues[param.Key] = param.Value;
            foreach (var param in Parameters)
            {
                var isPersistent = param.Name.StartsWith("!"); //any parameter that starts with an exclamantion mark should be persisted as a capture value
                var paramName = isPersistent ? param.Name.Substring(1) : param.Name;

                //only override the value if the new value is not empty
                var newValue = ReplaceVariables(param.Value, paramValues);
                if ((!string.IsNullOrEmpty(newValue)) || (!paramValues.ContainsKey(paramName)))
                    paramValues[paramName] = ReplaceVariables(param.Value, paramValues); //since the order isn't guaranteed, true parameters should never be used as a default value.  only generators

                if (isPersistent)
                {
                    if (capturedValues.ContainsKey(paramName))
                        capturedValues[paramName] = paramValues[paramName];
                    else
                        capturedValues.Add(paramName, paramValues[paramName]);
                }
            }

            return paramValues;
        }

        #endregion Private Methods

        #region ISerializable

        private const int SerializationVersion = 2;

        //serialize this object
        public virtual void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("Serialization_Version", SerializationVersion, typeof(int));
            info.AddValue("Id", Id, typeof(Guid));
            info.AddValue("Body", Body, typeof(string));
            info.AddValue("CaptureValues", CaptureValues, typeof(ObservableCollection<CaptionedKeyValuePair>));
            info.AddValue("Category", Category, typeof(Guid));
            info.AddValue("CredentialName", CredentialName, typeof(string));
            info.AddValue("Description", Description, typeof(string));
            info.AddValue("Headers", Headers, typeof(Dictionary<string, string>));
            info.AddValue("Label", Label, typeof(string));
            info.AddValue("Parameters", Parameters, typeof(ObservableCollection<RestParameter>));
            info.AddValue("ResourceUrl", ResourceUrl, typeof(string));
            info.AddValue("Username", Username, typeof(string));
            info.AddValue("Verb", Verb, typeof(Method));

            //encrypt the password before saving it
            if (Password != null)
            {
                byte[] encryptedBytes = ProtectedData.Protect(Encoding.UTF8.GetBytes(Password), null,
                    DataProtectionScope.CurrentUser);
                info.AddValue(nameof(Password), Convert.ToBase64String(encryptedBytes), typeof (string));
            }
            else
                info.AddValue(nameof(Password), null, typeof(string));
        }

        //special constructor that is used to deserialize values
        protected RestCommand(SerializationInfo info, StreamingContext context)
        {
            int serializationVersion = info.GetInt32("Serialization_Version");
            Id = (Guid)info.GetValue("Id", typeof(Guid));
            Body = info.GetString("Body");
            CaptureValues = (ObservableCollection<CaptionedKeyValuePair>)info.GetValue("CaptureValues", typeof(ObservableCollection<CaptionedKeyValuePair>));
            Category = (RestCommandCategory)info.GetValue("Category", typeof (RestCommandCategory));
            Description = info.GetString("Description");
            Headers = (ObservableCollection<CaptionedKeyValuePair>)info.GetValue("Headers", typeof(ObservableCollection<CaptionedKeyValuePair>));
            Label = info.GetString("Label");
            Parameters = (ObservableCollection<RestParameter>)info.GetValue("Parameters", typeof (ObservableCollection<RestParameter>));
            ResourceUrl = info.GetString("ResourceUrl");
            Verb = (Method)info.GetValue("Verb", typeof(Method));

            if (serializationVersion < 2)
                return;

            CredentialName = info.GetString(nameof(CredentialName));
            Username = info.GetString(nameof(Username));
            //decrypt the password
            var encString = info.GetString(nameof(Password));
            if (encString != null)
            {
                byte[] clearBytes = ProtectedData.Unprotect(Convert.FromBase64String(encString), null,
                    DataProtectionScope.CurrentUser);
                _password = Encoding.UTF8.GetString(clearBytes);
            }

            SetupDefaultIsOpens();
        }

        #endregion ISerializable
    }
}
