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
    public interface IUserStateService
    {
        UserState UserState { get; }
        Task SaveUserStateAsync();
    }

    public class UserStateService : IUserStateService
    {
        private readonly string _userStateFilePath = Settings.Default.SaveFolder + @"\userState.dat";

        private UserState _userState;
        public UserState UserState => _userState ?? (_userState = GetUserStateAsync());

        private UserState GetUserStateAsync()
        {
            UserState result = null;
            if (File.Exists(_userStateFilePath))
            {
                try
                {
                    IFormatter formatter = new BinaryFormatter();
                    using (var s = new FileStream(_userStateFilePath, FileMode.Open))
                    {
                        result = (UserState)formatter.Deserialize(s);
                    }
                }
                catch (Exception)
                {
                    return new UserState();
                }
            }
            else
                result = new UserState();

            return result;
        }

        public async Task SaveUserStateAsync()
        {
            if (_userState == null)
                return;

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
