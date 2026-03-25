namespace OptikFormApp.Models
{
    public class AppSettings
    {
        public string SchoolName { get; set; } = "Okul Adı";
        public string DefaultExcelPath { get; set; } = "";
        public double NetCoefficient { get; set; } = 1.0;
        public double BaseScore { get; set; } = 0.0;
        public double WrongDeductionFactor { get; set; } = 0.25;
        public int ThemeIndex { get; set; } = 0;
        public int LayoutIndex { get; set; } = 0;
    }
}
