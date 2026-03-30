using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace OptikFormApp.Models
{
    public class QuestionDifficulty : INotifyPropertyChanged
    {
        private int _questionNumber;
        private double _successRate;
        private string _difficultyLevel = "";
        private int _correctAnswers;
        private int _totalAnswers;

        public int QuestionNumber
        {
            get => _questionNumber;
            set { _questionNumber = value; OnPropertyChanged(); }
        }

        public double SuccessRate
        {
            get => _successRate;
            set { _successRate = value; OnPropertyChanged(); }
        }

        public string DifficultyLevel
        {
            get => _difficultyLevel;
            set { _difficultyLevel = value; OnPropertyChanged(); }
        }

        public int CorrectAnswers
        {
            get => _correctAnswers;
            set { _correctAnswers = value; OnPropertyChanged(); }
        }

        public int TotalAnswers
        {
            get => _totalAnswers;
            set { _totalAnswers = value; OnPropertyChanged(); }
        }

        public string DifficultyColor
        {
            get
            {
                return SuccessRate switch
                {
                    >= 80 => "#10B981", // Green - Easy
                    >= 60 => "#F59E0B", // Amber - Medium
                    >= 40 => "#EF4444", // Red - Hard
                    _ => "#6B7280"  // Gray - Very Hard
                };
            }
        }

        public string DifficultyIcon
        {
            get
            {
                return SuccessRate switch
                {
                    >= 80 => "🟢", // Green - Easy
                    >= 60 => "🟡", // Amber - Medium
                    >= 40 => "🔴", // Red - Hard
                    _ => "⚫"  // Gray - Very Hard
                };
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
