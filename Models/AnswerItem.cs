namespace OptikFormApp.Models
{
    public enum AnswerState 
    { 
        NotEvaluated, 
        Correct, 
        Incorrect, 
        Empty 
    }

    public class AnswerItem
    {
        public char Character { get; set; }
        public AnswerState State { get; set; }
        public int QuestionNumber { get; set; }
    }
}
