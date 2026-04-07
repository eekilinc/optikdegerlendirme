using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using OptikFormApp.Models;

namespace OptikFormApp.ViewModels
{
    public class SeverityToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is ValidationSeverity severity)
            {
                return severity switch
                {
                    ValidationSeverity.Error => new SolidColorBrush(Color.FromRgb(239, 68, 68)), // Red-500
                    ValidationSeverity.Warning => new SolidColorBrush(Color.FromRgb(245, 158, 11)), // Amber-500
                    _ => new SolidColorBrush(Color.FromRgb(107, 114, 128)) // Gray-500
                };
            }
            return new SolidColorBrush(Color.FromRgb(107, 114, 128));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class SeverityToIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is ValidationSeverity severity)
            {
                return severity switch
                {
                    ValidationSeverity.Error => "❌",
                    ValidationSeverity.Warning => "⚠️",
                    _ => "ℹ️"
                };
            }
            return "ℹ️";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
