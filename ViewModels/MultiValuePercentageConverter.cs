using System;
using System.Globalization;
using System.Windows.Data;

namespace OptikFormApp.ViewModels
{
    public class MultiValuePercentageConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length >= 2 && values[0] is double percentage && values[1] is double totalWidth)
            {
                // MultiBinding: [0] = Percentage (0-100), [1] = Total Width
                return (percentage / 100.0) * totalWidth;
            }
            return 0.0;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
