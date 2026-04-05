namespace OptikFormApp.Models;

public class AppSettings
{
    public string DatabasePath { get; set; } = "optik.db";
    public string BackupPath { get; set; } = "backups";
    public int MaxStudents { get; set; } = 1000;
    public int MaxQuestions { get; set; } = 50;
    public string DefaultLanguage { get; set; } = "tr-TR";
    public bool AutoSave { get; set; } = true;
    public int AutoSaveInterval { get; set; } = 5;
    public bool EnableLogging { get; set; } = true;
    public string LogLevel { get; set; } = "Information";
    
    // Additional properties used by MainViewModel
    public string SchoolName { get; set; } = "AĞLASUN MYO";
    public string DefaultExcelPath { get; set; } = "";
    public double NetCoefficient { get; set; } = 0.5;
    public double BaseScore { get; set; } = 50.0;
    public double WrongDeductionFactor { get; set; } = 0.25;
    public int ThemeIndex { get; set; } = 0;
    public int LayoutIndex { get; set; } = 0;
    public int FontSize { get; set; } = 12;
}
