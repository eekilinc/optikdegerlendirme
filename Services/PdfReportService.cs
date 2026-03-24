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
        public void GenerateStudentReports(List<StudentResult> students, string outputFolder)
        {
            // QuestPDF License - Required for community use
            QuestPDF.Settings.License = LicenseType.Community;

            foreach (var student in students)
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
                                table.Cell().Text(t => { t.Span("Net Puan: ").SemiBold(); t.Span(student.Score.ToString("F2")); });
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
                            x.Item().Grid(grid =>
                            {
                                grid.Columns(10);
                                grid.Spacing(5);
                                
                                for (int i = 0; i < student.ColoredAnswers.Count; i++)
                                {
                                    var ans = student.ColoredAnswers[i];
                                    grid.Item().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(2).AlignCenter().Column(c => {
                                        c.Item().Text((i + 1).ToString()).FontSize(8).FontColor(Colors.Grey.Medium);
                                        c.Item().Text(ans.Character.ToString()).Bold().FontColor(
                                            ans.State == AnswerState.Correct ? Colors.Green.Medium :
                                            ans.State == AnswerState.Incorrect ? Colors.Red.Medium : Colors.Grey.Medium
                                        );
                                    });
                                }
                            });
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
