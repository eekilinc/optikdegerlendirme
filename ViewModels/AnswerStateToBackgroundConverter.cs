using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using OptikFormApp.Models;

namespace OptikFormApp.ViewModels
{
    public class AnswerStateToBackgroundConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is AnswerState state)
            {
                return state switch
                {
                    AnswerState.Correct => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D1FAE5")), // Light Green
                    AnswerState.Incorrect => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FEE2E2")), // Light Red
                    AnswerState.Empty => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F1F5F9")), // Light Slate
                    _ => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFFFF")) // White
                };
            }
            return Brushes.White;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }
}
