using System;
using System.Globalization;
using System.Windows.Data;

namespace OptikFormApp.ViewModels
{
    public class ValueToHeightConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double val)
            {
                double factor = 1.0;
                if (parameter != null && double.TryParse(parameter.ToString(), out double p))
                {
                    factor = p;
                }
                return val * factor;
            }
            if (value is int intVal)
            {
                double factor = 1.0;
                if (parameter != null && double.TryParse(parameter.ToString(), out double p))
                {
                    factor = p;
                }
                return (double)intVal * factor;
            }
            return 0.0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }
}
