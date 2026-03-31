using System;
using System.Collections.Generic;
using System.Linq;
using OptikFormApp.Models;

namespace OptikFormApp.Services
{
    /// <summary>
    /// Öğrenci başarı tahmini servisi - Makine öğrenmesi tabanlı başarı tahminleri
    /// </summary>
    public class SuccessPredictionService
    {
        public class PredictionResult
        {
            public string StudentId { get; set; } = "";
            public string FullName { get; set; } = "";
            public double CurrentScore { get; set; }
            public double PredictedScore { get; set; }
            public double Confidence { get; set; } // 0-1 arası
            public RiskLevel Risk { get; set; }
            public string RiskDescription { get; set; } = "";
            public List<string> Recommendations { get; set; } = new();
            public double GradeProbability { get; set; } // Geçme olasılığı
        }

        public class ClassPredictionSummary
        {
            public int TotalStudents { get; set; }
            public int HighRiskCount { get; set; }
            public int MediumRiskCount { get; set; }
            public int LowRiskCount { get; set; }
            public double AveragePredictedScore { get; set; }
            public double PassRate { get; set; }
            public List<string> ClassRecommendations { get; set; } = new();
        }

        public enum RiskLevel
        {
            Low,      // Düşük risk - Başarılı
            Medium,   // Orta risk - Dikkat
            High      // Yüksek risk - Başarısız olma olasılığı yüksek
        }

        /// <summary>
        /// Öğrenci başarı tahmini yap
        /// </summary>
        public PredictionResult PredictStudentSuccess(
            StudentResult student,
            List<StudentResult> allStudents,
            double passingScore = 50)
        {
            if (allStudents.Count < 2)
                return CreateDefaultPrediction(student);

            var stats = CalculateClassStatistics(allStudents);
            
            // Çoklu faktör analizi
            var factors = AnalyzeSuccessFactors(student, stats);
            
            // Tahmin skoru hesapla (0-100)
            double predictedScore = CalculatePredictedScore(student, factors, stats);
            
            // Güven seviyesi hesapla
            double confidence = CalculateConfidence(student, allStudents.Count);
            
            // Risk seviyesi belirle
            var risk = DetermineRiskLevel(predictedScore, passingScore, confidence);
            
            // Geçme olasılığı hesapla
            double passProbability = CalculatePassProbability(predictedScore, passingScore, confidence);

            return new PredictionResult
            {
                StudentId = student.StudentId,
                FullName = student.FullName,
                CurrentScore = student.Score,
                PredictedScore = Math.Round(predictedScore, 2),
                Confidence = Math.Round(confidence, 2),
                Risk = risk,
                RiskDescription = GetRiskDescription(risk, predictedScore),
                Recommendations = GenerateRecommendations(student, factors, risk),
                GradeProbability = Math.Round(passProbability, 2)
            };
        }

        /// <summary>
        /// Tüm sınıf için başarı tahmini özeti
        /// </summary>
        public ClassPredictionSummary PredictClassSuccess(
            List<StudentResult> students,
            double passingScore = 50)
        {
            if (students.Count == 0)
                return new ClassPredictionSummary();

            var predictions = students
                .Select(s => PredictStudentSuccess(s, students, passingScore))
                .ToList();

            var summary = new ClassPredictionSummary
            {
                TotalStudents = students.Count,
                HighRiskCount = predictions.Count(p => p.Risk == RiskLevel.High),
                MediumRiskCount = predictions.Count(p => p.Risk == RiskLevel.Medium),
                LowRiskCount = predictions.Count(p => p.Risk == RiskLevel.Low),
                AveragePredictedScore = Math.Round(predictions.Average(p => p.PredictedScore), 2),
                PassRate = Math.Round(predictions.Count(p => p.GradeProbability > 0.7) / (double)students.Count, 2)
            };

            // Sınıf seviyesinde öneriler
            if (summary.HighRiskCount > summary.TotalStudents * 0.3)
                summary.ClassRecommendations.Add("⚠️ Sınıfın %30'undan fazlası yüksek riskli - Ek destek gerekli");
            
            if (summary.AveragePredictedScore < passingScore)
                summary.ClassRecommendations.Add("📚 Sınıf ortalaması hedefin altında - Genel tekrar önerilir");
            
            if (summary.PassRate < 0.8)
                summary.ClassRecommendations.Add("🎯 Geçme oranı düşük - Ek çalışma programı düşünülmeli");

            return summary;
        }

        /// <summary>
        /// Başarı faktörlerini analiz et
        /// </summary>
        private SuccessFactors AnalyzeSuccessFactors(StudentResult student, ClassStats stats)
        {
            return new SuccessFactors
            {
                // Performans faktörü (puan bazlı)
                PerformanceFactor = Normalize(student.Score, 0, 100),
                
                // Başarı oranı (doğru/yanlış oranı)
                SuccessRate = student.CorrectCount + student.IncorrectCount > 0
                    ? student.CorrectCount / (double)(student.CorrectCount + student.IncorrectCount)
                    : 0,
                
                // Sınıf içi sıralama faktörü
                RankingFactor = stats.TotalStudents > 0
                    ? 1 - (student.Rank / (double)stats.TotalStudents)
                    : 0.5,
                
                // Net katsayısı etkisi
                NetFactor = Math.Min(student.NetCount / stats.MaxNet, 1),
                
                // Boş bırakma oranı (ters ilişki - çok boş = risk)
                EmptyRate = student.Answers.Count > 0
                    ? 1 - (student.EmptyCount / (double)student.Answers.Count)
                    : 0
            };
        }

        /// <summary>
        /// Tahmini skor hesapla
        /// </summary>
        private double CalculatePredictedScore(StudentResult student, SuccessFactors factors, ClassStats stats)
        {
            // Ağırlıklı faktör kombinasyonu
            double weightedScore = 
                factors.PerformanceFactor * 0.35 +
                factors.SuccessRate * 0.25 +
                factors.RankingFactor * 0.20 +
                factors.NetFactor * 0.15 +
                factors.EmptyRate * 0.05;

            // Mevcut puana regresyon uygula
            double regression = stats.MeanScore * 0.3 + student.Score * 0.7;
            
            // Son tahmin
            double prediction = weightedScore * 100 * 0.6 + regression * 0.4;
            
            // Sınırla (0-100)
            return Math.Max(0, Math.Min(100, prediction));
        }

        /// <summary>
        /// Güven seviyesi hesapla
        /// </summary>
        private double CalculateConfidence(StudentResult student, int totalStudents)
        {
            // Daha fazla veri = daha yüksek güven
            double dataConfidence = Math.Min(totalStudents / 100.0, 1.0);
            
            // Tamamlanmış cevap sayısı etkisi
            double completionRate = student.Answers.Count > 0
                ? (student.CorrectCount + student.IncorrectCount) / (double)student.Answers.Count
                : 0;
            
            // Güven = veri miktarı * tamamlama oranı
            return Math.Max(0.3, dataConfidence * 0.7 + completionRate * 0.3);
        }

        /// <summary>
        /// Risk seviyesi belirle
        /// </summary>
        private RiskLevel DetermineRiskLevel(double predictedScore, double passingScore, double confidence)
        {
            double riskScore = predictedScore * confidence;
            
            if (riskScore < passingScore * 0.8)
                return RiskLevel.High;
            else if (riskScore < passingScore * 1.1)
                return RiskLevel.Medium;
            else
                return RiskLevel.Low;
        }

        /// <summary>
        /// Geçme olasılığı hesapla
        /// </summary>
        private double CalculatePassProbability(double predictedScore, double passingScore, double confidence)
        {
            double gap = predictedScore - passingScore;
            double normalizedGap = gap / 20.0; // 20 puan = 1 standart sapma
            
            // Sigmoid fonksiyonu ile olasılık hesapla
            double probability = 1 / (1 + Math.Exp(-normalizedGap * confidence));
            
            return probability;
        }

        /// <summary>
        /// Risk açıklaması getir
        /// </summary>
        private string GetRiskDescription(RiskLevel risk, double predictedScore)
        {
            return risk switch
            {
                RiskLevel.High => $"⚠️ Yüksek Risk - Tahmini başarısızlık (Puan: {predictedScore:F1})",
                RiskLevel.Medium => $"⚡ Orta Risk - Dikkat gerektiriyor (Puan: {predictedScore:F1})",
                RiskLevel.Low => $"✅ Düşük Risk - Başarılı olması muhtemel (Puan: {predictedScore:F1})",
                _ => "Bilinmiyor"
            };
        }

        /// <summary>
        /// Öneriler oluştur
        /// </summary>
        private List<string> GenerateRecommendations(StudentResult student, SuccessFactors factors, RiskLevel risk)
        {
            var recommendations = new List<string>();

            if (risk == RiskLevel.High)
            {
                recommendations.Add("🎯 Acil destek alması önerilir");
                recommendations.Add("📚 Temel konularda tekrar yapmalı");
            }
            else if (risk == RiskLevel.Medium)
            {
                recommendations.Add("⚡ Düzenli takip faydalı olacaktır");
                recommendations.Add("📖 Zayıf olduğu konulara odaklanmalı");
            }

            if (factors.EmptyRate < 0.5)
                recommendations.Add("✏️ Daha fazla soru çözmeli - Boş bırakma oranı yüksek");

            if (factors.SuccessRate < 0.6)
                recommendations.Add("🔍 Yanlış cevapları incelemeli - Başarı oranı düşük");

            if (student.Score < 60)
                recommendations.Add("💪 Daha fazla pratik yapmalı - Puan hedefin altında");

            return recommendations;
        }

        /// <summary>
        /// Sınıf istatistiklerini hesapla
        /// </summary>
        private ClassStats CalculateClassStatistics(List<StudentResult> students)
        {
            if (students.Count == 0)
                return new ClassStats();

            var scores = students.Select(s => s.Score).ToList();
            var nets = students.Select(s => s.NetCount).ToList();

            return new ClassStats
            {
                TotalStudents = students.Count,
                MeanScore = scores.Average(),
                MaxScore = scores.Max(),
                MinScore = scores.Min(),
                MaxNet = nets.Max()
            };
        }

        /// <summary>
        /// Varsayılan tahmin oluştur (yetersiz veri durumu)
        /// </summary>
        private PredictionResult CreateDefaultPrediction(StudentResult student)
        {
            return new PredictionResult
            {
                StudentId = student.StudentId,
                FullName = student.FullName,
                CurrentScore = student.Score,
                PredictedScore = student.Score,
                Confidence = 0.3,
                Risk = RiskLevel.Medium,
                RiskDescription = "📊 Yetersiz veri - Tahmin güvenilir değil",
                Recommendations = new List<string> { "Daha fazla veri gerekli" },
                GradeProbability = student.Score >= 50 ? 0.6 : 0.4
            };
        }

        private double Normalize(double value, double min, double max)
        {
            if (max == min) return 0.5;
            return Math.Max(0, Math.Min(1, (value - min) / (max - min)));
        }

        private class SuccessFactors
        {
            public double PerformanceFactor { get; set; }
            public double SuccessRate { get; set; }
            public double RankingFactor { get; set; }
            public double NetFactor { get; set; }
            public double EmptyRate { get; set; }
        }

        private class ClassStats
        {
            public int TotalStudents { get; set; }
            public double MeanScore { get; set; }
            public double MaxScore { get; set; }
            public double MinScore { get; set; }
            public double MaxNet { get; set; }
        }
    }
}
