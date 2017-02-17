using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RestRunner.Models;

namespace RestRunner.Helpers
{
    public static class BasicAuthConverter
    {
        public static RestCredential BasicAuthToCredential(string basicAuthHeader)
        {
            //The authorization header is either empty or isn't Basic
            if (!basicAuthHeader.StartsWith("Basic "))
                return null;

            //convert the header to username and password
            string encodedUsernamePassword = basicAuthHeader.Substring("Basic ".Length).Trim(); //strip the Basic prefix
            string usernamePassword = Encoding.UTF8.GetString(Convert.FromBase64String(encodedUsernamePassword));
            var userPwdSplit = usernamePassword.Split(':');

            return new RestCredential("", userPwdSplit[0], userPwdSplit[1]);
        }
    }
}
