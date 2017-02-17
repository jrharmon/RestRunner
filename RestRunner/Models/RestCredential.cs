using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using RestRunner.ViewModels;

namespace RestRunner.Models
{
    [Serializable]
    public class RestCredential : ObservableBase, ISerializable, IEquatable<RestCredential>
    {
        public RestCredential(string name, string username, string password)
        {
            Id = Guid.NewGuid();
            _name = name;
            _username = username;
            _password = password;
        }

        #region Properties

        public Guid Id { get; set; }

        private string _description;
        public string Description
        {
            get { return _description; }
            set { Set(ref _description, value); }
        }

        private string _name;
        public string Name
        {
            get { return _name; }
            set { Set(ref _name, value); }
        }

        private string _password;
        public string Password
        {
            get { return _password; }
            set { Set(ref _password, value); }
        }

        private string _username;
        public string Username
        {
            get { return _username; }
            set { Set(ref _username, value); }
        }

        #endregion Properties

        #region IEquatable

        public bool Equals(RestCredential other)
        {
            if (ReferenceEquals(null, other)) return false;

            return
                string.Equals(_description, other._description) &&
                string.Equals(_name, other._name) &&
                string.Equals(_password, other._password) &&
                string.Equals(_username, other._username);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj.GetType() == GetType() && Equals(obj as RestCredential);
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }

        #endregion IEquatable

        #region ISerializable

        private const int SerializationVersion = 1;

        //serialize this object
        public virtual void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("Serialization_Version", SerializationVersion, typeof(int));
            info.AddValue("Id", Id, typeof(Guid));
            info.AddValue("Description", Description, typeof(string));
            info.AddValue("Name", Name, typeof(string));
            info.AddValue("Username", Username, typeof(string));

            //encrypt the password before saving it
            byte[] encryptedBytes = ProtectedData.Protect(Encoding.UTF8.GetBytes(Password), null, DataProtectionScope.CurrentUser);
            info.AddValue("Password", Convert.ToBase64String(encryptedBytes), typeof(string));
        }

        //special constructor that is used to deserialize values
        protected RestCredential(SerializationInfo info, StreamingContext context)
        {
            int serializationVersion = info.GetInt32("Serialization_Version");
            Id = (Guid)info.GetValue("Id", typeof(Guid));
            _description = info.GetString("Description");
            _name = info.GetString("Name");
            _username = info.GetString("Username");

            //decrypt the password
            try
            {
                byte[] clearBytes = ProtectedData.Unprotect(Convert.FromBase64String(info.GetString("Password")), null, DataProtectionScope.CurrentUser);
                _password = Encoding.UTF8.GetString(clearBytes);
            }
            catch (Exception)
            {
                _password = "";
            }
            
        }

        #endregion ISerializable
    }
}
