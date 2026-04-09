using System;
using System.Collections.Generic;
using System.Linq;
using OptikFormApp.Models;

namespace OptikFormApp.Services
{
    /// <summary>
    /// Gelişmiş soru analizi ve sınav güvenilirlik istatistikleri
    /// </summary>
    public class ItemAnalysisService
    {
        /// <summary>
        /// Soru başına istatistiksel analiz
        /// </summary>
        public record QuestionItemStats(
            int QuestionNumber,
            int TotalStudents,
            int CorrectCount,
            int WrongCount,
            int EmptyCount,
            double DifficultyIndex,      // 0-1 arası, 1=kolay
            double DiscriminationIndex,  // -1 ile 1 arası, >0.3 iyi
            double PointBiserial,        // Korelasyon katsayısı
            double[] OptionDistribution  // A,B,C,D,E, Boş dağılımı
        );

        /// <summary>
        /// Sınav güvenilirlik istatistikleri
        /// </summary>
        public record ReliabilityStats(
            double KR20,                    // Kuder-Richardson Formula 20
            double CronbachAlpha,           // Cronbach's Alpha
            double StandardError,           // Standart hata
            double MeanScore,
            double StandardDeviation,
            double Variance,
            int TotalQuestions,
            int TotalStudents
        );

        /// <summary>
        /// Öğrenci anomali tespiti için sonuç
        /// </summary>
        public record AnomalyResult(
            string StudentId,
            string StudentName,
            AnomalyType Type,
            double Severity, // 0-1 arası
            string Description,
            double[] SuspiciousPattern // Hangi sorularda şüpheli
        );

        public enum AnomalyType
        {
            UnusualAnswerPattern,   // Beklenmedik cevap kalıbı
            ScoreInconsistency,     // Puan tutarsızlığı
            TimeAnomaly,           // Süre anomalisi (varsa)
            CopySuspicion          // Kopya şüphesi
        }

        /// <summary>
        /// Tüm sorular için item analizi hesapla
        /// </summary>
        public List<QuestionItemStats> AnalyzeQuestions(
            List<StudentResult> students, 
            string[] answerKey,
            int questionCount)
        {
            var results = new List<QuestionItemStats>();
            
            if (students.Count == 0 || questionCount == 0)
                return results;

            for (int q = 0; q < questionCount; q++)
            {
                var stats = CalculateQuestionStats(students, answerKey, q);
                results.Add(stats);
            }

            return results;
        }

        private QuestionItemStats CalculateQuestionStats(
            List<StudentResult> students, 
            string[] answerKey, 
            int questionIndex)
        {
            int total = students.Count;
            int correct = 0, wrong = 0, empty = 0;
            var optionCounts = new double[6]; // A,B,C,D,E, Boş
            
            var studentScores = new List<(double score, bool correct)>();

            foreach (var student in students)
            {
                if (student.Answers == null || questionIndex >= student.Answers.Count)
                    continue;

                string answer = student.Answers[questionIndex]?.ToString().ToUpper() ?? "";
                string correctAnswer = answerKey[questionIndex]?.ToUpper() ?? "";

                bool isCorrect = answer == correctAnswer && !string.IsNullOrEmpty(answer);
                bool isEmpty = string.IsNullOrEmpty(answer) || answer == " ";

                if (isCorrect) correct++;
                else if (isEmpty) empty++;
                else wrong++;

                // Option dağılımı
                int optionIndex = answer switch
                {
                    "A" => 0,
                    "B" => 1,
                    "C" => 2,
                    "D" => 3,
                    "E" => 4,
                    _ => 5 // Boş
                };
                optionCounts[optionIndex]++;

                studentScores.Add((student.Score, isCorrect));
            }

            // Güçlük indeksi (p) - doğru cevaplayanların oranı
            double difficulty = total > 0 ? (double)correct / total : 0;

            // Ayırt edicilik indeksi (r) - üst %27 ve alt %27 karşılaştırması
            double discrimination = CalculateDiscriminationIndex(studentScores);

            // Point-biserial korelasyon
            double pointBiserial = CalculatePointBiserial(studentScores);

            // Dağılımı normalize et
            var distribution = optionCounts.Select(c => total > 0 ? c / total : 0).ToArray();

            return new QuestionItemStats(
                questionIndex + 1,
                total,
                correct,
                wrong,
                empty,
                difficulty,
                discrimination,
                pointBiserial,
                distribution
            );
        }

        /// <summary>
        /// Ayırt edicilik indeksi - üst %27 ve alt %27 grup karşılaştırması
        /// </summary>
        private double CalculateDiscriminationIndex(List<(double score, bool correct)> studentScores)
        {
            if (studentScores.Count < 30) // Minimum örneklem
                return 0;

            var sorted = studentScores.OrderByDescending(s => s.score).ToList();
            int groupSize = (int)Math.Ceiling(sorted.Count * 0.27);
            
            var topGroup = sorted.Take(groupSize);
            var bottomGroup = sorted.Skip(sorted.Count - groupSize);

            double topCorrect = topGroup.Count(s => s.correct);
            double bottomCorrect = bottomGroup.Count(s => s.correct);

            double topRate = (double)topCorrect / groupSize;
            double bottomRate = (double)bottomCorrect / groupSize;

            return topRate - bottomRate; // -1 ile 1 arası
        }

        /// <summary>
        /// Point-biserial korelasyon katsayısı
        /// </summary>
        private double CalculatePointBiserial(List<(double score, bool correct)> studentScores)
        {
            if (studentScores.Count < 2)
                return 0;

            var correctGroup = studentScores.Where(s => s.correct).Select(s => s.score).ToList();
            var wrongGroup = studentScores.Where(s => !s.correct).Select(s => s.score).ToList();

            if (correctGroup.Count == 0 || wrongGroup.Count == 0)
                return 0;

            double meanCorrect = correctGroup.Average();
            double meanWrong = wrongGroup.Average();
            double totalMean = studentScores.Average(s => s.score);
            double totalStd = CalculateStandardDeviation(studentScores.Select(s => s.score));

            if (totalStd == 0)
                return 0;

            double p = (double)correctGroup.Count / studentScores.Count;
            double q = 1 - p;

            return ((meanCorrect - meanWrong) / totalStd) * Math.Sqrt(p * q);
        }

        /// <summary>
        /// KR-20 (Kuder-Richardson Formula 20) güvenilirlik katsayısı
        /// </summary>
        public ReliabilityStats CalculateReliability(
            List<StudentResult> students, 
            string[] answerKey,
            int questionCount)
        {
            if (students.Count < 2 || questionCount == 0)
                return new ReliabilityStats(0, 0, 0, 0, 0, 0, questionCount, students.Count);

            var scores = students.Select(s => s.Score).ToList();
            double mean = scores.Average();
            double variance = CalculateVariance(scores);
            double stdDev = Math.Sqrt(variance);

            if (variance == 0)
                return new ReliabilityStats(0, 0, 0, mean, stdDev, variance, questionCount, students.Count);

            // Her sorunun varyansı
            double sumPQ = 0;
            var itemStats = AnalyzeQuestions(students, answerKey, questionCount);
            
            foreach (var item in itemStats)
            {
                double p = item.DifficultyIndex;
                double q = 1 - p;
                sumPQ += p * q;
            }

            // KR-20 = (k/(k-1)) * (1 - (sum(p*q)/variance))
            double kr20 = (questionCount / (double)(questionCount - 1)) * (1 - (sumPQ / variance));

            // Cronbach's Alpha (KR-20'ye yakın, ama genel formül)
            double cronbachAlpha = CalculateCronbachAlpha(students, answerKey, questionCount, variance);

            // Standart hata
            double sem = stdDev * Math.Sqrt(1 - Math.Max(0, kr20));

            return new ReliabilityStats(
                Math.Max(0, kr20),
                Math.Max(0, cronbachAlpha),
                sem,
                mean,
                stdDev,
                variance,
                questionCount,
                students.Count
            );
        }

        /// <summary>
        /// Cronbach's Alpha hesapla (KR-20'nin genelleştirilmiş hali)
        /// </summary>
        private double CalculateCronbachAlpha(
            List<StudentResult> students, 
            string[] answerKey,
            int questionCount,
            double totalVariance)
        {
            if (totalVariance == 0 || questionCount < 2)
                return 0;

            // Her sorunun varyansı
            double sumItemVariances = 0;
            
            for (int q = 0; q < questionCount; q++)
            {
                var itemScores = new List<double>();
                
                foreach (var student in students)
                {
                    if (student.Answers != null && q < student.Answers.Count)
                    {
                        string answer = student.Answers[q]?.ToString().ToUpper() ?? "";
                        string correctAnswer = answerKey[q]?.ToUpper() ?? "";
                        double score = (answer == correctAnswer && !string.IsNullOrEmpty(answer)) ? 1 : 0;
                        itemScores.Add(score);
                    }
                }

                if (itemScores.Count > 1)
                    sumItemVariances += CalculateVariance(itemScores);
            }

            double k = questionCount;
            double alpha = (k / (k - 1)) * (1 - (sumItemVariances / totalVariance));
            
            return alpha;
        }

        /// <summary>
        /// Anomali tespiti - şüpheli cevap kalıpları
        /// </summary>
        public List<AnomalyResult> DetectAnomalies(
            List<StudentResult> students, 
            string[] answerKey,
            int questionCount)
        {
            var anomalies = new List<AnomalyResult>();
            
            if (students.Count < 10 || questionCount == 0)
                return anomalies;

            // Z-skorları hesapla
            var scores = students.Select(s => s.Score).ToList();
            double mean = scores.Average();
            double stdDev = CalculateStandardDeviation(scores);

            // Standart sapma çok düşükse Z-skor anlamsız olur, atla
            bool useZScore = stdDev > 5.0; // Minimum 5 puan standart sapma gerekli

            foreach (var student in students)
            {
                // 1. Puan anomalisi (çok düşük veya çok yüksek) - sadece anlamlı stdDev ile
                if (useZScore)
                {
                    double zScore = (student.Score - mean) / stdDev;
                    
                    if (Math.Abs(zScore) > 3.0) // Eşik yükseltildi: 2.5 -> 3.0
                    {
                        anomalies.Add(new AnomalyResult(
                            student.StudentId,
                            student.FullName,
                            AnomalyType.ScoreInconsistency,
                            Math.Min(Math.Abs(zScore) / 4, 1.0), // Normalize edildi
                            zScore > 0 ? "Puan ortalamanın çok üzerinde" : "Puan ortalamanın çok altında",
                            Array.Empty<double>()
                        ));
                    }
                }

                // 2. Cevap kalıbı analizi
                if (student.Answers != null && student.Answers.Count >= questionCount)
                {
                    var pattern = AnalyzeAnswerPattern(student, students, answerKey, questionCount);
                    if (pattern.Severity > 0.7)
                    {
                        anomalies.Add(new AnomalyResult(
                            student.StudentId,
                            student.FullName,
                            AnomalyType.UnusualAnswerPattern,
                            pattern.Severity,
                            pattern.Description,
                            pattern.SuspiciousQuestions
                        ));
                    }

                    // 3. Kopya şüphesi - başka öğrencilerle yüksek benzerlik
                    var copySuspicion = DetectCopySuspicion(student, students, questionCount);
                    if (copySuspicion.Severity > 0.8)
                    {
                        anomalies.Add(new AnomalyResult(
                            student.StudentId,
                            student.FullName,
                            AnomalyType.CopySuspicion,
                            copySuspicion.Severity,
                            copySuspicion.Description,
                            copySuspicion.SuspiciousQuestions
                        ));
                    }
                }
            }

            return anomalies.OrderByDescending(a => a.Severity).ToList();
        }

        /// <summary>
        /// Cevap kalıbı analizi - rasgele cevaplama tespiti
        /// </summary>
        private (double Severity, string Description, double[] SuspiciousQuestions) AnalyzeAnswerPattern(
            StudentResult student,
            List<StudentResult> allStudents,
            string[] answerKey,
            int questionCount)
        {
            // Aynı seçeneği çok seçme (örn: AAAAAA...)
            var answerCounts = new Dictionary<string, int>();
            int emptyCount = 0;
            
            for (int i = 0; i < questionCount && i < student.Answers.Count; i++)
            {
                string answer = student.Answers[i]?.ToString().ToUpper() ?? "";
                if (string.IsNullOrEmpty(answer) || answer == " ")
                    emptyCount++;
                else if (answerCounts.ContainsKey(answer))
                    answerCounts[answer]++;
                else
                    answerCounts[answer] = 1;
            }

            // En çok seçilen seçenek
            int maxCount = answerCounts.Count > 0 ? answerCounts.Values.Max() : 0;
            double concentration = (double)maxCount / questionCount;

            // Boş bırakma oranı
            double emptyRate = (double)emptyCount / questionCount;

            // Şüpheli soruları işaretle
            var suspiciousQuestions = new double[questionCount];
            
            if (concentration > 0.6) // %60'tan fazla aynı seçenek
            {
                string dominantOption = answerCounts.OrderByDescending(kv => kv.Value).First().Key;
                for (int i = 0; i < questionCount && i < student.Answers.Count; i++)
                {
                    string answer = student.Answers[i]?.ToString().ToUpper() ?? "";
                    if (answer == dominantOption)
                        suspiciousQuestions[i] = 1;
                }
                
                return (concentration, $"Tek seçenek ({dominantOption}) yoğun kullanımı: %{concentration * 100:F0}", suspiciousQuestions);
            }

            if (emptyRate > 0.7) // %70'ten fazla boş
            {
                return (emptyRate, $"Çok fazla boş cevap: %{emptyRate * 100:F0}", suspiciousQuestions);
            }

            return (0, "", Array.Empty<double>());
        }

        /// <summary>
        /// Kopya şüphesi tespiti - diğer öğrencilerle cevap benzerliği
        /// </summary>
        private (double Severity, string Description, double[] SuspiciousQuestions) DetectCopySuspicion(
            StudentResult student,
            List<StudentResult> allStudents,
            int questionCount)
        {
            double maxSimilarity = 0;
            string similarStudent = "";
            int matchingAnswers = 0;

            foreach (var other in allStudents)
            {
                if (other.StudentId == student.StudentId)
                    continue;

                int matches = 0;
                int comparable = 0;

                for (int i = 0; i < questionCount; i++)
                {
                    if (i >= student.Answers.Count || i >= other.Answers.Count)
                        continue;

                    string ans1 = student.Answers[i]?.ToString() ?? "";
                    string ans2 = other.Answers[i]?.ToString() ?? "";

                    if (!string.IsNullOrEmpty(ans1) && !string.IsNullOrEmpty(ans2))
                    {
                        comparable++;
                        if (ans1 == ans2)
                            matches++;
                    }
                }

                if (comparable > 0)
                {
                    double similarity = (double)matches / comparable;
                    if (similarity > maxSimilarity)
                    {
                        maxSimilarity = similarity;
                        similarStudent = other.FullName;
                        matchingAnswers = matches;
                    }
                }
            }

            var suspiciousQuestions = new double[questionCount];
            
            // %85'ten fazla benzerlik ve 10+ cevap eşleşmesi
            if (maxSimilarity > 0.85 && matchingAnswers >= 10)
            {
                return (
                    maxSimilarity,
                    $"{similarStudent} ile yüksek cevap benzerliği (%{maxSimilarity * 100:F1})",
                    suspiciousQuestions
                );
            }

            return (0, "", Array.Empty<double>());
        }

        private double CalculateVariance(List<double> values)
        {
            if (values.Count < 2)
                return 0;

            double mean = values.Average();
            double sumSquaredDiff = values.Sum(v => Math.Pow(v - mean, 2));
            return sumSquaredDiff / (values.Count - 1); // Sample variance
        }

        private double CalculateStandardDeviation(List<double> values)
        {
            return Math.Sqrt(CalculateVariance(values));
        }

        private double CalculateStandardDeviation(IEnumerable<double> values)
        {
            return CalculateStandardDeviation(values.ToList());
        }
    }
}
