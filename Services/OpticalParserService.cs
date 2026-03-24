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
        public async Task<List<StudentResult>> ParseFileAsync(string filePath)
        {
            var results = new List<StudentResult>();
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            var encoding = Encoding.GetEncoding("windows-1254"); 

            var lines = await File.ReadAllLinesAsync(filePath, encoding);

            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (line.Length <= 34) continue;

                var result = new StudentResult
                {
                    FullName = line.Substring(0, 22).Trim(),
                    StudentId = line.Substring(22, 11).Trim(),
                    BookletType = line.Substring(33, 1).Trim(),
                    RawAnswers = line.Substring(34)
                };
                
                results.Add(result);
            }

            return results;
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

                int length = Math.Min(student.RawAnswers.Length, keyToUse.Length);

                for (int i = 0; i < length; i++)
                {
                    char studAns = student.RawAnswers[i];
                    char correctAns = keyToUse[i];

                    if (correctAns == 'X')
                    {
                        student.CorrectCount++;
                        student.QuestionResults.Add(true);
                        continue;
                    }

                    if (studAns == ' ')
                    {
                        student.EmptyCount++;
                        student.QuestionResults.Add(false);
                    }
                    else if (studAns == correctAns)
                    {
                        student.CorrectCount++;
                        student.QuestionResults.Add(true);
                    }
                    else
                    {
                        student.IncorrectCount++;
                        student.QuestionResults.Add(false);
                    }
                }

                if (length > 0)
                {
                    student.Score = Math.Round((double)student.CorrectCount / length * 100.0, 2);
                }
            }
        }
    }
}
