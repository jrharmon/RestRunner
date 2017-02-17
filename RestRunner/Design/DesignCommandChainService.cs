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
using RestRunner.Services;

namespace RestRunner.Design
{
    public class DesignCommandChainService : ICommandChainService
    {
        private readonly string _commandChainsFilePath = Settings.Default.SaveFolder + @"\commandChains.dat";

        public async Task<List<RestCommandChain>> GetCommandChainsAsync(IList<RestCommandChainCategory> chainCategories = null)
        {
            var result = new List<RestCommandChain>();
            var commandService = new DesignCommandService();
            var commands = await commandService.GetCommandsAsync();

            var adhocCategory = new RestCommandChainCategory("Adhoc");

            var chain = new RestCommandChain("Adhoc") { Category = adhocCategory };
            chain.AddCommand(commands.Single(c => c.Label == "Adhoc"));
            result.Add(chain);

            return result;
        }

        public bool HasChanged(IList<RestCommandChain> chains)
        {
            return false;
        }

        /// <summary>
        /// This is helpful for resetting the save file if something gets screwed up.
        /// </summary>
        /// <param name="chains"></param>
        /// <returns></returns>
        public async Task SaveCommandChainsAsync(IEnumerable<RestCommandChain> chains)
        {
            //throw new NotImplementedException(); //uncomment to overwrite the save file with test data
            await Task.Run(() =>
            {
                IFormatter formatter = new BinaryFormatter();
                using (var s = new FileStream(_commandChainsFilePath, FileMode.Create))
                {
                    formatter.Serialize(s, chains.ToList());
                }
            });
        }
    }
}
