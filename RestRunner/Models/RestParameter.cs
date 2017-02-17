using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms.VisualStyles;
using RestRunner.ViewModels;
using RestSharp;

namespace RestRunner.Models
{
    [Serializable]
    public class RestParameter : ObservableBase, ISerializable, IEquatable<RestParameter>
    {
        private const int MaxParameterPresetValues = 10;
        
        private string _name;
        public string Name
        {
            get { return _name; }
            set { Set(ref _name, value); }
        }
        
        private string _value;
        public string Value
        {
            get { return _value; }
            set { Set(ref _value, value); }
        }
        
        private ObservableCollection<string> _presetValues;
        public ObservableCollection<string> PresetValues
        {
            get { return _presetValues; }
            set { Set(ref _presetValues, value); }
        }

        public bool IsPresetValuesUpdatedOnExecution { get; set; } //if true, any value used when running a command will be added here, and old values will get aged out

        public RestParameter(string name, IList<string> presetValues)
        {
            Name = name;
            Value = "";
            PresetValues = presetValues is ObservableCollection<string>
                ? (ObservableCollection<string>) presetValues
                : new ObservableCollection<string>(presetValues ?? new List<string>());
            IsPresetValuesUpdatedOnExecution = true;
        }

        public RestParameter(string name, string value, IList<string> presetValues = null, bool isPresetValuesUpdatedOnExecution = true)
        {
            Name = name;
            Value = value;
            PresetValues = presetValues is ObservableCollection<string>
                ? (ObservableCollection<string>)presetValues
                : new ObservableCollection<string>(presetValues ?? new List<string>());
            IsPresetValuesUpdatedOnExecution = isPresetValuesUpdatedOnExecution;
        }

        public bool Equals(RestParameter other)
        {
            if (ReferenceEquals(null, other)) return false;

            return
                string.Equals(Name, other.Name) &&
                string.Equals(Value, other.Value);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj.GetType() == GetType() && Equals(obj as RestParameter);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Name?.GetHashCode() ?? 0;
                hashCode = (hashCode * 397) ^ (Value?.GetHashCode() ?? 0);
                hashCode = (hashCode * 397) ^ (PresetValues?.GetHashCode() ?? 0);
                return hashCode;
            }
        }

        public void UpdatePresetValues()
        {
            if (!IsPresetValuesUpdatedOnExecution)
                return;

            if (string.IsNullOrWhiteSpace(Value))
                return;

            //add or move the value to the top, and make sure the list doesn't get too long
            if (!PresetValues.Contains(Value))
            {
                PresetValues.Insert(0, Value);

                if (PresetValues.Count > MaxParameterPresetValues)
                    PresetValues.RemoveAt(MaxParameterPresetValues);
            }
            else
            {
                var curValue = Value; //for some crazy reason, as soon as you remove Value (or even if you set the value of PresetValus[0] (where the index points to the position with Value)), Value gets set to ""
                PresetValues.Remove(Value);
                Value = curValue;
                PresetValues.Insert(0, Value);
            }
        }

        #region ISerializable

        private const int SerializationVersion = 1;

        //serialize this object
        public virtual void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("Serialization_Version", SerializationVersion, typeof(int));
            info.AddValue("Name", Name, typeof(string));
            info.AddValue("Value", Value, typeof(string));
            info.AddValue("PresetValues", PresetValues, typeof(ObservableCollection<string>));
            info.AddValue("IsPresetValuesUpdatedOnExecution", IsPresetValuesUpdatedOnExecution, typeof(bool));
        }

        //special constructor that is used to deserialize values
        protected RestParameter(SerializationInfo info, StreamingContext context)
        {
            //if this is from the old automated serialization format, handle it differently
            if (info.MemberCount == 3)
            {
                List<string> presetValues = null;
                foreach (var prop in info)
                {
                    if (prop.Name.Contains("<Name>"))
                        Name = (string)prop.Value;
                    else if (prop.Name.Contains("<Value>"))
                        Value = (string)prop.Value;
                    else if (prop.Name.Contains("<PresetValues>"))
                        presetValues = (List<string>)prop.Value;
                }

                PresetValues = new ObservableCollection<string>(presetValues ?? new List<string>());
                IsPresetValuesUpdatedOnExecution = true;

                return;
            }

            int serializationVersion = info.GetInt32("Serialization_Version");
            Name = info.GetString("Name");
            Value = info.GetString("Value");
            PresetValues = (ObservableCollection<string>)info.GetValue("PresetValues", typeof(ObservableCollection<string>));
            IsPresetValuesUpdatedOnExecution = info.GetBoolean("IsPresetValuesUpdatedOnExecution");
        }

        #endregion ISerializable
    }
}
