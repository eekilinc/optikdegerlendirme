using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace OptikFormApp.Models
{
    public class LearningOutcome : INotifyPropertyChanged
    {
        private string _name = string.Empty;
        public string Name 
        { 
            get => _name; 
            set { _name = value; OnPropertyChanged(); } 
        }
        
        private string _bookletName = "A";
        public string BookletName 
        { 
            get => _bookletName; 
            set { _bookletName = value; OnPropertyChanged(); } 
        }
        
        private string _questionNumbersRaw = string.Empty;
        public string QuestionNumbersRaw 
        { 
            get => _questionNumbersRaw; 
            set 
            { 
                _questionNumbersRaw = value; 
                ParseQuestionNumbers(); 
                OnPropertyChanged();
            } 
        }

        public List<int> QuestionNumbers { get; set; } = new List<int>();
        
        private double _successRate;
        public double SuccessRate { get => _successRate; set { _successRate = value; OnPropertyChanged(); } }
        
        private int _correctCount;
        public int CorrectCount { get => _correctCount; set { _correctCount = value; OnPropertyChanged(); } }
        
        private int _totalQuestions;
        public int TotalQuestions { get => _totalQuestions; set { _totalQuestions = value; OnPropertyChanged(); } }

        private double _globalSuccessRate;
        public double GlobalSuccessRate { get => _globalSuccessRate; set { _globalSuccessRate = value; OnPropertyChanged(); } }

        private int _globalCorrectCount;
        public int GlobalCorrectCount { get => _globalCorrectCount; set { _globalCorrectCount = value; OnPropertyChanged(); } }

        private int _globalTotalQuestions;
        public int GlobalTotalQuestions { get => _globalTotalQuestions; set { _globalTotalQuestions = value; OnPropertyChanged(); } }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private void ParseQuestionNumbers()
        {
            QuestionNumbers.Clear();
            if (string.IsNullOrWhiteSpace(_questionNumbersRaw)) return;

            var parts = _questionNumbersRaw.Split(new[] { ',', ';' }, System.StringSplitOptions.RemoveEmptyEntries);
            foreach (var part in parts)
            {
                var p = part.Trim();
                if (p.Contains("-"))
                {
                    var range = p.Split('-');
                    if (range.Length == 2 && int.TryParse(range[0], out int start) && int.TryParse(range[1], out int end))
                    {
                        for (int i = start; i <= end; i++) QuestionNumbers.Add(i);
                    }
                }
                else if (int.TryParse(p, out int num))
                {
                    QuestionNumbers.Add(num);
                }
            }
        }
    }
}
