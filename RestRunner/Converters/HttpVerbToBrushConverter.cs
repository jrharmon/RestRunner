using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media;
using RestSharp;

namespace RestRunner.Converters
{
    public class HttpVerbToBrushConverter : IValueConverter
    {
        private static readonly Brush GetBrush = new SolidColorBrush(Color.FromRgb(75, 174, 79));
        private static readonly Brush PostBrush = new SolidColorBrush(Color.FromRgb(254, 151, 0));
        private static readonly Brush PutBrush = new SolidColorBrush(Color.FromRgb(2, 168, 243));
        private static readonly Brush DefaultBrush = new SolidColorBrush(Color.FromRgb(157, 157, 157));

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var verb = (Method) value;
            switch (verb)
            {
                case Method.GET:
                    return GetBrush;
                case Method.POST:
                    return PostBrush;
                case Method.PUT:
                    return PutBrush;
                default:
                    return DefaultBrush;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
