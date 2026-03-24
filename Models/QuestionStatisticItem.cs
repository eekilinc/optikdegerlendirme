namespace OptikFormApp.Models
{
    public class QuestionStatisticItem
    {
        public string Booklet { get; set; } = string.Empty;
        public int QuestionNumber { get; set; }
        public string CorrectAnswer { get; set; } = string.Empty;
        
        public double CorrectPercent { get; set; }
        public double IncorrectPercent { get; set; }
        public double EmptyPercent { get; set; }

        public int CountA { get; set; }
        public int CountB { get; set; }
        public int CountC { get; set; }
        public int CountD { get; set; }
        public int CountE { get; set; }
        public int CountEmpty { get; set; }
    }
}
