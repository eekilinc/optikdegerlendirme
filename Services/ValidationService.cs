using System;
using System.Collections.Generic;
using System.Linq;
using OptikFormApp.Models;

namespace OptikFormApp.Services
{
    public class ValidationService
    {
        public List<ValidationIssue> Validate(List<StudentResult> students, List<AnswerKeyModel> answerKeys)
        {
            var issues = new List<ValidationIssue>();

            // 1. Duplicate Student ID Check
            var duplicateIds = students.Where(s => !string.IsNullOrWhiteSpace(s.StudentId))
                                      .GroupBy(s => s.StudentId)
                                      .Where(g => g.Count() > 1)
                                      .Select(g => g.Key)
                                      .ToList();

            if (duplicateIds.Any())
            {
                issues.Add(new ValidationIssue
                {
                    Title = "Mükerrer Öğrenci Numarası",
                    Message = $"{duplicateIds.Count} farklı öğrenci numarası birden fazla kez kullanılmış.",
                    Severity = ValidationSeverity.Warning,
                    AffectedItems = string.Join(", ", duplicateIds)
                });
            }

            // 2. Missing Booklet Type Check
            var validBooklets = answerKeys.Select(k => k.BookletName.ToUpper()).ToList();
            var missingBooklets = students.Where(s => !validBooklets.Contains(s.BookletType.ToUpper()))
                                         .Select(s => s.BookletType)
                                         .Distinct()
                                         .ToList();

            if (missingBooklets.Any())
            {
                issues.Add(new ValidationIssue
                {
                    Title = "Tanımsız Kitapçık Türü",
                    Message = "Bazı öğrenciler cevap anahtarı tanımlanmamış kitapçık türleri kullanıyor.",
                    Severity = ValidationSeverity.Error,
                    AffectedItems = string.Join(", ", missingBooklets)
                });
            }

            // 3. Empty Names Check
            var emptyNamesCount = students.Count(s => string.IsNullOrWhiteSpace(s.FullName));
            if (emptyNamesCount > 0)
            {
                issues.Add(new ValidationIssue
                {
                    Title = "İsimsiz Kayıtlar",
                    Message = $"{emptyNamesCount} öğrenci kaydında isim alanı boş.",
                    Severity = ValidationSeverity.Warning
                });
            }

            return issues;
        }
    }
}
