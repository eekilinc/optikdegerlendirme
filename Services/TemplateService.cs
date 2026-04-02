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
            // Program Files dizinine yazma izni olmayabileceği için AppData'ya kaydet
            var appDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "OptikDegerlendirme");
            
            _templatesPath = Path.Combine(appDataPath, "Templates");
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

        public async Task SaveTemplateAsync(ExamTemplate template)
        {
            template.UpdatedAt = DateTime.Now;
            var filePath = Path.Combine(_templatesPath, $"{template.Id}.json");
            var json = JsonSerializer.Serialize(template, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(filePath, json);
        }

        public async Task<ExamTemplate?> LoadTemplateAsync(string id)
        {
            var filePath = Path.Combine(_templatesPath, $"{id}.json");
            if (!File.Exists(filePath)) return null;
            
            var json = await File.ReadAllTextAsync(filePath);
            return JsonSerializer.Deserialize<ExamTemplate>(json);
        }

        public async Task DeleteTemplateAsync(string id)
        {
            var filePath = Path.Combine(_templatesPath, $"{id}.json");
            if (File.Exists(filePath))
            {
                await Task.Run(() => File.Delete(filePath));
            }
        }

        public async Task<List<ExamTemplate>> GetAllTemplatesAsync()
        {
            if (!Directory.Exists(_templatesPath))
                return new List<ExamTemplate>();

            return await Task.Run(() =>
            {
                var templates = new List<ExamTemplate>();
                foreach (var file in Directory.GetFiles(_templatesPath, "*.json"))
                {
                    try
                    {
                        var json = File.ReadAllText(file);
                        var template = JsonSerializer.Deserialize<ExamTemplate>(json);
                        if (template != null)
                            templates.Add(template);
                    }
                    catch
                    {
                        // Ignore invalid template files
                    }
                }
                return templates.OrderByDescending(t => t.UpdatedAt).ToList();
            });
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
            if (!Directory.Exists(_templatesPath))
                return new List<ExamTemplate>();

            var templates = new List<ExamTemplate>();
            foreach (var file in Directory.GetFiles(_templatesPath, "*.json"))
            {
                try
                {
                    var json = File.ReadAllText(file);
                    var template = JsonSerializer.Deserialize<ExamTemplate>(json);
                    if (template != null)
                        templates.Add(template);
                }
                catch
                {
                    // Ignore invalid template files
                }
            }
            return templates.OrderByDescending(t => t.UpdatedAt).ToList();
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
