using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GalaSoft.MvvmLight;
using RestRunner.ViewModels;

namespace RestRunner.Models
{
    /// <summary>
    /// This is needed to be able to pass this type as a type argument to a generic WPF control, as nested
    /// generic types aren't supported in WPF XAML
    /// </summary>
    [Serializable]
    public class CaptionedKeyValuePair : ObservableBase, IEquatable<CaptionedKeyValuePair>
    {
        public CaptionedKeyValuePair(string key, string value, string caption = null)
        {
            _key = key;
            _value = value;
            _caption = caption ?? key;
        }

        private string _caption;
        public string Caption
        {
            get { return _caption; }
            set { Set(ref _caption, value); }
        }

        private string _key;
        public string Key
        {
            get { return _key; }
            set { Set(ref _key, value); }
        }

        private string _value;
        public string Value
        {
            get { return _value; }
            set { Set(ref _value, value); }
        }

        public bool Equals(CaptionedKeyValuePair other)
        {
            if (ReferenceEquals(null, other)) return false;

            return
                string.Equals(_caption, other._caption) &&
                string.Equals(_key, other._key) &&
                string.Equals(_value, other._value);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj.GetType() == GetType() && Equals(obj as CaptionedKeyValuePair);
        }
    }

    public class CaptionedKeyValuePair<TKey, TValue> : ViewModelBase
    {
        public CaptionedKeyValuePair(TKey key, TValue value, string caption = null)
        {
            _key = key;
            _value = value;
            _caption = caption ?? key.ToString();
        }

        private string _caption;
        public string Caption
        {
            get { return _caption; }
            set { Set(ref _caption, value); }
        }

        private TKey _key;
        public TKey Key
        {
            get { return _key; }
            set { Set(ref _key, value); }
        }

        private TValue _value;
        public TValue Value
        {
            get { return _value; }
            set { Set(ref _value, value); }
        }
    }

    public static class CaptionedKeyValuePairListExtension
    {
        public static bool ContainsKey(this IList<CaptionedKeyValuePair> list, string key)
        {
            return list.Any(p => p.Key.Equals(key));
        }

        public static bool ContainsKey<TKey, TValue>(this IList<CaptionedKeyValuePair<TKey, TValue>> list, TKey key)
        {
            return list.Any(p => p.Key.Equals(key));
        }

        public static string Get(this IList<CaptionedKeyValuePair> list, string key)
        {
            return list.SingleOrDefault(p => p.Key.Equals(key)).Value;
        }

        public static TValue Get<TKey, TValue>(this IList<CaptionedKeyValuePair<TKey, TValue>> list, TKey key)
        {
            return list.SingleOrDefault(p => p.Key.Equals(key)).Value;
        }

        public static void Set(this IList<CaptionedKeyValuePair> list, string key, string value)
        {
            var pair = list.SingleOrDefault(p => p.Key.Equals(key));
            
            if (pair != null)
                pair.Value = value;
            else
                list.Add(new CaptionedKeyValuePair(key, value));
        }

        public static void Set<TKey, TValue>(this IList<CaptionedKeyValuePair<TKey, TValue>> list, TKey key, TValue value)
        {
            var pair = list.SingleOrDefault(p => p.Key.Equals(key));

            if (pair != null)
                pair.Value = value;
            else
                list.Add(new CaptionedKeyValuePair<TKey, TValue>(key, value));
        }
    }
}
