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

            var locVariables = new ObservableCollection<CaptionedKeyValuePair>
            {
                new CaptionedKeyValuePair("service-host", "http://localhost:5555"),
            };
            var devVariables = new ObservableCollection<CaptionedKeyValuePair>
            {
                new CaptionedKeyValuePair("service-host", "http://localhost:5555"),
            };
            var tstVariables = new ObservableCollection<CaptionedKeyValuePair>
            {
                new CaptionedKeyValuePair("service-host", "http://localhost:5555"),
            };
            var stgVariables = new ObservableCollection<CaptionedKeyValuePair>
            {
                new CaptionedKeyValuePair("service-host", "http://localhost:5555"),
            };
            var prdVariables = new ObservableCollection<CaptionedKeyValuePair>
            {
                new CaptionedKeyValuePair("service-host", "http://localhost:5555"),
            };

            var locCredentials = new ObservableCollection<RestCredential>
            {
                new RestCredential("Test Credential", "username", "password")
            };
            var devCredentials = new ObservableCollection<RestCredential>
            {
                new RestCredential("Test Credential", "username", "password")
            };
            var tstCredentials = new ObservableCollection<RestCredential>
            {
                new RestCredential("Test Credential", "username", "password")
            };
            var stgCredentials = new ObservableCollection<RestCredential>
            {
                new RestCredential("Test Credential", "username", "password")
            };
            var prdCredentials = new ObservableCollection<RestCredential>
            {
                new RestCredential("Test Credential", "username", "password")
            };

            _environments = new List<RestEnvironment>()
            {
                new RestEnvironment("local", "", locVariables, locCredentials),
                new RestEnvironment("dev", "", devVariables, devCredentials),
                new RestEnvironment("test", "", tstVariables, tstCredentials),
                new RestEnvironment("stage", "", stgVariables, stgCredentials, 5),
                new RestEnvironment("prod", "", prdVariables, prdCredentials, 1)
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
