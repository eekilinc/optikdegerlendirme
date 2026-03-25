using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace OptikFormApp.Models
{
    public class StudentResult
    {
        public int RowNumber { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string StudentId { get; set; } = string.Empty;
        public string BookletType { get; set; } = string.Empty;
        
        public string RawAnswers { get; set; } = string.Empty;
        public ObservableCollection<AnswerItem> ColoredAnswers { get; set; } = new ObservableCollection<AnswerItem>();

        public int CorrectCount { get; set; }
        public int IncorrectCount { get; set; }
        public int WrongCount { get; set; }
        public int EmptyCount { get; set; }
        public double NetCount { get; set; }
        public double Score { get; set; }
        public int Rank { get; set; }
        
        public List<string> Answers { get; set; } = new List<string>();
        public List<bool> QuestionResults { get; set; } = new List<bool>();
    }
}
