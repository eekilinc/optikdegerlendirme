using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

        /// <summary>
        /// Tüm sınav analizlerini tek PDF raporunda birleştirir
        /// </summary>
        public void GenerateFullReport(
            string examName,
            List<StudentResult> students,
            List<ItemAnalysisService.QuestionItemStats> questionStats,
            List<QuestionDifficulty> questionDifficulties,
            ObservableCollection<LearningOutcome> learningOutcomes,
            ItemAnalysisService.ReliabilityStats reliabilityStats,
            StatisticsReportService.ClassStatistics classStats,
            ObservableCollection<StatisticsReportService.ScoreDistribution> scoreDistribution,
            ObservableCollection<StatisticsReportService.QuestionCorrelation> questionCorrelations,
            Dictionary<string, double> percentiles,
            ObservableCollection<AnswerKeyModel> answerKeys,
            string outputFilePath)
        {
            if (students == null || students.Count == 0)
                throw new ArgumentException("Öğrenci listesi boş olamaz.", nameof(students));

            Document.Create(container =>
            {
                // AKADEMİK KAPAK SAYFASI
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.PageColor("#ffffff");
                    page.DefaultTextStyle(x => x.FontSize(11).FontFamily(Fonts.TimesNewRoman));

                    page.Content().Column(x =>
                    {
                        x.Spacing(30);

                        // Üst Dekoratif Çizgi
                        x.Item().Height(4).Background("#1e3a5f");
                        x.Item().PaddingTop(2).Height(2).Background("#3b82f6");

                        // Başlık Bölümü
                        x.Item().AlignCenter().Column(c =>
                        {
                            c.Item().Text("SINAV DEĞERLENDİRME RAPORU").FontSize(22).Bold().FontColor("#1e3a5f").FontFamily(Fonts.TimesNewRoman);
                            c.Item().PaddingTop(8).Text("ITEM ANALYSIS & STATISTICAL REPORT").FontSize(12).Italic().FontColor("#64748b").FontFamily(Fonts.TimesNewRoman);
                            c.Item().PaddingVertical(20).LineHorizontal(1).LineColor("#cbd5e1");
                        });

                        // Sınav Bilgileri
                        x.Item().Border(1).BorderColor("#e2e8f0").Background("#f8fafc").Padding(20).Column(c =>
                        {
                            c.Item().AlignCenter().PaddingBottom(15).Text("SINAV BİLGİLERİ").FontSize(14).Bold().FontColor("#1e3a5f");
                            c.Item().Table(t =>
                            {
                                t.ColumnsDefinition(cd => { cd.RelativeColumn(); cd.RelativeColumn(); });
                                t.Cell().Text("Sınav Adı:").FontSize(11).SemiBold().FontColor("#475569");
                                t.Cell().Text(examName).FontSize(11).FontColor("#1e293b");
                                t.Cell().Text("Rapor Tarihi:").FontSize(11).SemiBold().FontColor("#475569");
                                t.Cell().Text(DateTime.Now.ToString("dd MMMM yyyy, HH:mm", new System.Globalization.CultureInfo("tr-TR"))).FontSize(11).FontColor("#1e293b");
                                t.Cell().Text("Öğrenci Sayısı:").FontSize(11).SemiBold().FontColor("#475569");
                                t.Cell().Text(students.Count.ToString()).FontSize(11).FontColor("#1e293b");
                                t.Cell().Text("Soru Sayısı:").FontSize(11).SemiBold().FontColor("#475569");
                                t.Cell().Text((questionDifficulties?.Count ?? 0).ToString()).FontSize(11).FontColor("#1e293b");
                            });
                        });

                        // Özet İstatistikler - Modern Kartlar
                        x.Item().AlignCenter().PaddingVertical(15).Text("ÖZET İSTATİSTİKLER").FontSize(14).Bold().FontColor("#1e3a5f");
                        x.Item().Row(r =>
                        {
                            r.RelativeItem().Border(1).BorderColor("#dbeafe").Background("#eff6ff").Padding(12).AlignCenter().Column(c =>
                            {
                                c.Item().Text("ORTALAMA").FontSize(9).FontColor("#64748b");
                                c.Item().Text(classStats?.AverageScore.ToString("F2") ?? "0.00").FontSize(20).Bold().FontColor("#1d4ed8");
                            });
                            r.RelativeItem().Border(1).BorderColor("#dcfce7").Background("#f0fdf4").Padding(12).AlignCenter().Column(c =>
                            {
                                c.Item().Text("MEDYAN").FontSize(9).FontColor("#64748b");
                                c.Item().Text(classStats?.MedianScore.ToString("F2") ?? "0.00").FontSize(20).Bold().FontColor("#15803d");
                            });
                            r.RelativeItem().Border(1).BorderColor("#f3e8ff").Background("#faf5ff").Padding(12).AlignCenter().Column(c =>
                            {
                                c.Item().Text("STD. SAPMA").FontSize(9).FontColor("#64748b");
                                c.Item().Text(classStats?.StandardDeviation.ToString("F2") ?? "0.00").FontSize(20).Bold().FontColor("#7c3aed");
                            });
                            r.RelativeItem().Border(1).BorderColor("#fef3c7").Background("#fffbeb").Padding(12).AlignCenter().Column(c =>
                            {
                                c.Item().Text("GÜVENİRLİK").FontSize(9).FontColor("#64748b");
                                c.Item().Text(reliabilityStats?.CronbachAlpha.ToString("F2") ?? "0.00").FontSize(20).Bold().FontColor("#b45309");
                            });
                        });

                        // Alt Dekoratif Çizgi
                        x.Item().PaddingTop(40).Height(2).Background("#3b82f6");
                        x.Item().PaddingTop(2).Height(4).Background("#1e3a5f");
                    });
                });

                // SAYFA 1: DETAYLI İSTATİSTİKLER
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(1.5f, Unit.Centimetre);
                    page.PageColor("#ffffff");
                    page.DefaultTextStyle(x => x.FontSize(10).FontFamily(Fonts.TimesNewRoman));

                    page.Header().BorderBottom(1).BorderColor("#e2e8f0").PaddingBottom(8).Row(row =>
                    {
                        row.RelativeItem().Text($"{examName} - Sınav Analiz Raporu").FontSize(11).Bold().FontColor("#1e3a5f");
                        row.ConstantItem(80).AlignRight().Text(x => { x.Span("Sayfa ").FontSize(10).FontColor("#64748b"); x.CurrentPageNumber(); });
                    });

                    page.Content().Column(x =>
                    {
                        x.Spacing(12);

                        // Bölüm 1: Zorluk Analizi
                        x.Item().Background("#1e3a5f").Padding(8).Text("1. SORU ZORLUK DAĞILIMI").FontSize(12).Bold().FontColor("#ffffff");
                        
                        if (questionDifficulties?.Count > 0)
                        {
                            var easy = questionDifficulties.Count(q => q.DifficultyLevel == "Kolay");
                            var medium = questionDifficulties.Count(q => q.DifficultyLevel == "Orta");
                            var hard = questionDifficulties.Count(q => q.DifficultyLevel == "Zor");
                            var veryHard = questionDifficulties.Count(q => q.DifficultyLevel == "Çok Zor");
                            
                            x.Item().PaddingTop(8).Table(t =>
                            {
                                t.ColumnsDefinition(cd => { cd.ConstantColumn(80); cd.RelativeColumn(); cd.ConstantColumn(60); cd.ConstantColumn(80); });
                                
                                t.Header(h =>
                                {
                                    h.Cell().Background("#1e3a5f").Padding(6).Text("Zorluk").FontSize(9).Bold().FontColor("#ffffff");
                                    h.Cell().Background("#1e3a5f").Padding(6).Text("Görsel").FontSize(9).Bold().FontColor("#ffffff");
                                    h.Cell().Background("#1e3a5f").Padding(6).AlignCenter().Text("Sayı").FontSize(9).Bold().FontColor("#ffffff");
                                    h.Cell().Background("#1e3a5f").Padding(6).AlignCenter().Text("Oran %").FontSize(9).Bold().FontColor("#ffffff");
                                });

                                int total = questionDifficulties.Count;
                                
                                // Kolay
                                t.Cell().Background("#f0fdf4").BorderBottom(1).BorderColor("#bbf7d0").Padding(5).Text("Kolay").FontSize(9).FontColor("#166534");
                                t.Cell().Background("#f0fdf4").BorderBottom(1).BorderColor("#bbf7d0").Padding(5).Text(new string('█', (int)((double)easy / total * 30))).FontSize(9).FontColor("#22c55e");
                                t.Cell().Background("#f0fdf4").BorderBottom(1).BorderColor("#bbf7d0").Padding(5).AlignCenter().Text(easy.ToString()).FontSize(9).Bold();
                                t.Cell().Background("#f0fdf4").BorderBottom(1).BorderColor("#bbf7d0").Padding(5).AlignCenter().Text($"{(double)easy/total*100:F1}%").FontSize(9);
                                
                                // Orta
                                t.Cell().Background("#fffbeb").BorderBottom(1).BorderColor("#fcd34d").Padding(5).Text("Orta").FontSize(9).FontColor("#92400e");
                                t.Cell().Background("#fffbeb").BorderBottom(1).BorderColor("#fcd34d").Padding(5).Text(new string('█', (int)((double)medium / total * 30))).FontSize(9).FontColor("#f59e0b");
                                t.Cell().Background("#fffbeb").BorderBottom(1).BorderColor("#fcd34d").Padding(5).AlignCenter().Text(medium.ToString()).FontSize(9).Bold();
                                t.Cell().Background("#fffbeb").BorderBottom(1).BorderColor("#fcd34d").Padding(5).AlignCenter().Text($"{(double)medium/total*100:F1}%").FontSize(9);
                                
                                // Zor
                                t.Cell().Background("#fef2f2").BorderBottom(1).BorderColor("#fecaca").Padding(5).Text("Zor").FontSize(9).FontColor("#991b1b");
                                t.Cell().Background("#fef2f2").BorderBottom(1).BorderColor("#fecaca").Padding(5).Text(new string('█', (int)((double)hard / total * 30))).FontSize(9).FontColor("#ef4444");
                                t.Cell().Background("#fef2f2").BorderBottom(1).BorderColor("#fecaca").Padding(5).AlignCenter().Text(hard.ToString()).FontSize(9).Bold();
                                t.Cell().Background("#fef2f2").BorderBottom(1).BorderColor("#fecaca").Padding(5).AlignCenter().Text($"{(double)hard/total*100:F1}%").FontSize(9);
                                
                                // Çok Zor
                                t.Cell().Background("#f8fafc").BorderBottom(1).BorderColor("#e2e8f0").Padding(5).Text("Çok Zor").FontSize(9).FontColor("#475569");
                                t.Cell().Background("#f8fafc").BorderBottom(1).BorderColor("#e2e8f0").Padding(5).Text(new string('█', (int)((double)veryHard / total * 30))).FontSize(9).FontColor("#64748b");
                                t.Cell().Background("#f8fafc").BorderBottom(1).BorderColor("#e2e8f0").Padding(5).AlignCenter().Text(veryHard.ToString()).FontSize(9).Bold();
                                t.Cell().Background("#f8fafc").BorderBottom(1).BorderColor("#e2e8f0").Padding(5).AlignCenter().Text($"{(double)veryHard/total*100:F1}%").FontSize(9);
                            });
                        }

                        // Bölüm 2: Puan Dağılım Histogramı
                        x.Item().PaddingTop(15).Background("#1e3a5f").Padding(8).Text("2. PUAN DAĞILIM HİSTOGRAMI").FontSize(12).Bold().FontColor("#ffffff");
                        
                        if (scoreDistribution?.Count > 0)
                        {
                            x.Item().PaddingTop(8).Table(t =>
                            {
                                t.ColumnsDefinition(cd => { cd.ConstantColumn(80); cd.ConstantColumn(50); cd.RelativeColumn(); });
                                
                                t.Header(h =>
                                {
                                    h.Cell().Background("#334155").Padding(5).Text("Puan Aralığı").FontSize(8).Bold().FontColor("#ffffff");
                                    h.Cell().Background("#334155").Padding(5).AlignCenter().Text("n").FontSize(8).Bold().FontColor("#ffffff");
                                    h.Cell().Background("#334155").Padding(5).Text("Dağılım").FontSize(8).Bold().FontColor("#ffffff");
                                });
                                
                                int maxCount = scoreDistribution.Max(d => d.Count);
                                bool alternate = false;
                                foreach (var d in scoreDistribution)
                                {
                                    var bgColor = alternate ? "#f1f5f9" : "#ffffff";
                                    int barWidth = maxCount > 0 ? (int)((double)d.Count / maxCount * 100) : 0;
                                    string bar = new string('█', barWidth / 4);
                                    
                                    t.Cell().Background(bgColor).BorderBottom(1).BorderColor("#e2e8f0").Padding(4).Text(d.Range).FontSize(8);
                                    t.Cell().Background(bgColor).BorderBottom(1).BorderColor("#e2e8f0").Padding(4).AlignCenter().Text(d.Count.ToString()).FontSize(8);
                                    t.Cell().Background(bgColor).BorderBottom(1).BorderColor("#e2e8f0").Padding(4).Text(bar).FontSize(8).FontColor("#2563eb");
                                    alternate = !alternate;
                                }
                            });
                        }

                        // Bölüm 3: Percentil Tablosu
                        x.Item().PaddingTop(15).Background("#1e3a5f").Padding(8).Text("3. PERCENTİL DEĞERLERİ").FontSize(12).Bold().FontColor("#ffffff");
                        
                        if (percentiles?.Count > 0)
                        {
                            x.Item().PaddingTop(8).Table(t =>
                            {
                                t.ColumnsDefinition(cd => { cd.RelativeColumn(); cd.RelativeColumn(); cd.RelativeColumn(); cd.RelativeColumn(); cd.RelativeColumn(); });
                                
                                foreach (var p in percentiles.OrderBy(kv => kv.Key))
                                {
                                    t.Cell().Border(1).BorderColor("#e2e8f0").Background("#f8fafc").Padding(8).AlignCenter().Column(c =>
                                    {
                                        c.Item().Text(p.Key).FontSize(9).FontColor("#64748b");
                                        c.Item().Text(p.Value.ToString("F2")).FontSize(14).Bold().FontColor("#1e293b");
                                    });
                                }
                            });
                        }
                    });

                    page.Footer().AlignCenter().Text(x => { x.Span("Sınav Değerlendirme Raporu | Sayfa ").FontSize(9).FontColor("#94a3b8"); x.CurrentPageNumber(); });
                });

                // SAYFA 2: ÖĞRENCİ LİSTESİ
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(1.5f, Unit.Centimetre);
                    page.PageColor("#ffffff");
                    page.DefaultTextStyle(x => x.FontSize(9).FontFamily(Fonts.TimesNewRoman));

                    page.Header().BorderBottom(1).BorderColor("#e2e8f0").PaddingBottom(8).Row(row =>
                    {
                        row.RelativeItem().Text($"{examName} - Öğrenci Listesi").FontSize(11).Bold().FontColor("#1e3a5f");
                        row.ConstantItem(80).AlignRight().Text(x => { x.Span("Sayfa ").FontSize(10).FontColor("#64748b"); x.CurrentPageNumber(); });
                    });

                    page.Content().PaddingVertical(10).Column(x =>
                    {
                        x.Item().PaddingBottom(10).Background("#1e3a5f").Padding(8).Text("4. SIRALAMALI ÖĞRENCİ LİSTESİ").FontSize(12).Bold().FontColor("#ffffff");
                        
                        x.Item().Table(t =>
                        {
                            t.ColumnsDefinition(cd => { cd.ConstantColumn(35); cd.RelativeColumn(4); cd.ConstantColumn(80); cd.ConstantColumn(60); cd.ConstantColumn(60); cd.ConstantColumn(60); cd.ConstantColumn(50); });
                            
                            t.Header(h =>
                            {
                                h.Cell().Background("#334155").Padding(5).AlignCenter().Text("Sıra").FontSize(8).Bold().FontColor("#ffffff");
                                h.Cell().Background("#334155").Padding(5).Text("Ad Soyad").FontSize(8).Bold().FontColor("#ffffff");
                                h.Cell().Background("#334155").Padding(5).AlignCenter().Text("Öğr. No").FontSize(8).Bold().FontColor("#ffffff");
                                h.Cell().Background("#334155").Padding(5).AlignCenter().Text("Kitapçık").FontSize(8).Bold().FontColor("#ffffff");
                                h.Cell().Background("#334155").Padding(5).AlignCenter().Text("Doğru").FontSize(8).Bold().FontColor("#ffffff");
                                h.Cell().Background("#334155").Padding(5).AlignCenter().Text("Yanlış").FontSize(8).Bold().FontColor("#ffffff");
                                h.Cell().Background("#334155").Padding(5).AlignCenter().Text("Puan").FontSize(8).Bold().FontColor("#ffffff");
                            });
                            
                            bool alternate = false;
                            foreach (var s in students.OrderBy(st => st.Rank))
                            {
                                var bgColor = alternate ? "#f8fafc" : "#ffffff";
                                
                                t.Cell().Background(bgColor).BorderBottom(1).BorderColor("#e2e8f0").Padding(4).AlignCenter().Text(s.Rank.ToString()).FontSize(8);
                                t.Cell().Background(bgColor).BorderBottom(1).BorderColor("#e2e8f0").Padding(4).Text(s.FullName).FontSize(8);
                                t.Cell().Background(bgColor).BorderBottom(1).BorderColor("#e2e8f0").Padding(4).AlignCenter().Text(s.StudentId).FontSize(8);
                                t.Cell().Background(bgColor).BorderBottom(1).BorderColor("#e2e8f0").Padding(4).AlignCenter().Text(s.BookletType).FontSize(8);
                                t.Cell().Background(bgColor).BorderBottom(1).BorderColor("#e2e8f0").Padding(4).AlignCenter().Text(s.CorrectCount.ToString()).FontSize(8).FontColor("#15803d");
                                t.Cell().Background(bgColor).BorderBottom(1).BorderColor("#e2e8f0").Padding(4).AlignCenter().Text(s.IncorrectCount.ToString()).FontSize(8).FontColor("#dc2626");
                                t.Cell().Background(bgColor).BorderBottom(1).BorderColor("#e2e8f0").Padding(4).AlignCenter().Text(s.Score.ToString("F2")).FontSize(8).Bold().FontColor("#1d4ed8");
                                
                                alternate = !alternate;
                            }
                        });
                    });

                    page.Footer().AlignCenter().Text(x => { x.Span("Sınav Değerlendirme Raporu | Sayfa ").FontSize(9).FontColor("#94a3b8"); x.CurrentPageNumber(); });
                });

                // SAYFA 3: MADDE ANALİZİ
                if (questionStats != null && questionStats.Count > 0)
                {
                    container.Page(page =>
                    {
                        page.Size(PageSizes.A4);
                        page.Margin(1.5f, Unit.Centimetre);
                        page.PageColor("#ffffff");
                        page.DefaultTextStyle(x => x.FontSize(9).FontFamily(Fonts.TimesNewRoman));

                        page.Header().BorderBottom(1).BorderColor("#e2e8f0").PaddingBottom(8).Row(row =>
                        {
                            row.RelativeItem().Text($"{examName} - Madde Analizi").FontSize(11).Bold().FontColor("#1e3a5f");
                            row.ConstantItem(80).AlignRight().Text(x => { x.Span("Sayfa ").FontSize(10).FontColor("#64748b"); x.CurrentPageNumber(); });
                        });

                        page.Content().PaddingVertical(10).Column(x =>
                        {
                            x.Item().PaddingBottom(10).Background("#1e3a5f").Padding(8).Text("5. MADDE İSTATİSTİKLERİ").FontSize(12).Bold().FontColor("#ffffff");
                            
                            x.Item().PaddingBottom(5).Text("Tablo 1. Soru Bazında İstatistiksel Değerler").FontSize(10).Italic().FontColor("#475569");
                            
                            x.Item().Table(t =>
                            {
                                t.ColumnsDefinition(cd => { 
                                    cd.ConstantColumn(30); 
                                    cd.ConstantColumn(55); 
                                    cd.ConstantColumn(55); 
                                    cd.ConstantColumn(55); 
                                    cd.ConstantColumn(70); 
                                    cd.RelativeColumn(); 
                                });
                                
                                t.Header(h =>
                                {
                                    h.Cell().Background("#334155").Padding(5).AlignCenter().Text("Soru").FontSize(8).Bold().FontColor("#ffffff");
                                    h.Cell().Background("#334155").Padding(5).AlignCenter().Text("p (Zorluk)").FontSize(8).Bold().FontColor("#ffffff");
                                    h.Cell().Background("#334155").Padding(5).AlignCenter().Text("r (Ayırt.)").FontSize(8).Bold().FontColor("#ffffff");
                                    h.Cell().Background("#334155").Padding(5).AlignCenter().Text("rpb").FontSize(8).Bold().FontColor("#ffffff");
                                    h.Cell().Background("#334155").Padding(5).AlignCenter().Text("Doğru/Toplam").FontSize(8).Bold().FontColor("#ffffff");
                                    h.Cell().Background("#334155").Padding(5).AlignCenter().Text("Değerlendirme").FontSize(8).Bold().FontColor("#ffffff");
                                });
                                
                                bool alternate = false;
                                foreach (var q in questionStats.OrderBy(qs => qs.QuestionNumber))
                                {
                                    var bgColor = alternate ? "#f8fafc" : "#ffffff";
                                    
                                    string difficultyLevel = q.DifficultyIndex switch
                                    {
                                        > 0.8 => "Çok Kolay",
                                        > 0.6 => "Kolay",
                                        > 0.4 => "Orta",
                                        > 0.2 => "Zor",
                                        _ => "Çok Zor"
                                    };
                                    
                                    string evalColor = q.DifficultyIndex switch
                                    {
                                        > 0.7 and < 0.9 => "#15803d",  // İdeal
                                        > 0.4 and <= 0.7 => "#ca8a04", // Kabul edilebilir
                                        _ => "#dc2626" // Gözden geçirilmeli
                                    };
                                    
                                    t.Cell().Background(bgColor).BorderBottom(1).BorderColor("#e2e8f0").Padding(4).AlignCenter().Text(q.QuestionNumber.ToString()).FontSize(8).Bold();
                                    t.Cell().Background(bgColor).BorderBottom(1).BorderColor("#e2e8f0").Padding(4).AlignCenter().Text((q.DifficultyIndex * 100).ToString("F1")).FontSize(8);
                                    t.Cell().Background(bgColor).BorderBottom(1).BorderColor("#e2e8f0").Padding(4).AlignCenter().Text(q.DiscriminationIndex.ToString("F2")).FontSize(8);
                                    t.Cell().Background(bgColor).BorderBottom(1).BorderColor("#e2e8f0").Padding(4).AlignCenter().Text(q.PointBiserial.ToString("F2")).FontSize(8);
                                    t.Cell().Background(bgColor).BorderBottom(1).BorderColor("#e2e8f0").Padding(4).AlignCenter().Text($"{q.CorrectCount}/{q.TotalStudents}").FontSize(8);
                                    t.Cell().Background(bgColor).BorderBottom(1).BorderColor("#e2e8f0").Padding(4).AlignCenter().Text(difficultyLevel).FontSize(8).FontColor(evalColor);
                                    
                                    alternate = !alternate;
                                }
                            });
                            
                            // CEVAP ANAHTARI BÖLÜMÜ - Tek satırda yatay liste
                            var primaryKey = answerKeys?.FirstOrDefault(k => !string.IsNullOrEmpty(k.Answers));
                            if (primaryKey != null)
                            {
                                x.Item().PaddingTop(10).Background("#15803d").Padding(5).Text("CEVAP ANAHTARI").FontSize(10).Bold().FontColor("#ffffff");
                                
                                // 10'lu gruplar halinde göster
                                int groupSize = 10;
                                for (int i = 0; i < primaryKey.Answers.Length; i += groupSize)
                                {
                                    var group = Enumerable.Range(i, Math.Min(groupSize, primaryKey.Answers.Length - i))
                                        .Select(idx => $"{idx + 1}-{primaryKey.Answers[idx].ToString().ToUpper()}");
                                    
                                    string line = string.Join("    ", group); // 4 boşlukla ayır
                                    
                                    x.Item().PaddingTop(3).PaddingBottom(3).Text(line)
                                        .FontSize(10)
                                        .FontColor("#1e293b")
                                        .FontFamily(Fonts.CourierNew);
                                }
                            }
                            
                            // Tablo 2: Şık Dağılımı Detayları (Kompakt)
                            x.Item().PaddingTop(15).PaddingBottom(3).Text("Tablo 2. Şık Dağılımı (%)").FontSize(10).Italic().FontColor("#475569");
                            
                            x.Item().Table(t =>
                            {
                                t.ColumnsDefinition(cd => { 
                                    cd.ConstantColumn(28);   // Soru No
                                    cd.ConstantColumn(32);   // A
                                    cd.ConstantColumn(32);   // B
                                    cd.ConstantColumn(32);   // C
                                    cd.ConstantColumn(32);   // D
                                    cd.ConstantColumn(32);   // E
                                    cd.ConstantColumn(32);   // Boş
                                    cd.ConstantColumn(35);   // Doğru
                                });
                                
                                // Başlık
                                t.Header(h =>
                                {
                                    h.Cell().Background("#1e3a5f").Padding(2).AlignCenter().Text("Soru").FontSize(7).Bold().FontColor("#ffffff");
                                    h.Cell().Background("#334155").Padding(2).AlignCenter().Text("A").FontSize(7).Bold().FontColor("#ffffff");
                                    h.Cell().Background("#334155").Padding(2).AlignCenter().Text("B").FontSize(7).Bold().FontColor("#ffffff");
                                    h.Cell().Background("#334155").Padding(2).AlignCenter().Text("C").FontSize(7).Bold().FontColor("#ffffff");
                                    h.Cell().Background("#334155").Padding(2).AlignCenter().Text("D").FontSize(7).Bold().FontColor("#ffffff");
                                    h.Cell().Background("#334155").Padding(2).AlignCenter().Text("E").FontSize(7).Bold().FontColor("#ffffff");
                                    h.Cell().Background("#334155").Padding(2).AlignCenter().Text("Boş").FontSize(7).Bold().FontColor("#ffffff");
                                    h.Cell().Background("#1e3a5f").Padding(2).AlignCenter().Text("Doğru").FontSize(7).Bold().FontColor("#ffffff");
                                });
                                
                                bool altRow = false;
                                foreach (var q in questionStats.OrderBy(qs => qs.QuestionNumber))
                                {
                                    var bg = altRow ? "#f8fafc" : "#ffffff";
                                    var dist = q.OptionDistribution;
                                    
                                    // GERÇEK DOĞRU CEVABI cevap anahtarından al
                                    string correctOpt = "?";
                                    if (primaryKey != null && q.QuestionNumber <= primaryKey.Answers.Length)
                                    {
                                        correctOpt = primaryKey.Answers[q.QuestionNumber - 1].ToString().ToUpper();
                                    }
                                    
                                    t.Cell().Background(bg).BorderBottom(1).BorderColor("#e2e8f0").Padding(1).AlignCenter().Text(q.QuestionNumber.ToString()).FontSize(7).Bold();
                                    
                                    // Her şık için değer - doğru şık yeşil, diğerleri gri
                                    for (int i = 0; i < 5; i++)
                                    {
                                        string optLetter = ((char)('A' + i)).ToString();
                                        string color = (optLetter == correctOpt) ? "#15803d" : "#64748b";
                                        double pct = (dist != null && dist.Length > i) ? dist[i] * 100 : 0;
                                        t.Cell().Background(bg).BorderBottom(1).BorderColor("#e2e8f0").Padding(1).AlignCenter()
                                            .Text($"{pct:F0}").FontSize(7).FontColor(color);
                                    }
                                    
                                    // Boş sayısı
                                    double emptyPct = q.EmptyCount / (double)q.TotalStudents * 100;
                                    t.Cell().Background(bg).BorderBottom(1).BorderColor("#e2e8f0").Padding(1).AlignCenter()
                                        .Text($"{emptyPct:F0}").FontSize(7).FontColor("#94a3b8");
                                    
                                    // Doğru cevap gösterimi
                                    t.Cell().Background("#dcfce7").BorderBottom(1).BorderColor("#e2e8f0").Padding(1).AlignCenter()
                                        .Text(correctOpt).FontSize(8).Bold().FontColor("#15803d");
                                    
                                    altRow = !altRow;
                                }
                            });
                            
                            x.Item().PaddingTop(15).Border(1).BorderColor("#e2e8f0").Background("#f8fafc").Padding(10).Column(c =>
                            {
                                c.Item().PaddingBottom(5).Text("YORUM AÇIKLAMALARI:").FontSize(9).Bold().FontColor("#1e293b");
                                c.Item().Text("• p (Zorluk İndeksi): 0.70-0.90 arası ideal, 0.40-0.70 kabul edilebilir, diğerleri gözden geçirilmeli").FontSize(8).FontColor("#475569");
                                c.Item().Text("• r (Ayırt Edicilik): Pozitif değerler iyi, negatif değerler sorunun gözden geçirilmesi gerekir").FontSize(8).FontColor("#475569");
                                c.Item().Text("• rpb (Point-Biserial Korelasyon): 0.20 üzeri kabul edilebilir, 0.40 üzeri mükemmel").FontSize(8).FontColor("#475569");
                            });
                        });

                        page.Footer().AlignCenter().Text(x => { x.Span("Sınav Değerlendirme Raporu | Sayfa ").FontSize(9).FontColor("#94a3b8"); x.CurrentPageNumber(); });
                    });
                }
                else
                {
                    // Madde analizi verisi yoksa boş sayfa göster
                    container.Page(page =>
                    {
                        page.Size(PageSizes.A4);
                        page.Margin(1.5f, Unit.Centimetre);
                        page.PageColor("#ffffff");
                        page.DefaultTextStyle(x => x.FontSize(9).FontFamily(Fonts.TimesNewRoman));

                        page.Header().BorderBottom(1).BorderColor("#e2e8f0").PaddingBottom(8).Row(row =>
                        {
                            row.RelativeItem().Text($"{examName} - Madde Analizi").FontSize(11).Bold().FontColor("#1e3a5f");
                            row.ConstantItem(80).AlignRight().Text(x => { x.Span("Sayfa ").FontSize(10).FontColor("#64748b"); x.CurrentPageNumber(); });
                        });

                        page.Content().PaddingVertical(10).Column(x =>
                        {
                            x.Item().PaddingBottom(10).Background("#dc2626").Padding(8).Text("MADDE ANALİZİ VERİSİ YOK").FontSize(12).Bold().FontColor("#ffffff");
                            x.Item().PaddingTop(20).Text($"QuestionStats sayısı: {(questionStats?.Count ?? 0)}").FontSize(10).FontColor("#dc2626");
                            x.Item().PaddingTop(10).Text("Lütfen önce değerlendirme işlemini tamamlayın.").FontSize(10).FontColor("#475569");
                        });

                        page.Footer().AlignCenter().Text(x => { x.Span("Sınav Değerlendirme Raporu | Sayfa ").FontSize(9).FontColor("#94a3b8"); x.CurrentPageNumber(); });
                    });
                }

                // SAYFA 4: KAZANIM ANALİZİ
                if (learningOutcomes?.Count > 0)
                {
                    container.Page(page =>
                    {
                        page.Size(PageSizes.A4);
                        page.Margin(1.5f, Unit.Centimetre);
                        page.PageColor("#ffffff");
                        page.DefaultTextStyle(x => x.FontSize(9).FontFamily(Fonts.TimesNewRoman));

                        page.Header().BorderBottom(1).BorderColor("#e2e8f0").PaddingBottom(8).Row(row =>
                        {
                            row.RelativeItem().Text($"{examName} - Kazanım Analizi").FontSize(11).Bold().FontColor("#1e3a5f");
                            row.ConstantItem(80).AlignRight().Text(x => { x.Span("Sayfa ").FontSize(10).FontColor("#64748b"); x.CurrentPageNumber(); });
                        });

                        page.Content().PaddingVertical(10).Column(x =>
                        {
                            x.Item().PaddingBottom(10).Background("#1e3a5f").Padding(8).Text("6. ÖĞRENME KAZANIMLARI ANALİZİ").FontSize(12).Bold().FontColor("#ffffff");
                            
                            x.Item().PaddingBottom(5).Text("Tablo 2. Kazanım Bazında Başarı Oranları").FontSize(10).Italic().FontColor("#475569");
                            
                            x.Item().Table(t =>
                            {
                                t.ColumnsDefinition(cd => { cd.ConstantColumn(35); cd.RelativeColumn(5); cd.ConstantColumn(50); cd.ConstantColumn(80); cd.ConstantColumn(70); });
                                
                                t.Header(h =>
                                {
                                    h.Cell().Background("#334155").Padding(5).AlignCenter().Text("No").FontSize(8).Bold().FontColor("#ffffff");
                                    h.Cell().Background("#334155").Padding(5).Text("Kazanım / Konu Adı").FontSize(8).Bold().FontColor("#ffffff");
                                    h.Cell().Background("#334155").Padding(5).AlignCenter().Text("Soru").FontSize(8).Bold().FontColor("#ffffff");
                                    h.Cell().Background("#334155").Padding(5).AlignCenter().Text("Başarı %").FontSize(8).Bold().FontColor("#ffffff");
                                    h.Cell().Background("#334155").Padding(5).AlignCenter().Text("Derecelendirme").FontSize(8).Bold().FontColor("#ffffff");
                                });
                                
                                int idx = 1;
                                bool alternate = false;
                                foreach (var o in learningOutcomes)
                                {
                                    var bgColor = alternate ? "#f8fafc" : "#ffffff";
                                    
                                    string level = o.SuccessRate switch
                                    {
                                        >= 80 => "Mükemmel",
                                        >= 60 => "İyi",
                                        >= 40 => "Orta",
                                        _ => "Gelişmeli"
                                    };
                                    
                                    string levelColor = o.SuccessRate switch
                                    {
                                        >= 80 => "#15803d",
                                        >= 60 => "#2563eb",
                                        >= 40 => "#ca8a04",
                                        _ => "#dc2626"
                                    };
                                    
                                    int qCount = o.QuestionNumbers?.Count ?? 0;
                                    
                                    t.Cell().Background(bgColor).BorderBottom(1).BorderColor("#e2e8f0").Padding(4).AlignCenter().Text(idx.ToString()).FontSize(8);
                                    t.Cell().Background(bgColor).BorderBottom(1).BorderColor("#e2e8f0").Padding(4).Text(o.Name).FontSize(8);
                                    t.Cell().Background(bgColor).BorderBottom(1).BorderColor("#e2e8f0").Padding(4).AlignCenter().Text(qCount.ToString()).FontSize(8);
                                    t.Cell().Background(bgColor).BorderBottom(1).BorderColor("#e2e8f0").Padding(4).AlignCenter().Text($"{o.SuccessRate:F1}%").FontSize(8).Bold().FontColor(levelColor);
                                    t.Cell().Background(bgColor).BorderBottom(1).BorderColor("#e2e8f0").Padding(4).AlignCenter().Text(level).FontSize(8).FontColor(levelColor);
                                    
                                    idx++;
                                    alternate = !alternate;
                                }
                            });
                            
                            x.Item().PaddingTop(15).Border(1).BorderColor("#e2e8f0").Background("#f8fafc").Padding(10).Column(c =>
                            {
                                c.Item().PaddingBottom(5).Text("BAŞARI DERECELENDİRMESİ:").FontSize(9).Bold().FontColor("#1e293b");
                                c.Item().Row(r =>
                                {
                                    r.RelativeItem().Text("≥ %80: Mükemmel").FontSize(8).FontColor("#15803d");
                                    r.RelativeItem().Text("60-79%: İyi").FontSize(8).FontColor("#2563eb");
                                    r.RelativeItem().Text("40-59%: Orta").FontSize(8).FontColor("#ca8a04");
                                    r.RelativeItem().Text("< %40: Gelişmeli").FontSize(8).FontColor("#dc2626");
                                });
                            });
                        });

                        page.Footer().AlignCenter().Text(x => { x.Span("Sınav Değerlendirme Raporu | Sayfa ").FontSize(9).FontColor("#94a3b8"); x.CurrentPageNumber(); });
                    });
                }

            }).GeneratePdf(outputFilePath);
        }
    }
}
