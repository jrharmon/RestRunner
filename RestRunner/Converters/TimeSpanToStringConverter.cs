using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace RestRunner.Converters
{
    public class TimeSpanToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var input = (TimeSpan) value;
            var hideWhenZero = parameter != null && System.Convert.ToBoolean(parameter);

            if ((input == TimeSpan.Zero) && (hideWhenZero))
                return "";

            return $"({input.ToString(@"hh\:mm\:ss\.ff")})";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
