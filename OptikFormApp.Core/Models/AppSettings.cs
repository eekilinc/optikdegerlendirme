namespace OptikFormApp.Core.Models;

public class AppSettings
{
    public string DatabasePath { get; set; } = "optik.db";
    public string BackupPath { get; set; } = "backups";
    public int MaxStudents { get; set; } = 5000;
    public bool AutoBackup { get; set; } = true;
    public int BackupIntervalHours { get; set; } = 24;
    public string DefaultLanguage { get; set; } = "tr-TR";
    public bool EnableLogging { get; set; } = true;
    public string LogLevel { get; set; } = "Information";
}
