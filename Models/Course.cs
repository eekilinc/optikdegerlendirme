namespace OptikFormApp.Models
{
    public class Course
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        
        // Display property
        public string DisplayName => string.IsNullOrWhiteSpace(Code) ? Name : $"[{Code}] {Name}";
    }
}
