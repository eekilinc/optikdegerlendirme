using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using OptikFormApp.Models;

namespace OptikFormApp.Services
{
    public class OpticalParserService
    {
        public async Task<(List<StudentResult> Students, List<AnswerKeyModel> AnswerKeys, List<string> Errors)> ParseFileAsync(string filePath)
        {
            var students = new List<StudentResult>();
            var answerKeys = new List<AnswerKeyModel>();
            var errors = new List<string>();

            if (!File.Exists(filePath))
            {
                errors.Add("Dosya bulunamadı.");
                return (students, answerKeys, errors);
            }

            try
            {
                System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
                var lines = await File.ReadAllLinesAsync(filePath, System.Text.Encoding.GetEncoding(1254));
                int lineNum = 0;

                foreach (var line in lines)
                {
                    lineNum++;
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    if (line.Length < 33) 
                    {
                        errors.Add($"Satır {lineNum}: Veri uzunluğu yetersiz (En az 33 karakter gerekli).");
                        continue; 
                    }

                    try 
                    {
                        string fullName = line.Substring(0, 22).Trim();
                        string studentId = line.Substring(22, 10).Trim();
                        string bookletType = line.Substring(32, 1).Trim().ToUpper();
                        string rawAnswers = line.Length > 33 ? line.Substring(33) : "";

                        if (!IsValidOpticalFormat(bookletType, rawAnswers, studentId))
                        {
                            errors.Add($"Satır {lineNum}: Geçersiz veri formatı (Optik forma uygun olmayan karakterler tespit edildi).");
                            continue;
                        }

                        if (string.IsNullOrEmpty(fullName) && string.IsNullOrEmpty(studentId))
                        {
                            answerKeys.Add(new AnswerKeyModel 
                            { 
                                BookletName = bookletType, 
                                Answers = rawAnswers 
                            });
                        }
                        else
                        {
                            students.Add(new StudentResult
                            {
                                FullName = fullName,
                                StudentId = studentId,
                                BookletType = bookletType,
                                RawAnswers = rawAnswers,
                                Answers = rawAnswers.ToCharArray().Select(c => c.ToString()).ToList()
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        errors.Add($"Satır {lineNum} ayrıştırma hatası: {ex.Message}");
                    }
                }

                if (students.Count == 0 && answerKeys.Count == 0)
                {
                    errors.Add("KRİTİK HATA: Dosyada geçerli hiçbir öğrenci veya cevap anahtarı kaydı bulunamadı.");
                }
            }
            catch (Exception ex)
            {
                errors.Add($"Dosya okuma hatası: {ex.Message}");
            }

            return (students, answerKeys, errors);
        }

        private bool IsValidOpticalFormat(string booklet, string answers, string id)
        {
            if (booklet.Length > 0 && !char.IsLetter(booklet[0])) return false;
            foreach (char c in id)
            {
                if (!char.IsDigit(c) && !char.IsWhiteSpace(c)) return false;
            }
            foreach (char c in answers)
            {
                if (!char.IsLetter(c) && c != ' ' && c != '*' && c != '-' && !char.IsDigit(c)) return false;
            }
            return true;
        }

        public void EvaluateStudents(List<StudentResult> students, List<AnswerKeyModel> keys, List<QuestionSetting> settings)
        {
            foreach (var student in students)
            {
                var key = keys.FirstOrDefault(k => k.BookletName == student.BookletType);
                if (key == null || string.IsNullOrEmpty(key.Answers)) continue;

                student.QuestionResults.Clear();
                int correct = 0;
                int wrong = 0;
                int empty = 0;

                for (int i = 0; i < key.Answers.Length; i++)
                {
                    bool isCorrect = false;
                    bool isEmpty = false;
                    
                    if (i < student.Answers.Count)
                    {
                        var stdAns = student.Answers[i].Trim().ToUpper();
                        var keyAns = key.Answers[i].ToString().ToUpper();
                        var stdChar = stdAns.Length > 0 ? stdAns[0] : ' ';

                        var qSetting = settings?.FirstOrDefault(s => s.BookletName == key.BookletName && s.QuestionNumber == (i + 1));

                        if (qSetting != null && qSetting.IsCancelled)
                        {
                            isCorrect = true;
                            correct++;
                        }
                        else if (string.IsNullOrEmpty(stdAns) || stdAns == " " || stdAns == "-")
                        {
                            isEmpty = true;
                            empty++;
                        }
                        else if (qSetting != null && qSetting.IsMultipleEnabled && qSetting.IsCorrect(stdChar))
                        {
                            isCorrect = true;
                            correct++;
                        }
                        else if (stdAns == keyAns)
                        {
                            isCorrect = true;
                            correct++;
                        }
                        else
                        {
                            wrong++;
                        }
                    }
                    else
                    {
                        isEmpty = true;
                        empty++;
                    }
                    student.QuestionResults.Add(isCorrect || isEmpty ? isCorrect : false); 
                    if (i < student.ColoredAnswers.Count)
                    {
                        student.ColoredAnswers[i].State = isCorrect ? AnswerState.Correct : (isEmpty ? AnswerState.Empty : AnswerState.Incorrect);
                    }
                }

                student.CorrectCount = correct;
                student.WrongCount = wrong;
                student.EmptyCount = empty;
                
                double net = correct - (wrong * 0.25);
                student.NetCount = Math.Max(0, net);
                student.Score = (key.Answers.Length > 0) ? (student.NetCount / key.Answers.Length) * 100 : 0;
            }
        }

        public List<QuestionStatisticItem> CalculateStatistics(List<StudentResult> students, List<AnswerKeyModel> keys)
        {
            var stats = new List<QuestionStatisticItem>();
            if (keys.Count == 0) return stats;

            int maxQuestions = keys.Max(k => k.Answers.Length);

            for (int i = 1; i <= maxQuestions; i++)
            {
                int correct = 0;
                int total = 0;

                foreach (var student in students)
                {
                    if (i <= student.QuestionResults.Count)
                    {
                        total++;
                        if (student.QuestionResults[i - 1]) correct++;
                    }
                }

                stats.Add(new QuestionStatisticItem
                {
                    QuestionNumber = i,
                    CorrectCount = correct,
                    CorrectPercent = total > 0 ? (double)correct / total * 100 : 0
                });
            }

            return stats;
        }
    }
}
