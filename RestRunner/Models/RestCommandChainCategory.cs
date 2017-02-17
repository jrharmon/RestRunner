using System;
using System.Collections.Generic;
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
    [DebuggerDisplay("ID: {Id}, Name: {Name}")]
    public class RestCommandChainCategory : ObservableBase, IComparable, IEquatable<RestCommandChainCategory>
    {
        public static RestCommandChainCategory DefaultCategory { get; } = new RestCommandChainCategory("Misc.");

        public RestCommandChainCategory(string name)
        {
            Id = Guid.NewGuid();
            _name = name;
        }

        #region Properties

        public Guid Id { get; set; }

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
        
        #endregion Properties

        #region Public Methods

        public int CompareTo(object obj)
        {
            if (obj == null)
                return 1;

            var thatObject = (RestCommandChainCategory)obj;
            return string.Compare(Name, thatObject.Name, StringComparison.Ordinal);
        }

        /// <summary>
        /// Create a completely new object, that is a copy of this one.
        /// </summary>
        /// <returns></returns>
        public RestCommandChainCategory DeepCopy()
        {
            IFormatter formatter = new BinaryFormatter();
            using (MemoryStream s = new MemoryStream())
            {
                formatter.Serialize(s, this);
                s.Position = 0;
                RestCommandChainCategory result = (RestCommandChainCategory)formatter.Deserialize(s);
                result.Id = Guid.NewGuid();
                return result;
            }
        }

        public bool Equals(RestCommandChainCategory other)
        {
            if (ReferenceEquals(null, other)) return false;

            return
                string.Equals(_name, other._name);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj.GetType() == GetType() && Equals(obj as RestCommandChainCategory);
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }

        #endregion Public Methods
    }
}
