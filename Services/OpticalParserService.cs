using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OptikFormApp.Models;

namespace OptikFormApp.Services
{
    public class OpticalParserService
    {
        public async Task<(List<StudentResult> students, List<AnswerKeyModel> answerKeys)> ParseFileAsync(string filePath)
        {
            var studentsList = new List<StudentResult>();
            var keysList = new List<AnswerKeyModel>();
            bool isParsingKeys = true;

            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            var encoding = Encoding.GetEncoding("windows-1254"); 

            var lines = await File.ReadAllLinesAsync(filePath, encoding);
            int validStudentCounter = 1;

            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                
                if (i == 0 && line.Length > 0 && line[0] == '\uFEFF')
                    line = line.Substring(1);

                if (string.IsNullOrWhiteSpace(line)) continue;
                if (line.Length < 33) continue; 

                string fullName = line.Substring(0, 22).Trim();
                string studentId = line.Substring(22, 10).Trim();
                string bookletType = line.Substring(32, 1).Trim();
                string rawAnswers = line.Length > 33 ? line.Substring(33) : "";

                // En başta yer alan (İsim ve Numara alanı boş olan) kayıtları Cevap Anahtarı olarak kabul et
                if (isParsingKeys && string.IsNullOrWhiteSpace(studentId) && string.IsNullOrWhiteSpace(fullName) && !string.IsNullOrWhiteSpace(bookletType))
                {
                    keysList.Add(new AnswerKeyModel
                    {
                        BookletName = bookletType,
                        Answers = rawAnswers.TrimEnd()
                    });
                    continue;
                }

                // Bir kere öğrenci okumaya başlandıysa (İsim veya numara doluysa), artık alttakiler öğrenci kabul edilir.
                isParsingKeys = false;

                var result = new StudentResult
                {
                    RowNumber = validStudentCounter++,
                    FullName = fullName,
                    StudentId = studentId,
                    BookletType = bookletType,
                    RawAnswers = rawAnswers
                };
                
                foreach(char c in result.RawAnswers)
                {
                    result.ColoredAnswers.Add(new AnswerItem { Character = c == ' ' ? '_' : c, State = AnswerState.NotEvaluated });
                }

                studentsList.Add(result);
            }

            return (studentsList, keysList);
        }

        public void EvaluateStudents(List<StudentResult> students, List<AnswerKeyModel> answerKeys)
        {
            foreach (var student in students)
            {
                var matchedKey = answerKeys.FirstOrDefault(k => string.Equals(k.BookletName, student.BookletType, StringComparison.OrdinalIgnoreCase));
                if (matchedKey == null || string.IsNullOrWhiteSpace(matchedKey.Answers)) continue;

                string keyToUse = matchedKey.Answers;

                student.CorrectCount = 0;
                student.IncorrectCount = 0;
                student.EmptyCount = 0;
                student.QuestionResults.Clear();
                student.ColoredAnswers.Clear();

                // Cevap anahtarinin uzunlugu tam olarak sorunun asil sayisidir, limitimiz bu sayidir.
                int length = keyToUse.Length;

                for (int i = 0; i < length; i++)
                {
                    char studAns = i < student.RawAnswers.Length ? student.RawAnswers[i] : ' ';
                    char correctAns = keyToUse[i];

                    if (correctAns == 'X')
                    {
                        student.CorrectCount++;
                        student.QuestionResults.Add(true);
                        student.ColoredAnswers.Add(new AnswerItem { Character = studAns == ' ' ? '_' : studAns, State = AnswerState.Correct });
                        continue;
                    }

                    if (studAns == ' ')
                    {
                        student.EmptyCount++;
                        student.QuestionResults.Add(false);
                        student.ColoredAnswers.Add(new AnswerItem { Character = '_', State = AnswerState.Empty });
                    }
                    else if (studAns == correctAns)
                    {
                        student.CorrectCount++;
                        student.QuestionResults.Add(true);
                        student.ColoredAnswers.Add(new AnswerItem { Character = studAns, State = AnswerState.Correct });
                    }
                    else
                    {
                        student.IncorrectCount++;
                        student.QuestionResults.Add(false);
                        student.ColoredAnswers.Add(new AnswerItem { Character = studAns, State = AnswerState.Incorrect });
                    }
                }

                if (length > 0)
                {
                    student.Score = Math.Round((double)student.CorrectCount / length * 100.0, 2);
                }
            }
        }

        public List<QuestionStatisticItem> CalculateStatistics(List<StudentResult> students, List<AnswerKeyModel> answerKeys)
        {
            var stats = new List<QuestionStatisticItem>();

            foreach (var key in answerKeys.Where(k => !string.IsNullOrWhiteSpace(k.Answers)))
            {
                var bookletStudents = students.Where(s => string.Equals(s.BookletType, key.BookletName, StringComparison.OrdinalIgnoreCase)).ToList();
                if (bookletStudents.Count == 0) continue;

                for (int i = 0; i < key.Answers.Length; i++)
                {
                    char correctAns = key.Answers[i];
                    if (correctAns == 'X') continue;

                    int a = 0, b = 0, c = 0, d = 0, e = 0, empty = 0, correct = 0, incorrect = 0;

                    foreach (var st in bookletStudents)
                    {
                        char studAns = i < st.RawAnswers.Length ? st.RawAnswers[i] : ' ';
                        
                        if (studAns == 'A') a++;
                        else if (studAns == 'B') b++;
                        else if (studAns == 'C') c++;
                        else if (studAns == 'D') d++;
                        else if (studAns == 'E') e++;
                        else if (studAns == ' ') empty++;

                        if (studAns == correctAns) correct++;
                        else if (studAns == ' ') { }
                        else incorrect++;
                    }

                    stats.Add(new QuestionStatisticItem {
                        Booklet = key.BookletName,
                        QuestionNumber = i + 1,
                        CorrectAnswer = correctAns.ToString(),
                        CorrectPercent = Math.Round((double)correct / bookletStudents.Count * 100, 2),
                        IncorrectPercent = Math.Round((double)incorrect / bookletStudents.Count * 100, 2),
                        EmptyPercent = Math.Round((double)empty / bookletStudents.Count * 100, 2),
                        CountA = a, CountB = b, CountC = c, CountD = d, CountE = e, CountEmpty = empty
                    });
                }
            }
            return stats;
        }
    }
}
