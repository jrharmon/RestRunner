using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace RestRunner.Converters
{
    /// <summary>
    /// If the string has any value, return Visible, else collapsed when empty or null
    /// </summary>
    public class StringHasValueToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string strValue = value as string; //if the input is not a string, it will be null and treated like an empty string

            return !string.IsNullOrEmpty(strValue) ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
