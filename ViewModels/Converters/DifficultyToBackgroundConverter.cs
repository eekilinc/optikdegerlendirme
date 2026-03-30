using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace OptikFormApp.ViewModels
{
    public class DifficultyToBackgroundConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string colorString && !string.IsNullOrEmpty(colorString))
            {
                try
                {
                    var color = (Color)ColorConverter.ConvertFromString(colorString);
                    return new SolidColorBrush(color) { Opacity = 0.2 };
                }
                catch
                {
                    return new SolidColorBrush(Colors.Gray) { Opacity = 0.2 };
                }
            }
            return new SolidColorBrush(Colors.Gray) { Opacity = 0.2 };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
