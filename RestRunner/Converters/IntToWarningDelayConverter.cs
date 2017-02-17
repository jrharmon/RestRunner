using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace RestRunner.Converters
{
    public class IntToWarningDelayConverter : IValueConverter
    {
        /// <summary>
        /// Treats blanks as -1, and any negative number as a blank
        /// </summary>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (int)value >= 0 ? value.ToString() : "";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            //remove any delay if a non-number is passed in
            int intValue;
            if (!int.TryParse((string) value, out intValue))
                return -1;

            return string.IsNullOrEmpty((string)value) ? -1 : System.Convert.ToInt32(value);
        }
    }
}
