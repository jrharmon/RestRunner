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

namespace RestRunner.Services
{
    public interface IEnvironmentService
    {
        Task<List<RestEnvironment>> GetEnvironmentsAsync();
        bool HasChanged(IList<RestEnvironment> environments);
        Task SaveEnvironmentsAsync(IEnumerable<RestEnvironment> environments);
    }

    public class EnvironmentService : IEnvironmentService
    {
        private readonly string _environmentsFilePath = Settings.Default.SaveFolder + @"\environments.dat";
        private List<RestEnvironment> _mostRecentEnvironments; //a copy of the last list returned or saved

        public async Task<List<RestEnvironment>> GetEnvironmentsAsync()
        {
            List<RestEnvironment> result;
            if (File.Exists(_environmentsFilePath))
            {
                List<RestEnvironment> environments = null;
                await Task.Run(() =>
                {
                    IFormatter formatter = new BinaryFormatter();
                    using (var s = new FileStream(_environmentsFilePath, FileMode.Open))
                    {
                        environments = (List<RestEnvironment>)formatter.Deserialize(s);
                    }
                });

                var sortedEnvironments = environments.OrderBy(c => c.Name).ToList();
                result = new List<RestEnvironment>(sortedEnvironments);
            }
            else
                result = new List<RestEnvironment>();

            //make sure that all objects have valid IDs (this should just be temporary, as once it is rolled out, everyone's IDs should be all set)
            foreach (var env in result)
            {
                foreach (var cred in env.Credentials)
                {
                    if (cred.Id == Guid.Empty)
                        cred.Id = Guid.NewGuid();
                }
            }

            _mostRecentEnvironments = result.Select(c => c.DeepCopy()).ToList();
            return result;
        }

        public bool HasChanged(IList<RestEnvironment> environments)
        {
            var origEnvironments = _mostRecentEnvironments;

            if (origEnvironments.Count != environments.Count)
                return true;

            return !origEnvironments.SequenceEqual(environments);
        }

        public async Task SaveEnvironmentsAsync(IEnumerable<RestEnvironment> environments)
        {
            await Task.Run(() =>
            {
                IFormatter formatter = new BinaryFormatter();
                using (var s = new FileStream(_environmentsFilePath, FileMode.Create))
                {
                    formatter.Serialize(s, environments.ToList());
                }
            });

            _mostRecentEnvironments = environments.Select(c => c.DeepCopy()).ToList();
        }
    }
}
