using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace OptikFormApp.Models
{
    public class StudentResult : INotifyPropertyChanged
    {
        private int _rowNumber;
        private string _fullName = string.Empty;
        private string _studentId = string.Empty;
        private string _bookletType = string.Empty;
        private string _rawAnswers = string.Empty;
        private int _correctCount;
        private int _incorrectCount;
        private int _emptyCount;
        private double _netCount;
        private double _score;
        private int _rank;

        public int RowNumber { get => _rowNumber; set { _rowNumber = value; OnPropertyChanged(); } }
        public string FullName { get => _fullName; set { _fullName = value; OnPropertyChanged(); } }
        public string StudentId { get => _studentId; set { _studentId = value; OnPropertyChanged(); } }
        public string BookletType { get => _bookletType; set { _bookletType = value; OnPropertyChanged(); } }
        public string RawAnswers { get => _rawAnswers; set { _rawAnswers = value; OnPropertyChanged(); } }
        
        public ObservableCollection<AnswerItem> ColoredAnswers { get; set; } = new ObservableCollection<AnswerItem>();

        public int CorrectCount { get => _correctCount; set { _correctCount = value; OnPropertyChanged(); } }
        public int IncorrectCount { get => _incorrectCount; set { _incorrectCount = value; OnPropertyChanged(); } }
        public int EmptyCount { get => _emptyCount; set { _emptyCount = value; OnPropertyChanged(); } }
        public double NetCount { get => _netCount; set { _netCount = value; OnPropertyChanged(); } }
        public double Score { get => _score; set { _score = value; OnPropertyChanged(); } }
        public int Rank { get => _rank; set { _rank = value; OnPropertyChanged(); } }
        
        public List<string> Answers { get; set; } = new List<string>();
        public List<bool> QuestionResults { get; set; } = new List<bool>();

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
