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
using RestSharp;

namespace RestRunner.Design
{
    public class DesignCommandService : ICommandService
    {
        private readonly string _commandsFilePath = Settings.Default.SaveFolder + @"\commands.dat";
        private List<RestCommand> _commands; 

        public async Task<List<RestCommand>> GetCommandsAsync()
        {
            if (_commands != null)
                return _commands;

            var result = new List<RestCommand>();
            var adhocCategory = new RestCommandCategory("Adhoc", @"");
            
            //Adhoc
            var bodyText = @"";
            var captureValues = new ObservableCollection<CaptionedKeyValuePair>();
            var command = new RestCommand("%service-host%/", bodyText, Method.POST)
            {
                CaptureValues = captureValues,
                Category = adhocCategory,
                Label = "Adhoc"
            };
            result.Add(command);
            
            _commands = result;
            return _commands;
        }

        public bool HasChanged(IList<RestCommand> commands)
        {
            return false;
        }

        /// <summary>
        /// This is helpful for resetting the save file if something gets screwed up.
        /// </summary>
        /// <param name="commands"></param>
        /// <returns></returns>
        public async Task SaveCommandsAsync(IEnumerable<RestCommand> commands)
        {
            await Task.Run(() =>
            {
                IFormatter formatter = new BinaryFormatter();
                using (var s = new FileStream(_commandsFilePath, FileMode.Create))
                {
                    formatter.Serialize(s, commands.ToList());
                }
            });
        }
    }
}
