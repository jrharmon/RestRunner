using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RestSharp;

namespace RestRunner.Models
{
    [Serializable]
    public class RestResult
    {
        public Dictionary<string, string> CapturedValues { get; }
        public RestCommand Command { get; } //the command that created this result
        public TimeSpan Duration { get; } //how long the command took to run

        /// <summary>
        /// True if the command did not complete successfully.  This is not related to the status code, so if a command comes back with a
        /// status code of 500, this will still be false.
        /// </summary>
        public bool HadTransmissionError { get; set; }

        //complete text of the HTTP input/output text
        public string HttpCommunicationDisplay
        {
            get
            {
                var sb = new StringBuilder(InputHeadersDisplay);
                sb.AppendLine();
                sb.AppendLine(RequestBody);
                sb.AppendLine();
                sb.AppendLine(OutputHeadersDisplay);
                sb.AppendLine();
                sb.AppendLine(ResponseBody);

                return sb.ToString();
            }
        }

        public string InputHeadersDisplay { get; }
        public string OutputHeadersDisplay
        {
            get
            {
                var sb = new StringBuilder();
                if (RestResponse != null)
                {
                    sb.AppendLine($"HTTP/1.1 {(int) RestResponse.StatusCode} {RestResponse.StatusDescription}");
                    foreach (var header in RestResponse.Headers)
                        sb.AppendLine($"{header.Name}: {header.Value}");
                    if (RestResponse.Headers.All(h => h.Name != "Content-Length"))
                        sb.AppendLine($"Content-Length: {RestResponse.Content.Length}");
                }

                //remove the trailing EOL
                if (sb.Length > 2)
                    sb.Length -= 2;

                return sb.ToString();
            }
        }
        public string RequestBody { get; }
        public string ResponseBody { get; }
        public IRestResponse RestResponse { get; }
        public int StatusCode => RestResponse != null ? (int)RestResponse.StatusCode : 0;

        /// <summary>
        /// True if the command returned, and had a status code in the 200 range.  If the command did not return, or had a status code outside
        /// of that range, then it's false, and will be used to stop further commands in a chain from running, or further commands/chains run in
        /// a multiple run.
        /// </summary>
        public bool Succeeded { get; }

        public RestResult(string requestBody, string responseBody, Dictionary<string, string> capturedValues, TimeSpan duration, IRestResponse restResponse, string inputHeadersDisplay, bool succeeded, RestCommand command)
        {
            RequestBody = requestBody;
            ResponseBody = responseBody;
            CapturedValues = capturedValues;
            Duration = duration;
            HadTransmissionError = restResponse?.StatusCode == 0;
            RestResponse = restResponse;
            InputHeadersDisplay = inputHeadersDisplay;
            int statusCode = restResponse != null ? (int) restResponse.StatusCode : 0;
            Succeeded = ((statusCode >= 200) && (statusCode < 300));
            Command = command;
        }
    }
}
