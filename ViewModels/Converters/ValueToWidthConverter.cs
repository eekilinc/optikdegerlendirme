using System;
using System.Globalization;
using System.Windows.Data;

namespace OptikFormApp.ViewModels
{
    public class ValueToWidthConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double successRate && parameter is string maxWidthStr && double.TryParse(maxWidthStr, out double maxWidth))
            {
                return (successRate / 100.0) * maxWidth;
            }
            return 0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
