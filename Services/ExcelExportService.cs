using System;
using System.Collections.Generic;
using ClosedXML.Excel;
using OptikFormApp.Models;

namespace OptikFormApp.Services
{
    public class ExcelExportService
    {
        public void ExportToExcel(List<StudentResult> students, List<QuestionStatisticItem> stats, IEnumerable<LearningOutcome> outcomes, string filePath)
        {
            using (var workbook = new XLWorkbook())
            {
                // -- SHEET 1: ÖĞRENCİ LİSTESİ --
                var ws1 = workbook.Worksheets.Add("Sınav Sonuçları");
                ws1.Cell(1, 1).Value = "Sıra (Derece)";
                ws1.Cell(1, 2).Value = "Öğrenci No";
                ws1.Cell(1, 3).Value = "Ad Soyad";
                ws1.Cell(1, 4).Value = "Kitapçık";
                ws1.Cell(1, 5).Value = "Doğru";
                ws1.Cell(1, 6).Value = "Yanlış";
                ws1.Cell(1, 7).Value = "Boş";
                ws1.Cell(1, 8).Value = "Puan";
                ws1.Cell(1, 9).Value = "Öğrenci Cevapları";

                var headerRange1 = ws1.Range("A1:I1");
                headerRange1.Style.Font.Bold = true;
                headerRange1.Style.Fill.BackgroundColor = XLColor.AirForceBlue;
                headerRange1.Style.Font.FontColor = XLColor.White;

                int row = 2;
                foreach (var student in students)
                {
                    ws1.Cell(row, 1).Value = student.Rank;
                    ws1.Cell(row, 2).Value = student.StudentId;
                    ws1.Cell(row, 3).Value = student.FullName;
                    ws1.Cell(row, 4).Value = student.BookletType;
                    ws1.Cell(row, 5).Value = student.CorrectCount;
                    ws1.Cell(row, 6).Value = student.IncorrectCount;
                    ws1.Cell(row, 7).Value = student.EmptyCount;
                    ws1.Cell(row, 8).Value = student.Score;
                    ws1.Cell(row, 8).Style.NumberFormat.Format = "0.00";
                    ws1.Cell(row, 9).Value = student.RawAnswers;
                    row++;
                }
                ws1.Columns().AdjustToContents();

                // -- SHEET 2: MADDE ANALİZİ --
                if (stats != null && stats.Count > 0)
                {
                    var ws2 = workbook.Worksheets.Add("Madde Analizi");
                    ws2.Cell(1, 1).Value = "Kitapçık";
                    ws2.Cell(1, 2).Value = "Soru No";
                    ws2.Cell(1, 3).Value = "Doğru Cevap";
                    ws2.Cell(1, 4).Value = "Doğru Oranı";
                    ws2.Cell(1, 5).Value = "Yanlış Oranı";
                    ws2.Cell(1, 6).Value = "Boş Oranı";
                    ws2.Cell(1, 7).Value = "A";
                    ws2.Cell(1, 8).Value = "B";
                    ws2.Cell(1, 9).Value = "C";
                    ws2.Cell(1, 10).Value = "D";
                    ws2.Cell(1, 11).Value = "E";
                    ws2.Cell(1, 12).Value = "Boş Sayısı";

                    var headerRange2 = ws2.Range("A1:L1");
                    headerRange2.Style.Font.Bold = true;
                    headerRange2.Style.Fill.BackgroundColor = XLColor.CoolGrey;
                    headerRange2.Style.Font.FontColor = XLColor.White;

                    row = 2;
                    foreach (var stat in stats)
                    {
                        ws2.Cell(row, 1).Value = stat.Booklet;
                        ws2.Cell(row, 2).Value = stat.QuestionNumber;
                        ws2.Cell(row, 3).Value = stat.CorrectAnswer;
                        ws2.Cell(row, 4).Value = stat.CorrectPercent / 100.0;
                        ws2.Cell(row, 4).Style.NumberFormat.Format = "0.00%";
                        ws2.Cell(row, 5).Value = stat.IncorrectPercent / 100.0;
                        ws2.Cell(row, 5).Style.NumberFormat.Format = "0.00%";
                        ws2.Cell(row, 6).Value = stat.EmptyPercent / 100.0;
                        ws2.Cell(row, 6).Style.NumberFormat.Format = "0.00%";
                        
                        ws2.Cell(row, 7).Value = stat.CountA;
                        ws2.Cell(row, 8).Value = stat.CountB;
                        ws2.Cell(row, 9).Value = stat.CountC;
                        ws2.Cell(row, 10).Value = stat.CountD;
                        ws2.Cell(row, 11).Value = stat.CountE;
                        ws2.Cell(row, 12).Value = stat.CountEmpty;
                        row++;
                    }
                    ws2.Columns().AdjustToContents();
                }

                // -- SHEET 3: KONU ANALİZİ --
                if (outcomes != null)
                {
                    bool hasOutcomes = false;
                    foreach (var o in outcomes) { hasOutcomes = true; break; }
                    
                    if (hasOutcomes)
                    {
                        var ws3 = workbook.Worksheets.Add("Konu Analizi");
                        ws3.Cell(1, 1).Value = "Kitapçık";
                        ws3.Cell(1, 2).Value = "Konu Adı";
                        ws3.Cell(1, 3).Value = "İlgili Sorular";
                        ws3.Cell(1, 4).Value = "Genel Başarı %";
                        ws3.Cell(1, 5).Value = "Kitapçık Başarısı %";
                        ws3.Cell(1, 6).Value = "Genel Doğru";
                        ws3.Cell(1, 7).Value = "Genel Toplam";

                        var headerRange3 = ws3.Range("A1:G1");
                        headerRange3.Style.Font.Bold = true;
                        headerRange3.Style.Fill.BackgroundColor = XLColor.Emerald;
                        headerRange3.Style.Font.FontColor = XLColor.White;

                        row = 2;
                        foreach (var outcome in outcomes)
                        {
                            ws3.Cell(row, 1).Value = outcome.BookletName;
                            ws3.Cell(row, 2).Value = outcome.Name;
                            ws3.Cell(row, 3).Value = outcome.QuestionNumbersRaw;
                            ws3.Cell(row, 4).Value = outcome.GlobalSuccessRate / 100.0;
                            ws3.Cell(row, 4).Style.NumberFormat.Format = "0.0%";
                            ws3.Cell(row, 5).Value = outcome.SuccessRate / 100.0;
                            ws3.Cell(row, 5).Style.NumberFormat.Format = "0.0%";
                            ws3.Cell(row, 6).Value = outcome.GlobalCorrectCount;
                            ws3.Cell(row, 7).Value = outcome.GlobalTotalQuestions;
                            row++;
                        }
                        ws3.Columns().AdjustToContents();
                    }
                }

                workbook.SaveAs(filePath);
            }
        }
    }
}
