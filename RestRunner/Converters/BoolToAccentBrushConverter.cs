using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace RestRunner.Converters
{
    class BoolToAccentBrushConverter : IValueConverter
    {
        private static readonly Brush DefaultBrush = new SolidColorBrush(Color.FromRgb(157, 157, 157));

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var boolValue = (bool)value;
            var desiredBoolValue = parameter == null || System.Convert.ToBoolean(parameter); //the value that will cause the result to be the highlight color.  the other value will be the default one
            Brush accentBrush = (Brush)Application.Current.FindResource("AccentColorBrush");
            return boolValue == desiredBoolValue ? accentBrush : DefaultBrush;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
