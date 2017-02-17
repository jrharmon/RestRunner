using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using RestRunner.ViewModels;

namespace RestRunner.Models
{
    [Serializable]
    [DebuggerDisplay("ID: {Id}, Label: {Name}")]
    public class RestEnvironment : ObservableBase, IEquatable<RestEnvironment>
    {
        public RestEnvironment(string name, string description = null, ObservableCollection<CaptionedKeyValuePair> variables = null, ObservableCollection<RestCredential> credentials = null,  int executionWarningDelayMinutes = -1)
        {
            Id = Guid.NewGuid();
            _name = name;
            _description = description;
            _variables = variables;
            _credentials = credentials;
            _executionWarningDelayMinutes = executionWarningDelayMinutes;
        }

        #region Properties

        public Guid Id { get; private set; }

        //used for ComboBoxes to bind to, as they need to match up to a string value
        public IList<string> CredentialNames => Credentials.Select(c => c.Name).ToList();

        private ObservableCollection<RestCredential> _credentials;
        public ObservableCollection<RestCredential> Credentials
        {
            get { return _credentials ?? (_credentials = new ObservableCollection<RestCredential>()); }
            set
            {
                Set(ref _credentials, value);
                RaisePropertyChanged("CredentialNames");
            }
        }

        private string _description;
        public string Description
        {
            get { return _description; }
            set { Set(ref _description, value); }
        }

        /// <summary>
        /// The number of minutes of being in an environment without executing anything before any further executions
        /// have a warning appear, reminding the user what environment they are in.  0 means that they will be warned
        /// for every execution, and any negative number means that they will never be warned.  When first opening the
        /// app, if the current environment has a warning, it will be active immediately.
        /// </summary>
        private int _executionWarningDelayMinutes;
        public int ExecutionWarningDelayMinutes
        {
            get { return _executionWarningDelayMinutes; }
            set { Set(ref _executionWarningDelayMinutes, value); }
        }

        private string _name;
        public string Name
        {
            get { return _name; }
            set { Set(ref _name, value); }
        }

        private ObservableCollection<CaptionedKeyValuePair> _variables;
        public ObservableCollection<CaptionedKeyValuePair> Variables
        {
            get { return _variables ?? (_variables = new ObservableCollection<CaptionedKeyValuePair>()); }
            set { Set(ref _variables, value); }
        }

        #endregion Properties

        /// <summary>
        /// Create a completely new object, that is a copy of this one.
        /// </summary>
        /// <returns></returns>
        public RestEnvironment DeepCopy()
        {
            IFormatter formatter = new BinaryFormatter();
            using (MemoryStream s = new MemoryStream())
            {
                formatter.Serialize(s, this);
                s.Position = 0;
                RestEnvironment result = (RestEnvironment)formatter.Deserialize(s);
                result.Id = Guid.NewGuid();
                return result;
            }
        }

        public bool Equals(RestEnvironment other)
        {
            if (ReferenceEquals(null, other)) return false;

            return
                Credentials.SequenceEqual(other.Credentials) && //use public property to protect against null values
                string.Equals(_description, other._description) &&
                _executionWarningDelayMinutes == other._executionWarningDelayMinutes &&
                string.Equals(_name, other._name) &&
                Variables.SequenceEqual(other.Variables); //use public property to protect against null values
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj.GetType() == GetType() && Equals(obj as RestEnvironment);
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }

        /// <summary>
        /// Add all variables and credentials with values from the environment passed in.
        /// </summary>
        /// <param name="updatedEnvironment"></param>
        /// <param name="onlyAddNew">If true, don't update existing values.  Only add new values that did not already exist.</param>
        public void Update(RestEnvironment updatedEnvironment, bool onlyAddNew = false)
        {
            if (updatedEnvironment == null)
                return;

            //updates are done by dropping/removing, so that data bindings will be properly updated

            foreach (var variable in updatedEnvironment.Variables)
            {
                var existingVariable = Variables.FirstOrDefault(v => v.Key == variable.Key);

                if (existingVariable != null)
                {
                    if (onlyAddNew)
                        continue;
                    Variables.Remove(existingVariable);
                }

                Variables.Add(variable);
            }
            foreach (var cred in updatedEnvironment.Credentials)
            {
                var existingCredential = Credentials.FirstOrDefault(c => c.Name == cred.Name);

                if (existingCredential != null)
                {
                    if (onlyAddNew)
                        continue;
                    Credentials.Remove(existingCredential);
                }

                Credentials.Add(cred);
            }
        }
    }
}
