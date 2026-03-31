using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using OptikFormApp.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using QuestPDF.Previewer;
using QuestPDF;

namespace OptikFormApp.Services
{
    public class PdfReportService
    {
        public record PdfProgressReport(
            int CompletedCount,
            int TotalCount,
            string CurrentStudentName,
            double Percentage
        );

        /// <summary>
        /// Senkron PDF üretimi (mevcut metod - geriye uyumluluk için)
        /// </summary>
        public void GenerateStudentReports(
            List<StudentResult> targetStudents, 
            List<StudentResult> allStudents, 
            IEnumerable<LearningOutcome> outcomes, 
            string outputFolder, 
            string schoolName = "Okul Adı")
        {
            GenerateStudentReportsAsync(targetStudents, allStudents, outcomes, outputFolder, schoolName, null, CancellationToken.None).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Asenkron paralel PDF üretimi - Progress reporting ve Cancellation desteği ile
        /// </summary>
        public async Task GenerateStudentReportsAsync(
            List<StudentResult> targetStudents,
            List<StudentResult> allStudents,
            IEnumerable<LearningOutcome> outcomes,
            string outputFolder,
            string schoolName = "Okul Adı",
            IProgress<PdfProgressReport>? progress = null,
            CancellationToken cancellationToken = default)
        {
            if (targetStudents == null || targetStudents.Count == 0)
                throw new ArgumentException("En az bir öğrenci gereklidir.", nameof(targetStudents));

            if (!Directory.Exists(outputFolder))
                Directory.CreateDirectory(outputFolder);

            double examAverage = allStudents?.Count > 0 ? allStudents.Average(x => x.Score) : 0;
            double examNetAverage = allStudents?.Count > 0 ? allStudents.Average(x => x.NetCount) : 0;
            int totalStudents = allStudents?.Count ?? 0;
            var outcomesList = outcomes?.ToList() ?? new List<LearningOutcome>();

            int completedCount = 0;
            int totalCount = targetStudents.Count;
            var progressLock = new object();

            var options = new ParallelOptions
            {
                MaxDegreeOfParallelism = Math.Min(4, Environment.ProcessorCount),
                CancellationToken = cancellationToken
            };

            await Parallel.ForEachAsync(targetStudents, options, async (student, ct) =>
            {
                string safeName = string.Join("_", student.FullName.Split(Path.GetInvalidFileNameChars()));
                string filePath = Path.Combine(outputFolder, $"Karne_{student.StudentId}_{safeName}.pdf");

                await Task.Run(() => GenerateSingleStudentPdf(
                    student, outcomesList, schoolName, examAverage, examNetAverage, totalStudents, filePath
                ), ct);

                lock (progressLock)
                {
                    completedCount++;
                    progress?.Report(new PdfProgressReport(
                        completedCount, totalCount, student.FullName, (double)completedCount / totalCount * 100));
                }
            });
        }

        private void GenerateSingleStudentPdf(
            StudentResult student, List<LearningOutcome> outcomes, string schoolName,
            double examAverage, double examNetAverage, int totalStudents, string filePath)
        {
            Document.Create(container =>
            {
                container.Page(page =>
                {
                        page.Size(PageSizes.A4);
                        page.Margin(1, Unit.Centimetre);
                        page.PageColor(Colors.White);
                        page.DefaultTextStyle(x => x.FontSize(12).FontFamily(Fonts.Verdana));

                        page.Header().Row(row =>
                        {
                            row.RelativeItem().Column(col =>
                            {
                                col.Item().Text(schoolName).FontSize(14).Medium().FontColor(Colors.Grey.Medium);
                                col.Item().Text("SINAV SONUÇ KARNESİ").FontSize(20).SemiBold().FontColor(Colors.Blue.Medium);
                                col.Item().Text($"{DateTime.Now:dd.MM.yyyy HH:mm}").FontSize(10).FontColor(Colors.Grey.Medium);
                            });
                        });

                        page.Content().PaddingVertical(1, Unit.Centimetre).Column(x =>
                        {
                            x.Spacing(20);

                            // Student Info
                            x.Item().Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn();
                                    columns.RelativeColumn();
                                });

                                table.Cell().Text(t => { t.Span("Ad Soyad: ").SemiBold(); t.Span(student.FullName); });
                                table.Cell().Text(t => { t.Span("Öğrenci No: ").SemiBold(); t.Span(student.StudentId); });
                                table.Cell().Text(t => { t.Span("Kitapçık: ").SemiBold(); t.Span(student.BookletType); });
                                table.Cell().Text(t => { t.Span("Puan / Sıralama: ").SemiBold(); t.Span($"{student.Score:F2} (Sıra: {student.Rank}/{totalStudents})"); });
                                table.Cell().Text(t => { t.Span("Sınıf Net/Puan Ort.: ").SemiBold(); t.Span($"{examNetAverage:F2} Net / {examAverage:F2} Puan"); });
                            });

                            // Summary Stats
                            x.Item().Row(row =>
                            {
                                row.RelativeItem().Column(c =>
                                {
                                    c.Item().AlignCenter().Text("DOĞRU").FontSize(10).SemiBold();
                                    c.Item().AlignCenter().Text(student.CorrectCount.ToString()).FontSize(16).FontColor(Colors.Green.Medium);
                                });
                                row.RelativeItem().Column(c =>
                                {
                                    c.Item().AlignCenter().Text("YANLIŞ").FontSize(10).SemiBold();
                                    c.Item().AlignCenter().Text(student.IncorrectCount.ToString()).FontSize(16).FontColor(Colors.Red.Medium);
                                });
                                row.RelativeItem().Column(c =>
                                {
                                    c.Item().AlignCenter().Text("BOŞ").FontSize(10).SemiBold();
                                    c.Item().AlignCenter().Text(student.EmptyCount.ToString()).FontSize(16).FontColor(Colors.Grey.Medium);
                                });
                            });

                            // Answers Table
                            x.Item().Text("Cevaplarınız:").SemiBold().Underline();
                            x.Item().Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    for (int i = 0; i < 10; i++) columns.RelativeColumn();
                                });
                                
                                for (int i = 0; i < student.ColoredAnswers.Count; i++)
                                {
                                    var ans = student.ColoredAnswers[i];
                                    table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(2).AlignCenter().Column(c => {
                                        c.Item().Text((i + 1).ToString()).FontSize(8).FontColor(Colors.Grey.Medium);
                                        c.Item().Text(ans.Character.ToString()).Bold().FontColor(
                                            ans.State == AnswerState.Correct ? Colors.Green.Medium :
                                            ans.State == AnswerState.Incorrect ? Colors.Red.Medium : Colors.Grey.Medium
                                        );
                                    });
                                }
                            });

                if (outcomes?.Count > 0)
                {
                    var studentOutcomes = outcomes.Where(o => 
                        string.Equals(o.BookletName, student.BookletType, StringComparison.OrdinalIgnoreCase)).ToList();
                    
                    if (studentOutcomes.Count > 0)
                    {
                        x.Item().PaddingTop(15).Text("Konu Bazlı Başarı Analiziniz:").SemiBold().Underline();
                        x.Item().Table(table =>
                        {
                            table.ColumnsDefinition(cols =>
                            {
                                cols.RelativeColumn(3);
                                cols.RelativeColumn(1);
                                cols.RelativeColumn(1);
                                cols.RelativeColumn(1);
                                cols.RelativeColumn(1);
                            });

                            table.Header(header =>
                            {
                                header.Cell().Text("Konu Adı").Bold().FontSize(10);
                                header.Cell().Text("Soru Sayısı").Bold().FontSize(10);
                                header.Cell().Text("Doğrunuz").Bold().FontSize(10);
                                header.Cell().Text("Başarı %").Bold().FontSize(10);
                                header.Cell().Text("Genel Ort.").Bold().FontSize(10);
                            });

                            foreach (var topic in studentOutcomes)
                            {
                                var questions = topic.QuestionNumbers;
                                int topicQCount = questions.Count;
                                int topicStdCorrect = 0;
                                
                                foreach (var q in questions)
                                {
                                    if (q - 1 >= 0 && q - 1 < student.QuestionResults.Count)
                                    {
                                        if (student.QuestionResults[q - 1]) topicStdCorrect++;
                                    }
                                }

                                double topicSuccess = topicQCount > 0 ? ((double)topicStdCorrect / topicQCount) * 100 : 0;

                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).PaddingVertical(2).Text(topic.Name).FontSize(10);
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).PaddingVertical(2).Text(topicQCount.ToString()).FontSize(10);
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).PaddingVertical(2).Text(topicStdCorrect.ToString()).FontSize(10);
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).PaddingVertical(2).Text($"{topicSuccess:F0}%").FontSize(10).FontColor(topicSuccess >= 50 ? Colors.Green.Medium : Colors.Red.Medium);
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).PaddingVertical(2).Text($"{topic.GlobalSuccessRate:F0}%").FontSize(10).FontColor(Colors.Grey.Medium);
                            }
                        });
                    }
                }
            });

            page.Footer().AlignCenter().Text(x =>
            {
                x.Span("Sayfa ");
                x.CurrentPageNumber();
            });
                });
            })
            .GeneratePdf(filePath);
        }
    }
}
