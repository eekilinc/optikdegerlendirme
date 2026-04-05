using System;

namespace OptikFormApp.Models
{
    public class ExamInfo
    {
        public int Id { get; set; }
        public string Title { get; set; } = "";
        public int CourseId { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}
