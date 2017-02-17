using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using RestRunner.ViewModels;

namespace RestRunner.Models
{
    [Serializable]
    [DebuggerDisplay("ID: {Id}, Name: {Name}")]
    public class RestCommandCategory : ObservableBase, IComparable, IEquatable<RestCommandCategory>
    {
        public static RestCommandCategory DefaultCategory { get; } = new RestCommandCategory("Misc.", "", "", "");

        public RestCommandCategory(string name, string baseUrl, string username = null, string password = null, Guid? id = null)
        {
            Id = id ?? Guid.NewGuid();
            _name = name;
            _baseUrl = baseUrl;
            _username = username;
            _password = password;
        }

        #region Properties

        public Guid Id { get; set; }

        private string _baseUrl;
        public string BaseUrl
        {
            get { return _baseUrl; }
            set { Set(ref _baseUrl, value); }
        }

        /// <summary>
        /// The name of the credential to use in the current environment.  If the currency environment does
        /// not contain a credential with this name, then it will be ignored.  If it does contain a credential
        /// with this name, then any username/password will be ignored
        /// </summary>
        private string _credentialName;
        public string CredentialName
        {
            get { return _credentialName; }
            set { Set(ref _credentialName, value); }
        }

        private bool _isExpanded;
        public bool IsExpanded
        {
            get { return _isExpanded; }
            set { Set(ref _isExpanded, value); }
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

        #region Public Methods

        public int CompareTo(object obj)
        {
            if (obj == null)
                return 1;

            var thatObject = (RestCommandCategory) obj;
            return string.Compare(Name, thatObject.Name, StringComparison.Ordinal);
        }

        /// <summary>
        /// Create a completely new object, that is a copy of this one.
        /// </summary>
        /// <returns></returns>
        public RestCommandCategory DeepCopy()
        {
            IFormatter formatter = new BinaryFormatter();
            using (MemoryStream s = new MemoryStream())
            {
                formatter.Serialize(s, this);
                s.Position = 0;
                RestCommandCategory result = (RestCommandCategory)formatter.Deserialize(s);
                result.Id = Guid.NewGuid();
                return result;
            }
        }

        public bool Equals(RestCommandCategory other)
        {
            if (ReferenceEquals(null, other)) return false;

            return
                string.Equals(_baseUrl, other._baseUrl) &&
                string.Equals(_name, other._name) &&
                string.Equals(_credentialName, other._credentialName) &&
                string.Equals(_password, other._password) &&
                string.Equals(_username, other._username);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj.GetType() == GetType() && Equals(obj as RestCommandCategory);
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }

        #endregion Public Methods
    }
}
