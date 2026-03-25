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
                issues.Add(new ValidationIssue
                {
                    Title = "Mükerrer Öğrenci Numarası",
                    Message = $"{duplicateIds.Count} farklı öğrenci numarası birden fazla kez kullanılmış.",
                    Severity = ValidationSeverity.Warning,
                    AffectedItems = string.Join(", ", duplicateIds)
                });

            // 2. Missing Booklet Type Check
            var validBooklets = answerKeys.Select(k => k.BookletName.ToUpper()).ToList();
            var missingBooklets = students.Where(s => !validBooklets.Contains(s.BookletType.ToUpper()))
                                         .Select(s => s.BookletType).Distinct().ToList();
            if (missingBooklets.Any())
                issues.Add(new ValidationIssue
                {
                    Title = "Tanımsız Kitapçık Türü",
                    Message = "Bazı öğrenciler cevap anahtarı tanımlanmamış kitapçık türleri kullanıyor.",
                    Severity = ValidationSeverity.Error,
                    AffectedItems = string.Join(", ", missingBooklets)
                });

            // 3. Empty Names Check
            int emptyNamesCount = students.Count(s => string.IsNullOrWhiteSpace(s.FullName));
            if (emptyNamesCount > 0)
                issues.Add(new ValidationIssue
                {
                    Title = "İsimsiz Kayıtlar",
                    Message = $"{emptyNamesCount} öğrenci kaydında isim alanı boş.",
                    Severity = ValidationSeverity.Warning
                });

            // 4. Empty Answer Key Check
            var emptyKeys = answerKeys.Where(k => string.IsNullOrWhiteSpace(k.Answers)).ToList();
            if (emptyKeys.Any())
                issues.Add(new ValidationIssue
                {
                    Title = "Boş Cevap Anahtarı",
                    Message = "Bir veya daha fazla kitapçığın cevap anahtarı tanımlanmamış. Puanlama yapılamaz.",
                    Severity = ValidationSeverity.Error,
                    AffectedItems = string.Join(", ", emptyKeys.Select(k => k.BookletName))
                });

            // 5. Question Count Mismatch Between Booklets
            var definedKeys = answerKeys.Where(k => !string.IsNullOrWhiteSpace(k.Answers)).ToList();
            if (definedKeys.Count > 1)
            {
                var lengths = definedKeys.Select(k => new { k.BookletName, Len = k.Answers.Length }).ToList();
                if (lengths.Select(x => x.Len).Distinct().Count() > 1)
                {
                    var detail = string.Join(", ", lengths.Select(x => $"{x.BookletName}={x.Len}"));
                    issues.Add(new ValidationIssue
                    {
                        Title = "Kitapçıklar Arası Soru Sayısı Uyumsuzluğu",
                        Message = $"Farklı kitapçıkların soru sayıları eşit değil: {detail}. Bu, puanlama hatalarına neden olabilir.",
                        Severity = ValidationSeverity.Warning,
                        AffectedItems = detail
                    });
                }
            }

            // 6. Student Answer Length Mismatch
            if (definedKeys.Any())
            {
                var mismatchedStudents = students
                    .Where(s =>
                    {
                        var key = answerKeys.FirstOrDefault(k =>
                            string.Equals(k.BookletName, s.BookletType, StringComparison.OrdinalIgnoreCase));
                        return key != null
                               && !string.IsNullOrWhiteSpace(key.Answers)
                               && s.RawAnswers.Length != key.Answers.Length;
                    })
                    .Select(s => $"{s.StudentId}({s.RawAnswers.Length})")
                    .ToList();

                if (mismatchedStudents.Any())
                    issues.Add(new ValidationIssue
                    {
                        Title = "Öğrenci Cevap Uzunluğu Uyumsuzluğu",
                        Message = $"{mismatchedStudents.Count} öğrencinin cevap sayısı, kitapçığındaki soru sayısıyla eşleşmiyor.",
                        Severity = ValidationSeverity.Warning,
                        AffectedItems = string.Join(", ", mismatchedStudents.Take(10))
                    });
            }

            return issues;
        }
    }
}
