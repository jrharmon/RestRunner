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
    class HttpStatusCodeToBrushConverter : IValueConverter
    {
        private static readonly Brush InvalidBrush = new SolidColorBrush(Color.FromRgb(254, 151, 0));
        private static readonly Brush ErrorBrush = new SolidColorBrush(Color.FromRgb(243, 66, 53));
        private static readonly Brush MovedBrush = new SolidColorBrush(Color.FromRgb(2, 168, 243));
        private static readonly Brush OkBrush = new SolidColorBrush(Color.FromRgb(75, 174, 79));
        private static readonly Brush DefaultBrush = new SolidColorBrush(Color.FromRgb(157, 157, 157));

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var statusCode = (int)value;
            if ((statusCode >= 200) && (statusCode < 300))
                return OkBrush;
            if ((statusCode >= 300) && (statusCode < 400))
                return MovedBrush;
            if ((statusCode >= 400) && (statusCode < 500))
                return InvalidBrush;
            if ((statusCode >= 500) && (statusCode < 600))
                return ErrorBrush;

            return DefaultBrush;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
