using System;

namespace OptikFormApp.Models
{
    public class ExamEntry
    {
        public int Id { get; set; }
        public int CourseId { get; set; }
        public string Title { get; set; } = string.Empty;
        public DateTime Date { get; set; } = DateTime.Now;
        
        // JSON serialized data for AnswerKeys, QuestionSettings, and LearningOutcomes
        public string ConfigJson { get; set; } = string.Empty;

        public string DisplayDate => Date.ToString("dd.MM.yyyy HH:mm");
    }
}
