using System;
using System.Collections.Generic;
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
    public interface ICommandChainService
    {
        Task<List<RestCommandChain>> GetCommandChainsAsync(IList<RestCommandChainCategory> chainCategories = null);
        bool HasChanged(IList<RestCommandChain> chains);
        Task SaveCommandChainsAsync(IEnumerable<RestCommandChain> chains);
    }

    public class CommandChainService : ICommandChainService
    {
        private readonly string _commandChainsFilePath = Settings.Default.SaveFolder + @"\commandChains.dat";
        private List<RestCommandChain> _mostRecentChains; //a copy of the last list returned or saved

        public async Task<List<RestCommandChain>> GetCommandChainsAsync(IList<RestCommandChainCategory> chainCategories = null)
        {
            List<RestCommandChain> result;
            if (File.Exists(_commandChainsFilePath))
            {
                List<RestCommandChain> chains = null;
                await Task.Run(() =>
                {
                    IFormatter formatter = new BinaryFormatter();
                    using (var s = new FileStream(_commandChainsFilePath, FileMode.Open))
                    {
                        chains = (List<RestCommandChain>)formatter.Deserialize(s);
                    }
                });

                result = chains.OrderBy(c => c.Label).ToList(); //order all chains alphabetically
            }
            else
                result = new List<RestCommandChain>();

            //make sure that all objects have valid IDs (this should just be temporary, as once it is rolled out, everyone's IDs should be all set)
            foreach (var chain in result)
            {
                if (chain.Id == Guid.Empty)
                    chain.Id = Guid.NewGuid();
                if (chain.Category.Id == Guid.Empty)
                    chain.Category.Id = Guid.NewGuid();
            }

            _mostRecentChains = result.Select(c => c.DeepCopy()).ToList();
            return result;
        }

        public bool HasChanged(IList<RestCommandChain> chains)
        {
            var origChains = _mostRecentChains;

            if (origChains.Count != chains.Count)
                return true;

            return origChains.Where((t, i) => !t.Equals(chains[i])).Any();
        }

        public async Task SaveCommandChainsAsync(IEnumerable<RestCommandChain> chains)
        {
            await Task.Run(() =>
            {
                IFormatter formatter = new BinaryFormatter();
                using (var s = new FileStream(_commandChainsFilePath, FileMode.Create))
                {
                    formatter.Serialize(s, chains.ToList());
                }
            });

            _mostRecentChains = chains.Select(c => c.DeepCopy()).ToList();
        }
    }
}
