using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using RestRunner.Models;
using RestRunner.Properties;
using RestRunner.Services;

namespace RestRunner.Design
{
    public class DesignEnvironmentService : IEnvironmentService
    {
        private readonly string _categoriesFilePath = Settings.Default.SaveFolder + @"\environments.dat";
        private List<RestEnvironment> _environments; 

        public async Task<List<RestEnvironment>> GetEnvironmentsAsync()
        {
            if (_environments != null)
                return _environments;

            var envCredentials = new ObservableCollection<RestCredential>
            {
                new RestCredential("Test Credential", "username", "password")
            };

            var envVariables = new ObservableCollection<CaptionedKeyValuePair>
            {
                new CaptionedKeyValuePair("service-host", "http://localhost:5555"),
            };

            _environments = new List<RestEnvironment>()
            {
                new RestEnvironment("local", "", envVariables, envCredentials),
                new RestEnvironment("dev", "", envVariables, envCredentials),
                new RestEnvironment("test", "", envVariables, envCredentials),
                new RestEnvironment("stage", "", envVariables, envCredentials, 5),
                new RestEnvironment("prod", "", envVariables, envCredentials, 1)
            };

            return _environments;
        }

        public bool HasChanged(IList<RestEnvironment> environments)
        {
            return false;
        }

        /// <summary>
        /// This is helpful for resetting the save file if something gets screwed up.
        /// </summary>
        /// <param name="environments"></param>
        /// <returns></returns>
        public async Task SaveEnvironmentsAsync(IEnumerable<RestEnvironment> environments)
        {
            await Task.Run(() =>
            {
                IFormatter formatter = new BinaryFormatter();
                using (var s = new FileStream(_categoriesFilePath, FileMode.Create))
                {
                    formatter.Serialize(s, environments.ToList());
                }
            });
        }
    }
}
