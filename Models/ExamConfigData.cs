using System.Collections.Generic;

namespace OptikFormApp.Models
{
    public class ExamConfigData
    {
        public List<AnswerKeyModel> AnswerKeys { get; set; } = new List<AnswerKeyModel>();
        public List<QuestionSetting> QuestionSettings { get; set; } = new List<QuestionSetting>();
        public List<LearningOutcome> LearningOutcomes { get; set; } = new List<LearningOutcome>();
    }
}
