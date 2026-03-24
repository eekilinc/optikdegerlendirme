using System.Collections.Generic;

namespace OptikFormApp.Models
{
    public class QuestionSetting
    {
        public string BookletName { get; set; } = string.Empty;
        public int QuestionNumber { get; set; }
        public bool IsCancelled { get; set; }
        public bool IsMultipleEnabled { get; set; }
        public string MultipleCorrectAnswers { get; set; } = string.Empty; // e.g. "AB"
        
        public bool IsCorrect(char studentAnswer)
        {
            if (IsCancelled) return true;
            if (!IsMultipleEnabled || string.IsNullOrEmpty(MultipleCorrectAnswers)) return false;
            return MultipleCorrectAnswers.Contains(studentAnswer.ToString());
        }
    }
}
