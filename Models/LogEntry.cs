using System;

namespace OptikFormApp.Models
{
    public enum LogLevel { Info, Warning, Error, Success }
    public class LogEntry
    {
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public string Message { get; set; } = string.Empty;
        public LogLevel Level { get; set; } = LogLevel.Info;
        
        public string DisplayTime => Timestamp.ToString("HH:mm:ss");
    }
}
