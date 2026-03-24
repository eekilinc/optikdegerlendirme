using System;
using System.Collections.Generic;

namespace OptikFormApp.Models
{
    public class StudentResult
    {
        public string FullName { get; set; } = string.Empty;
        public string StudentId { get; set; } = string.Empty;
        public string BookletType { get; set; } = string.Empty;
        
        // Raw sequence of characters representing chosen options (A, B, C, D, E, or Space ' ' for empty)
        public string RawAnswers { get; set; } = string.Empty;

        // Evaluation Results
        public int CorrectCount { get; set; }
        public int IncorrectCount { get; set; }
        public int EmptyCount { get; set; }
        public double Score { get; set; }
        
        // For item analysis, you might want to store boolean true/false per question
        public List<bool> QuestionResults { get; set; } = new List<bool>();
    }
}
