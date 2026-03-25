using System;
using System.Collections.Generic;
using System.IO;
using OptikFormApp.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using QuestPDF.Previewer;

namespace OptikFormApp.Services
{
    public class PdfReportService
    {
        public void GenerateStudentReports(List<StudentResult> targetStudents, List<StudentResult> allStudents, IEnumerable<LearningOutcome> outcomes, string outputFolder)
        {
            // QuestPDF License - Required for community use
            QuestPDF.Settings.License = LicenseType.Community;

            // Calculate Exam Average
            double examAverage = allStudents != null && allStudents.Count > 0 ? allStudents.Average(x => x.Score) : 0;
            double examNetAverage = allStudents != null && allStudents.Count > 0 ? allStudents.Average(x => x.NetCount) : 0;
            int totalStudents = allStudents != null ? allStudents.Count : 0;

            foreach (var student in targetStudents)
            {
                string safeName = string.Join("_", student.FullName.Split(Path.GetInvalidFileNameChars()));
                string filePath = Path.Combine(outputFolder, $"Karne_{student.StudentId}_{safeName}.pdf");

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

                            // Topic Analysis (Learning Outcomes)
                            if (outcomes != null)
                            {
                                bool hasOutcomes = false;
                                foreach (var o in outcomes) { hasOutcomes = true; break; }

                                if (hasOutcomes)
                                {
                                    x.Item().PaddingTop(15).Text("Konu Bazlı Başarı Analiziniz:").SemiBold().Underline();
                                    x.Item().Table(table =>
                                    {
                                        table.ColumnsDefinition(cols =>
                                        {
                                            cols.RelativeColumn(3); // Konu Adı
                                            cols.RelativeColumn(1); // Soru Sayısı
                                            cols.RelativeColumn(1); // Öğrenci Doğru
                                            cols.RelativeColumn(1); // Öğrenci Başarı %
                                        });

                                        table.Header(header =>
                                        {
                                            header.Cell().Text("Konu Adı").Bold().FontSize(10);
                                            header.Cell().Text("Soru Sayısı").Bold().FontSize(10);
                                            header.Cell().Text("Doğrunuz").Bold().FontSize(10);
                                            header.Cell().Text("Başarı Oranı").Bold().FontSize(10);
                                        });

                                        foreach (var topic in outcomes)
                                        {
                                            // Only show topics for this student's booklet
                                            if (!string.Equals(topic.BookletName, student.BookletType, StringComparison.OrdinalIgnoreCase))
                                                continue;

                                            // Calculate student's correct count for this topic
                                            var questions = topic.QuestionNumbers;
                                            int topicQCount = questions.Count;
                                            int topicStdCorrect = 0;
                                            if (topicQCount > 0)
                                            {
                                                foreach (var q in questions)
                                                {
                                                    if (q - 1 >= 0 && q - 1 < student.QuestionResults.Count)
                                                    {
                                                        if (student.QuestionResults[q - 1]) topicStdCorrect++;
                                                    }
                                                }
                                            }

                                            double topicSuccess = topicQCount > 0 ? ((double)topicStdCorrect / topicQCount) * 100 : 0;

                                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).PaddingVertical(2).Text(topic.Name).FontSize(10);
                                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).PaddingVertical(2).Text(topicQCount.ToString()).FontSize(10);
                                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).PaddingVertical(2).Text(topicStdCorrect.ToString()).FontSize(10);
                                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).PaddingVertical(2).Text($"{topicSuccess:F0}%").FontSize(10).FontColor(topicSuccess >= 50 ? Colors.Green.Medium : Colors.Red.Medium);
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
}
