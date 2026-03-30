using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows.Data;
using OptikFormApp.Models;

namespace OptikFormApp.ViewModels
{
    public class DifficultyCountConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is ObservableCollection<QuestionDifficulty> difficulties && parameter is string difficultyLevel)
            {
                return difficulties.Count(q => q.DifficultyLevel == difficultyLevel.ToString());
            }
            return 0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
