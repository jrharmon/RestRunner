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
    public class DesignUserStateService : IUserStateService
    {
        private readonly string _userStateFilePath = Settings.Default.SaveFolder + @"\userState.dat";

        private UserState _userState;
        public UserState UserState
        {
            get
            {
                if (_userState == null)
                {
                    var commandService = new DesignCommandService();
                    var environmentService = new DesignEnvironmentService();
                    _userState = new UserState();
                    _userState.SelectedCommandId = commandService.GetCommandsAsync().Result.First(c => c.Label == "Adhoc").Id;
                    _userState.SelectedEnvironmentId = environmentService.GetEnvironmentsAsync().Result[0].Id;
                }

                return _userState;
            }
        }
        public async Task SaveUserStateAsync()
        {
            await Task.Run(() =>
            {
                IFormatter formatter = new BinaryFormatter();
                using (var s = new FileStream(_userStateFilePath, FileMode.Create))
                {
                    formatter.Serialize(s, UserState);
                }
            });
        }
    }
}
