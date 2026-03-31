using System.Windows;
using System.Windows.Controls;
using System.Globalization;
using System;

namespace OptikFormApp.Views
{
    public partial class ToastNotificationHost : UserControl
    {
        public ToastNotificationHost()
        {
            InitializeComponent();
        }
    }

    public class PercentageToWidthConverter : System.Windows.Data.IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double progress && parameter is string maxWidthStr && double.TryParse(maxWidthStr, out double maxWidth))
            {
                return maxWidth * (progress / 100.0);
            }
            return 0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
