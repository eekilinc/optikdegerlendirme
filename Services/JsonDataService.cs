using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using OptikFormApp.Models;

namespace OptikFormApp.Services
{
    public class JsonDataService
    {
        public class ExamExportData
        {
            public string ExportVersion { get; set; } = "1.0";
            public DateTime ExportDate { get; set; } = DateTime.Now;
            public string Application { get; set; } = "OptikFormApp";
            
            // Exam Information
            public string ExamTitle { get; set; } = "";
            public string CourseName { get; set; } = "";
            public string SchoolName { get; set; } = "";
            public DateTime ExamDate { get; set; } = DateTime.Now;
            
            // Configuration
            public double NetCoefficient { get; set; } = 1.0;
            public double BaseScore { get; set; } = 0.0;
            public double WrongDeductionFactor { get; set; } = 0.25;
            
            // Data
            public List<AnswerKeyModel> AnswerKeys { get; set; } = new();
            public List<QuestionSetting> QuestionSettings { get; set; } = new();
            public List<LearningOutcome> LearningOutcomes { get; set; } = new();
            public List<StudentResult> Students { get; set; } = new();
            
            // Summary Statistics
            public int TotalStudents => Students?.Count ?? 0;
            public double AverageScore { get; set; }
            public double MaxScore { get; set; }
            public double MinScore { get; set; }
        }

        public async Task ExportToJsonAsync(
            string filePath,
            string examTitle,
            string courseName,
            string schoolName,
            double netCoefficient,
            double baseScore,
            double wrongDeductionFactor,
            IEnumerable<AnswerKeyModel> answerKeys,
            IEnumerable<QuestionSetting> questionSettings,
            IEnumerable<LearningOutcome> learningOutcomes,
            IEnumerable<StudentResult> students)
        {
            var studentList = students.ToList();
            var exportData = new ExamExportData
            {
                ExamTitle = examTitle,
                CourseName = courseName,
                SchoolName = schoolName,
                ExamDate = DateTime.Now,
                NetCoefficient = netCoefficient,
                BaseScore = baseScore,
                WrongDeductionFactor = wrongDeductionFactor,
                AnswerKeys = answerKeys.ToList(),
                QuestionSettings = questionSettings.ToList(),
                LearningOutcomes = learningOutcomes.ToList(),
                Students = studentList,
                AverageScore = studentList.Count > 0 ? studentList.Average(s => s.Score) : 0,
                MaxScore = studentList.Count > 0 ? studentList.Max(s => s.Score) : 0,
                MinScore = studentList.Count > 0 ? studentList.Min(s => s.Score) : 0
            };

            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            var json = JsonSerializer.Serialize(exportData, options);
            await File.WriteAllTextAsync(filePath, json);
        }

        public async Task<ExamExportData?> ImportFromJsonAsync(string filePath)
        {
            if (!File.Exists(filePath))
                return null;

            try
            {
                var json = await File.ReadAllTextAsync(filePath);
                var options = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };

                var data = JsonSerializer.Deserialize<ExamExportData>(json, options);
                return data;
            }
            catch
            {
                return null;
            }
        }

        public string GenerateSummary(ExamExportData data)
        {
            return $"""
=== JSON İTHALAT ÖZETİ ===
Sınav: {data.ExamTitle}
Ders: {data.CourseName}
Okul: {data.SchoolName}
Tarih: {data.ExamDate:dd.MM.yyyy HH:mm}

Yapılandırma:
• Net Katsayısı: {data.NetCoefficient}
• Taban Puan: {data.BaseScore}
• Yanlış Sayısı: {data.WrongDeductionFactor}

Veri:
• {data.AnswerKeys.Count} cevap anahtarı
• {data.QuestionSettings.Count} soru ayarı
• {data.LearningOutcomes.Count} kazanım
• {data.Students.Count} öğrenci

İstatistikler:
• Ortalama: {data.AverageScore:F2}
• En Yüksek: {data.MaxScore:F2}
• En Düşük: {data.MinScore:F2}
""";
        }

        public async Task<bool> ValidateJsonFileAsync(string filePath)
        {
            if (!File.Exists(filePath))
                return false;

            try
            {
                var json = await File.ReadAllTextAsync(filePath);
                var options = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };

                var data = JsonSerializer.Deserialize<ExamExportData>(json, options);
                return data != null && data.Students != null && data.AnswerKeys != null;
            }
            catch
            {
                return false;
            }
        }
    }
}
