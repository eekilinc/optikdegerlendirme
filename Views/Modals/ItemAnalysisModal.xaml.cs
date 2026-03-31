using System;
using System.Windows;
using System.Windows.Controls;
using OptikFormApp.ViewModels;

namespace OptikFormApp.Views.Modals
{
    public partial class ItemAnalysisModal : UserControl
    {
        public ItemAnalysisModal()
        {
            InitializeComponent();
        }
    }

    // Count to Visibility Converter
    public class CountToVisibilityConverter : System.Windows.Data.IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is int count)
                return count == 0 ? Visibility.Visible : Visibility.Collapsed;
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
