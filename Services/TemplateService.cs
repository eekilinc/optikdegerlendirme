using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using OptikFormApp.Models;

namespace OptikFormApp.Services
{
    public class TemplateService
    {
        private readonly string _templatesPath;

        public TemplateService()
        {
            _templatesPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Templates");
            if (!Directory.Exists(_templatesPath))
                Directory.CreateDirectory(_templatesPath);
        }

        public class ExamTemplate
        {
            public string Id { get; set; } = Guid.NewGuid().ToString();
            public string Name { get; set; } = "";
            public string Description { get; set; } = "";
            public DateTime CreatedAt { get; set; } = DateTime.Now;
            public DateTime UpdatedAt { get; set; } = DateTime.Now;
            
            // Template Data
            public List<AnswerKeyModel> AnswerKeys { get; set; } = new();
            public List<QuestionSetting> QuestionSettings { get; set; } = new();
            public List<LearningOutcome> LearningOutcomes { get; set; } = new();
            public double NetCoefficient { get; set; } = 1.0;
            public double BaseScore { get; set; } = 0.0;
            public double WrongDeductionFactor { get; set; } = 0.25;
            public string SchoolName { get; set; } = "Okul Adı";
        }

        public void SaveTemplate(ExamTemplate template)
        {
            template.UpdatedAt = DateTime.Now;
            var filePath = Path.Combine(_templatesPath, $"{template.Id}.json");
            var json = JsonSerializer.Serialize(template, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(filePath, json);
        }

        public ExamTemplate? LoadTemplate(string id)
        {
            var filePath = Path.Combine(_templatesPath, $"{id}.json");
            if (!File.Exists(filePath)) return null;
            
            var json = File.ReadAllText(filePath);
            return JsonSerializer.Deserialize<ExamTemplate>(json);
        }

        public void DeleteTemplate(string id)
        {
            var filePath = Path.Combine(_templatesPath, $"{id}.json");
            if (File.Exists(filePath))
                File.Delete(filePath);
        }

        public List<ExamTemplate> GetAllTemplates()
        {
            var templates = new List<ExamTemplate>();
            var files = Directory.GetFiles(_templatesPath, "*.json");
            
            foreach (var file in files)
            {
                try
                {
                    var json = File.ReadAllText(file);
                    var template = JsonSerializer.Deserialize<ExamTemplate>(json);
                    if (template != null)
                        templates.Add(template);
                }
                catch { /* Skip invalid files */ }
            }
            
            return templates.OrderByDescending(t => t.UpdatedAt).ToList();
        }

        public ExamTemplate CreateFromCurrent(
            string name, 
            string description,
            IEnumerable<AnswerKeyModel> answerKeys,
            IEnumerable<QuestionSetting> questionSettings,
            IEnumerable<LearningOutcome> learningOutcomes,
            double netCoefficient,
            double baseScore,
            double wrongDeductionFactor,
            string schoolName)
        {
            return new ExamTemplate
            {
                Id = Guid.NewGuid().ToString(),
                Name = name,
                Description = description,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now,
                AnswerKeys = answerKeys.ToList(),
                QuestionSettings = questionSettings.ToList(),
                LearningOutcomes = learningOutcomes.ToList(),
                NetCoefficient = netCoefficient,
                BaseScore = baseScore,
                WrongDeductionFactor = wrongDeductionFactor,
                SchoolName = schoolName
            };
        }

        public void ExportTemplate(string id, string exportPath)
        {
            var template = LoadTemplate(id);
            if (template == null) return;

            var json = JsonSerializer.Serialize(template, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(exportPath, json);
        }

        public ExamTemplate? ImportTemplate(string importPath)
        {
            if (!File.Exists(importPath)) return null;
            
            var json = File.ReadAllText(importPath);
            var template = JsonSerializer.Deserialize<ExamTemplate>(json);
            if (template != null)
            {
                template.Id = Guid.NewGuid().ToString();
                template.CreatedAt = DateTime.Now;
                template.UpdatedAt = DateTime.Now;
                SaveTemplate(template);
            }
            return template;
        }
    }
}
