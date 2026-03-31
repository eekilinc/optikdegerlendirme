using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace OptikFormApp.Views.Modals
{
    public partial class AdvancedFilterModal : UserControl
    {
        public AdvancedFilterModal()
        {
            InitializeComponent();
        }
    }

    // Sort Direction Converter (bool to arrow symbol)
    public class SortDirectionConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is true ? "↓ Azalan" : "↑ Artan";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    // Boolean to Visibility Converter
    public class BooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is true ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
