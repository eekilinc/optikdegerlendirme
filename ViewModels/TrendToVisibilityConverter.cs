using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace OptikFormApp.ViewModels
{
    public class TrendToVisibilityConverter : IValueConverter
    {
        public string Mode { get; set; } = "Positive"; // Positive, Negative, Zero

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double val)
            {
                if (Mode == "Positive") return val > 0.1 ? Visibility.Visible : Visibility.Collapsed;
                if (Mode == "Negative") return val < -0.1 ? Visibility.Visible : Visibility.Collapsed;
                if (Mode == "Zero") return Math.Abs(val) <= 0.1 ? Visibility.Visible : Visibility.Collapsed;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class NumericToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int i && i > 0) return Visibility.Visible;
            if (value is double d && d > 0) return Visibility.Visible;
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
