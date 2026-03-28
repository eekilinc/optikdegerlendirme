using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace OptikFormApp.ViewModels
{
    public class CountToVisibilityConverter : IValueConverter
    {
        public bool Invert { get; set; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int count)
            {
                bool isEmpty = count == 0;
                if (Invert) return isEmpty ? Visibility.Collapsed : Visibility.Visible;
                return isEmpty ? Visibility.Visible : Visibility.Collapsed;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }
}
