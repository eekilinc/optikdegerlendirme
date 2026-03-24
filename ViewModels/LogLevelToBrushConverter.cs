using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using OptikFormApp.Models;

namespace OptikFormApp.ViewModels
{
    public class LogLevelToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is LogLevel level)
            {
                return level switch
                {
                    LogLevel.Success => new SolidColorBrush(Color.FromRgb(16, 185, 129)), // Green
                    LogLevel.Warning => new SolidColorBrush(Color.FromRgb(245, 158, 11)), // Amber
                    LogLevel.Error => new SolidColorBrush(Color.FromRgb(239, 68, 68)),   // Red
                    _ => new SolidColorBrush(Color.FromRgb(100, 116, 139))               // Slate
                };
            }
            return Brushes.Black;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }
}
