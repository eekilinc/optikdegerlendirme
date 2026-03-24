using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using OptikFormApp.Models;

namespace OptikFormApp.ViewModels
{
    public class AnswerStateToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is AnswerState state)
            {
                return state switch
                {
                    AnswerState.Correct => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#10B981")), // Green
                    AnswerState.Incorrect => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EF4444")), // Red
                    AnswerState.Empty => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#94A3B8")), // Slate/Gray
                    _ => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1E293B")) // Not Evaluated (Dark)
                };
            }
            return Brushes.Black;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }
}
