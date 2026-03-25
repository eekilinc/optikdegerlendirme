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
            // Build lookup dictionaries for O(1) access
            var keyLookup = new Dictionary<string, AnswerKeyModel>();
            foreach (var k in keys)
                if (!keyLookup.ContainsKey(k.BookletName)) keyLookup[k.BookletName] = k;

            var settingsLookup = new Dictionary<(string, int), QuestionSetting>();
            if (settings != null)
                foreach (var s in settings)
                    settingsLookup[(s.BookletName, s.QuestionNumber)] = s;

            foreach (var student in students)
            {
                if (!keyLookup.TryGetValue(student.BookletType, out var key) || string.IsNullOrEmpty(key.Answers)) continue;

                student.QuestionResults.Clear();
                student.ColoredAnswers.Clear();
                int correct = 0;
                int wrong = 0;
                int empty = 0;

                for (int i = 0; i < key.Answers.Length; i++)
                {
                    bool isCorrect = false;
                    bool isEmpty = false;
                    char displayChar = '_';
                    
                    if (i < student.Answers.Count)
                    {
                        var stdAns = student.Answers[i].Trim().ToUpper();
                        var keyAns = key.Answers[i].ToString().ToUpper();
                        var stdChar = stdAns.Length > 0 ? stdAns[0] : ' ';
                        displayChar = stdChar == ' ' ? '_' : stdChar;

                        settingsLookup.TryGetValue((key.BookletName, i + 1), out var qSetting);

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
                    var state = isCorrect ? AnswerState.Correct : (isEmpty ? AnswerState.Empty : AnswerState.Incorrect);
                    student.ColoredAnswers.Add(new AnswerItem { Character = displayChar, State = state });
                }

                student.CorrectCount = correct;
                student.WrongCount = wrong;
                student.IncorrectCount = wrong;
                student.EmptyCount = empty;
                
                double net = correct - (wrong * 0.25);
                student.NetCount = Math.Max(0, net);
                student.Score = (key.Answers.Length > 0) ? (student.NetCount / key.Answers.Length) * 100 : 0;
            }

            // Assign Rank
            var ranked = students.OrderByDescending(s => s.Score).ThenByDescending(s => s.NetCount).ToList();
            int currentRank = 1;
            for (int i = 0; i < ranked.Count; i++)
            {
                if (i > 0 && 
                   (ranked[i].Score < ranked[i - 1].Score || 
                    ranked[i].NetCount < ranked[i - 1].NetCount))
                {
                    currentRank = i + 1;
                }
                ranked[i].Rank = currentRank;
            }
        }

        public List<QuestionStatisticItem> CalculateStatistics(List<StudentResult> students, List<AnswerKeyModel> keys)
        {
            var stats = new List<QuestionStatisticItem>();
            if (keys.Count == 0) return stats;

            foreach (var key in keys)
            {
                if (string.IsNullOrEmpty(key.Answers)) continue;
                var bookletStudents = students.Where(s => s.BookletType == key.BookletName).ToList();
                int totalStudents = bookletStudents.Count;

                for (int i = 0; i < key.Answers.Length; i++)
                {
                    int correct = 0;
                    int incorrect = 0;
                    int emptyCount = 0;
                    int countA = 0, countB = 0, countC = 0, countD = 0, countE = 0, countEmpty = 0;

                    foreach (var student in bookletStudents)
                    {
                        if (i < student.QuestionResults.Count)
                        {
                            if (student.QuestionResults[i]) correct++;
                            else
                            {
                                // Check if empty or incorrect
                                string ans = i < student.Answers.Count ? student.Answers[i].Trim().ToUpper() : "";
                                if (string.IsNullOrEmpty(ans) || ans == " " || ans == "-")
                                    emptyCount++;
                                else
                                    incorrect++;
                            }
                        }

                        // Count option distribution
                        if (i < student.Answers.Count)
                        {
                            string ans = student.Answers[i].Trim().ToUpper();
                            switch (ans)
                            {
                                case "A": countA++; break;
                                case "B": countB++; break;
                                case "C": countC++; break;
                                case "D": countD++; break;
                                case "E": countE++; break;
                                default: countEmpty++; break;
                            }
                        }
                        else
                        {
                            countEmpty++;
                        }
                    }

                    stats.Add(new QuestionStatisticItem
                    {
                        Booklet = key.BookletName,
                        QuestionNumber = i + 1,
                        CorrectAnswer = key.Answers[i].ToString().ToUpper(),
                        CorrectCount = correct,
                        CorrectPercent = totalStudents > 0 ? (double)correct / totalStudents * 100 : 0,
                        IncorrectPercent = totalStudents > 0 ? (double)incorrect / totalStudents * 100 : 0,
                        EmptyPercent = totalStudents > 0 ? (double)emptyCount / totalStudents * 100 : 0,
                        CountA = countA,
                        CountB = countB,
                        CountC = countC,
                        CountD = countD,
                        CountE = countE,
                        CountEmpty = countEmpty
                    });
                }
            }

            return stats;
        }
    }
}
