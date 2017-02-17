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
    public interface ICommandService
    {
        Task<List<RestCommand>> GetCommandsAsync();
        bool HasChanged(IList<RestCommand> commands);
        Task SaveCommandsAsync(IEnumerable<RestCommand> commands);
    }

    public class CommandService : ICommandService
    {
        private readonly string _commandsFilePath = Settings.Default.SaveFolder + @"\commands.dat";
        private List<RestCommand> _mostRecentCommands; //a copy of the last list returned or saved

        public async Task<List<RestCommand>> GetCommandsAsync()
        {
            List<RestCommand> result;
            if (File.Exists(_commandsFilePath))
            {
                List<RestCommand> commands = null;
                await Task.Run(() =>
                {
                    IFormatter formatter = new BinaryFormatter();
                    using (var s = new FileStream(_commandsFilePath, FileMode.Open))
                    {
                        commands = (List<RestCommand>) formatter.Deserialize(s); //binary serializer preserves object references for the categories, so deserializing multiple commands with the same category will create a single category object they all point to
                    }
                });

                result = commands.OrderBy(c => c.Label).ToList(); //order all commands alphabetically
            }
            else
                result = new List<RestCommand>();

            _mostRecentCommands = result.Select(c => c.DeepCopy()).ToList();
            return result;
        }

        public bool HasChanged(IList<RestCommand> commands)
        {
            var origCommands = _mostRecentCommands;

            if (origCommands.Count != commands.Count)
                return true;

            return origCommands.Where((t, i) => !t.Equals(commands[i])).Any();
        }

        public async Task SaveCommandsAsync(IEnumerable<RestCommand> commands)
        {
            //make sure that all objects have valid IDs (this should just be temporary, as once it is rolled out, everyone's IDs should be all set)
            foreach (var command in commands)
            {
                if (command.Category.Id == Guid.Empty)
                    command.Category.Id = Guid.NewGuid();
            }

            await Task.Run(() =>
            {
                IFormatter formatter = new BinaryFormatter();
                using (var s = new FileStream(_commandsFilePath, FileMode.Create))
                {
                    formatter.Serialize(s, commands.ToList());
                }
            });

            _mostRecentCommands = commands.Select(c => c.DeepCopy()).ToList();
        }
    }
}
