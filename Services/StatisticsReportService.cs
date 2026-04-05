using System;
using System.Collections.Generic;
using System.Linq;
using OptikFormApp.Models;

namespace OptikFormApp.Services
{
    public class StatisticsReportService
    {
        public class ClassStatistics
        {
            public double AverageScore { get; set; }
            public double MedianScore { get; set; }
            public double StandardDeviation { get; set; }
            public double MinScore { get; set; }
            public double MaxScore { get; set; }
            public double SuccessRate { get; set; }
            public int TotalStudents { get; set; }
            public int PassedCount { get; set; }
            public int FailedCount { get; set; }
        }

        public class ScoreDistribution
        {
            public string Range { get; set; } = "";
            public int Count { get; set; }
            public double Percentage { get; set; }
        }

        public class TrendAnalysis
        {
            public string Metric { get; set; } = "";
            public double CurrentValue { get; set; }
            public double PreviousValue { get; set; }
            public double Change { get; set; }
            public string Trend { get; set; } = "";
        }

        public class QuestionCorrelation
        {
            public int Question1 { get; set; }
            public int Question2 { get; set; }
            public double Correlation { get; set; }
            public string Relationship { get; set; } = "";
        }

        public ClassStatistics CalculateClassStatistics(List<StudentResult> students, double passingScore = 50)
        {
            if (students == null || students.Count == 0)
                return new ClassStatistics();

            var scores = students.Select(s => s.Score).OrderBy(s => s).ToList();
            var avg = scores.Average();
            var min = scores.Min();
            var max = scores.Max();
            var passed = scores.Count(s => s >= passingScore);

            // Standart sapma
            var variance = scores.Select(s => Math.Pow(s - avg, 2)).Average();
            var stdDev = Math.Sqrt(variance);

            // Medyan
            double median;
            int count = scores.Count;
            if (count % 2 == 0)
                median = (scores[count / 2 - 1] + scores[count / 2]) / 2.0;
            else
                median = scores[count / 2];

            return new ClassStatistics
            {
                AverageScore = Math.Round(avg, 2),
                MedianScore = Math.Round(median, 2),
                StandardDeviation = Math.Round(stdDev, 2),
                MinScore = min,
                MaxScore = max,
                SuccessRate = Math.Round((double)passed / count * 100, 2),
                TotalStudents = count,
                PassedCount = passed,
                FailedCount = count - passed
            };
        }

        public List<ScoreDistribution> GetScoreDistribution(List<StudentResult> students, int binCount = 10)
        {
            if (students == null || students.Count == 0)
                return new List<ScoreDistribution>();

            var maxScore = students.Max(s => s.Score);
            var binSize = maxScore / binCount;
            var total = students.Count;

            var distribution = new List<ScoreDistribution>();
            for (int i = 0; i < binCount; i++)
            {
                var min = i * binSize;
                var max = (i + 1) * binSize;
                var count = students.Count(s => s.Score >= min && s.Score < max || (i == binCount - 1 && s.Score == max));

                distribution.Add(new ScoreDistribution
                {
                    Range = $"{min:F0}-{max:F0}",
                    Count = count,
                    Percentage = Math.Round((double)count / total * 100, 1)
                });
            }

            return distribution;
        }

        public List<QuestionCorrelation> AnalyzeQuestionCorrelations(List<StudentResult> students, int maxQuestions = 50)
        {
            var correlations = new List<QuestionCorrelation>();
            if (students == null || students.Count < 5) return correlations;

            var questionCount = Math.Min(students.First().QuestionResults.Count, maxQuestions);

            // Rastgele örneklem - tüm kombinasyonlar çok yavaş olur
            var random = new Random();
            var samplePairs = new HashSet<(int, int)>();

            while (samplePairs.Count < Math.Min(20, questionCount * (questionCount - 1) / 2))
            {
                var q1 = random.Next(questionCount);
                var q2 = random.Next(questionCount);
                if (q1 != q2)
                {
                    var pair = q1 < q2 ? (q1, q2) : (q2, q1);
                    samplePairs.Add(pair);
                }
            }

            foreach (var (q1, q2) in samplePairs)
            {
                var corr = CalculateCorrelation(students, q1, q2);
                string rel;
                if (Math.Abs(corr) < 0.3) rel = "Zayıf";
                else if (Math.Abs(corr) < 0.7) rel = "Orta";
                else rel = "Güçlü";

                correlations.Add(new QuestionCorrelation
                {
                    Question1 = q1 + 1,
                    Question2 = q2 + 1,
                    Correlation = Math.Round(corr, 3),
                    Relationship = rel
                });
            }

            return correlations.OrderByDescending(c => Math.Abs(c.Correlation)).ToList();
        }

        private double CalculateCorrelation(List<StudentResult> students, int q1, int q2)
        {
            // Index bounds kontrolü
            if (students == null || students.Count == 0)
                return 0;
                
            var firstStudent = students.First();
            if (firstStudent.QuestionResults == null || 
                q1 < 0 || q1 >= firstStudent.QuestionResults.Count ||
                q2 < 0 || q2 >= firstStudent.QuestionResults.Count)
                return 0;

            var x = students.Select(s => 
                s.QuestionResults != null && q1 < s.QuestionResults.Count && s.QuestionResults[q1] ? 1.0 : 0.0
            ).ToList();
            var y = students.Select(s => 
                s.QuestionResults != null && q2 < s.QuestionResults.Count && s.QuestionResults[q2] ? 1.0 : 0.0
            ).ToList();

            var n = x.Count;
            var sumX = x.Sum();
            var sumY = y.Sum();
            var sumXY = x.Zip(y, (a, b) => a * b).Sum();
            var sumX2 = x.Sum(v => v * v);
            var sumY2 = y.Sum(v => v * v);

            var numerator = n * sumXY - sumX * sumY;
            var denominator = Math.Sqrt((n * sumX2 - sumX * sumX) * (n * sumY2 - sumY * sumY));

            return denominator == 0 ? 0 : numerator / denominator;
        }

        public List<TrendAnalysis> CompareExams(List<StudentResult> current, List<StudentResult> previous)
        {
            var trends = new List<TrendAnalysis>();
            if (current == null || previous == null || current.Count == 0 || previous.Count == 0)
                return trends;

            var currentAvg = current.Average(s => s.Score);
            var previousAvg = previous.Average(s => s.Score);
            var avgChange = currentAvg - previousAvg;

            trends.Add(new TrendAnalysis
            {
                Metric = "Sınıf Ortalaması",
                CurrentValue = Math.Round(currentAvg, 2),
                PreviousValue = Math.Round(previousAvg, 2),
                Change = Math.Round(avgChange, 2),
                Trend = avgChange > 0 ? "📈 Artış" : avgChange < 0 ? "📉 Düşüş" : "➡️ Sabit"
            });

            var currentNet = current.Average(s => s.NetCount);
            var previousNet = previous.Average(s => s.NetCount);
            var netChange = currentNet - previousNet;

            trends.Add(new TrendAnalysis
            {
                Metric = "Ortalama Net",
                CurrentValue = Math.Round(currentNet, 2),
                PreviousValue = Math.Round(previousNet, 2),
                Change = Math.Round(netChange, 2),
                Trend = netChange > 0 ? "📈 Artış" : netChange < 0 ? "📉 Düşüş" : "➡️ Sabit"
            });

            var currentSuccess = current.Count(s => s.Score >= 50);
            var prevSuccess = previous.Count(s => s.Score >= 50);
            var successRateChange = ((double)currentSuccess / current.Count - (double)prevSuccess / previous.Count) * 100;

            trends.Add(new TrendAnalysis
            {
                Metric = "Başarı Oranı (%)",
                CurrentValue = Math.Round((double)currentSuccess / current.Count * 100, 1),
                PreviousValue = Math.Round((double)prevSuccess / previous.Count * 100, 1),
                Change = Math.Round(successRateChange, 1),
                Trend = successRateChange > 0 ? "📈 Artış" : successRateChange < 0 ? "📉 Düşüş" : "➡️ Sabit"
            });

            return trends;
        }

        public Dictionary<string, double> CalculatePercentiles(List<StudentResult> students)
        {
            var percentiles = new Dictionary<string, double>();
            if (students == null || students.Count == 0) return percentiles;

            var sorted = students.Select(s => s.Score).OrderBy(s => s).ToList();
            var count = sorted.Count;

            percentiles["%10 (Üst)"] = sorted[(int)(count * 0.9)];
            percentiles["%25 (Q1)"] = sorted[(int)(count * 0.75)];
            percentiles["%50 (Medyan)"] = sorted[(int)(count * 0.5)];
            percentiles["%75 (Q3)"] = sorted[(int)(count * 0.25)];
            percentiles["%90 (Alt)"] = sorted[(int)(count * 0.1)];

            return percentiles;
        }

        public string GenerateReportSummary(List<StudentResult> students, string examName = "")
        {
            if (students == null || students.Count == 0)
                return "Veri yok.";

            var stats = CalculateClassStatistics(students);
            var dist = GetScoreDistribution(students, 5);
            var percentiles = CalculatePercentiles(students);

            var report = $"""
=== SINAV İSTATİSTİK RAPORU ===
Sınav: {examName}
Tarih: {DateTime.Now:dd.MM.yyyy HH:mm}

GENEL BİLGİLER:
• Toplam Öğrenci: {stats.TotalStudents}
• Ortalama Puan: {stats.AverageScore:F2}
• Medyan: {stats.MedianScore:F2}
• Standart Sapma: {stats.StandardDeviation:F2}
• Min/Max: {stats.MinScore:F0} / {stats.MaxScore:F0}
• Başarı Oranı: %{stats.SuccessRate:F1} ({stats.PassedCount} geçen / {stats.FailedCount} kalan)

YÜZDELİKLER:
• Üst %10: {percentiles.GetValueOrDefault("%10 (Üst)", 0):F1}
• Üst %25: {percentiles.GetValueOrDefault("%25 (Q1)", 0):F1}
• Medyan: {percentiles.GetValueOrDefault("%50 (Medyan)", 0):F1}
• Alt %25: {percentiles.GetValueOrDefault("%75 (Q3)", 0):F1}
• Alt %10: {percentiles.GetValueOrDefault("%90 (Alt)", 0):F1}

PUAN DAĞILIMI:
""";

            foreach (var d in dist)
            {
                report += $"\n• {d.Range}: {d.Count} kişi (%{d.Percentage})";
            }

            return report;
        }
    }
}
