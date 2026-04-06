using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace OptikFormApp.Models
{
    /// <summary>
    /// Tek bir soru-cevap çifti için detaylı model
    /// </summary>
    public class AnswerDetailItem : INotifyPropertyChanged
    {
        private int _questionNumber;
        private string _answer = string.Empty;
        private int _correctCount;
        private int _wrongCount;
        private int _emptyCount;
        private double _difficultyIndex;

        public int QuestionNumber
        {
            get => _questionNumber;
            set { _questionNumber = value; OnPropertyChanged(); }
        }

        public string Answer
        {
            get => _answer;
            set { _answer = value; OnPropertyChanged(); }
        }

        public int CorrectCount
        {
            get => _correctCount;
            set { _correctCount = value; OnPropertyChanged(); }
        }

        public int WrongCount
        {
            get => _wrongCount;
            set { _wrongCount = value; OnPropertyChanged(); }
        }

        public int EmptyCount
        {
            get => _emptyCount;
            set { _emptyCount = value; OnPropertyChanged(); }
        }

        public double DifficultyIndex
        {
            get => _difficultyIndex;
            set { _difficultyIndex = value; OnPropertyChanged(); }
        }

        public string DisplayStats => $"✓{CorrectCount} ✗{WrongCount} ○{EmptyCount}";

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class AnswerKeyModel : INotifyPropertyChanged
    {
        private string _bookletName = string.Empty;
        public string BookletName
        {
            get => _bookletName;
            set { _bookletName = value; OnPropertyChanged(); }
        }

        private string _answers = string.Empty;
        public string Answers
        {
            get => _answers;
            set 
            { 
                _answers = value; 
                OnPropertyChanged();
                SyncDetailsFromAnswers();
            }
        }

        /// <summary>
        /// Detaylı cevap listesi - soru numarası ve cevap
        /// </summary>
        public ObservableCollection<AnswerDetailItem> AnswerDetails { get; set; } = new();

        /// <summary>
        /// Answers string'inden AnswerDetails listesini senkronize et
        /// </summary>
        public void SyncDetailsFromAnswers()
        {
            AnswerDetails.Clear();
            for (int i = 0; i < _answers.Length; i++)
            {
                AnswerDetails.Add(new AnswerDetailItem
                {
                    QuestionNumber = i + 1,
                    Answer = _answers[i].ToString().ToUpper()
                });
            }
        }

        /// <summary>
        /// AnswerDetails listesinden Answers string'ini güncelle
        /// </summary>
        public void SyncAnswersFromDetails()
        {
            _answers = string.Join("", AnswerDetails.Select(d => d.Answer));
            OnPropertyChanged(nameof(Answers));
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
