using System.Collections.Generic;

namespace OptikFormApp.Models
{
    public class ExamConfigData
    {
        public List<AnswerKeyModel> AnswerKeys { get; set; } = new List<AnswerKeyModel>();
        public List<QuestionSetting> QuestionSettings { get; set; } = new List<QuestionSetting>();
        public List<LearningOutcome> LearningOutcomes { get; set; } = new List<LearningOutcome>();

        public double NetCoefficient { get; set; } = 1.0;
        public double BaseScore { get; set; } = 0.0;
        public double WrongDeductionFactor { get; set; } = 0.25;
        public string SchoolName { get; set; } = string.Empty;
    }
}
