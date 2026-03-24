using System.Collections.Generic;

namespace OptikFormApp.Models
{
    public enum ValidationSeverity { Warning, Error }

    public class ValidationIssue
    {
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public ValidationSeverity Severity { get; set; }
        public string AffectedItems { get; set; } = string.Empty;
    }
}
