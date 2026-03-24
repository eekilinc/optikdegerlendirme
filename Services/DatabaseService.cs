using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Data.Sqlite;
using OptikFormApp.Models;
using System.Text.Json;

namespace OptikFormApp.Services
{
    public class DatabaseService
    {
        private readonly string _dbPath;

        public DatabaseService()
        {
            _dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "optik.db");
            InitializeDatabase();
        }

        private void InitializeDatabase()
        {
            using (var connection = new SqliteConnection($"Data Source={_dbPath}"))
            {
                connection.Open();

                var command = connection.CreateCommand();
                command.CommandText = @"
                    CREATE TABLE IF NOT EXISTS Courses (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Code TEXT,
                        Name TEXT
                    );

                    CREATE TABLE IF NOT EXISTS Exams (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        CourseId INTEGER,
                        Title TEXT,
                        ExamDate TEXT,
                        ConfigJson TEXT,
                        FOREIGN KEY(CourseId) REFERENCES Courses(Id) ON DELETE CASCADE
                    );

                    CREATE TABLE IF NOT EXISTS ExamResults (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        ExamId INTEGER,
                        StudentId TEXT,
                        FullName TEXT,
                        BookletType TEXT,
                        RawAnswers TEXT,
                        Score REAL,
                        CorrectCount INTEGER,
                        IncorrectCount INTEGER,
                        EmptyCount INTEGER,
                        QuestionResultsJson TEXT,
                        FOREIGN KEY(ExamId) REFERENCES Exams(Id) ON DELETE CASCADE
                    );
                ";
                command.ExecuteNonQuery();
            }
        }

        public List<Course> GetCourses()
        {
            var courses = new List<Course>();
            using (var connection = new SqliteConnection($"Data Source={_dbPath}"))
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = "SELECT Id, Code, Name FROM Courses ORDER BY Name";
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        courses.Add(new Course
                        {
                            Id = reader.GetInt32(0),
                            Code = reader.IsDBNull(1) ? "" : reader.GetString(1),
                            Name = reader.GetString(2)
                        });
                    }
                }
            }
            return courses;
        }

        public int SaveCourse(Course course)
        {
            using (var connection = new SqliteConnection($"Data Source={_dbPath}"))
            {
                connection.Open();
                var command = connection.CreateCommand();
                if (course.Id == 0) {
                    command.CommandText = "INSERT INTO Courses (Code, Name) VALUES (@code, @name); SELECT last_insert_rowid();";
                } else {
                    command.CommandText = "UPDATE Courses SET Code=@code, Name=@name WHERE Id=@id";
                }
                command.Parameters.AddWithValue("@code", course.Code);
                command.Parameters.AddWithValue("@name", course.Name);
                if (course.Id > 0) command.Parameters.AddWithValue("@id", course.Id);
                
                if (course.Id == 0) return Convert.ToInt32(command.ExecuteScalar());
                command.ExecuteNonQuery();
                return course.Id;
            }
        }

        public void DeleteCourse(int courseId)
        {
            using (var connection = new SqliteConnection($"Data Source={_dbPath}"))
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = "DELETE FROM Courses WHERE Id = @id";
                command.Parameters.AddWithValue("@id", courseId);
                command.ExecuteNonQuery();
            }
        }

        public void RenameCourse(int courseId, string newCode, string newName)
        {
            using (var connection = new SqliteConnection($"Data Source={_dbPath}"))
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = "UPDATE Courses SET Code = @code, Name = @name WHERE Id = @id";
                command.Parameters.AddWithValue("@code", newCode);
                command.Parameters.AddWithValue("@name", newName);
                command.Parameters.AddWithValue("@id", courseId);
                command.ExecuteNonQuery();
            }
        }

        public void RenameExam(int examId, string newTitle)
        {
            using (var connection = new SqliteConnection($"Data Source={_dbPath}"))
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = "UPDATE Exams SET Title = @title WHERE Id = @id";
                command.Parameters.AddWithValue("@title", newTitle);
                command.Parameters.AddWithValue("@id", examId);
                command.ExecuteNonQuery();
            }
        }

        public List<ExamEntry> GetExamsForCourse(int courseId)
        {
            var exams = new List<ExamEntry>();
            using (var connection = new SqliteConnection($"Data Source={_dbPath}"))
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = "SELECT Id, CourseId, Title, ExamDate, ConfigJson FROM Exams WHERE CourseId = @cid ORDER BY ExamDate DESC";
                command.Parameters.AddWithValue("@cid", courseId);
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        exams.Add(new ExamEntry
                        {
                            Id = reader.GetInt32(0),
                            CourseId = reader.GetInt32(1),
                            Title = reader.GetString(2),
                            Date = DateTime.Parse(reader.GetString(3)),
                            ConfigJson = reader.GetString(4)
                        });
                    }
                }
            }
            return exams;
        }

        public int SaveExam(ExamEntry exam, List<StudentResult> results)
        {
            using (var connection = new SqliteConnection($"Data Source={_dbPath}"))
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    var cmd = connection.CreateCommand();
                    cmd.Transaction = transaction;
                    cmd.CommandText = "INSERT INTO Exams (CourseId, Title, ExamDate, ConfigJson) VALUES (@cid, @title, @date, @config); SELECT last_insert_rowid();";
                    cmd.Parameters.AddWithValue("@cid", exam.CourseId);
                    cmd.Parameters.AddWithValue("@title", exam.Title);
                    cmd.Parameters.AddWithValue("@date", exam.Date.ToString("o"));
                    cmd.Parameters.AddWithValue("@config", exam.ConfigJson);
                    
                    int examId = Convert.ToInt32(cmd.ExecuteScalar());

                    foreach (var res in results)
                    {
                        var resCmd = connection.CreateCommand();
                        resCmd.Transaction = transaction;
                        resCmd.CommandText = @"
                            INSERT INTO ExamResults (ExamId, StudentId, FullName, BookletType, RawAnswers, Score, CorrectCount, IncorrectCount, EmptyCount, QuestionResultsJson)
                            VALUES (@eid, @sid, @name, @booklet, @raw, @score, @corr, @inc, @emp, @qjson)";
                        resCmd.Parameters.AddWithValue("@eid", examId);
                        resCmd.Parameters.AddWithValue("@sid", res.StudentId);
                        resCmd.Parameters.AddWithValue("@name", res.FullName);
                        resCmd.Parameters.AddWithValue("@booklet", res.BookletType);
                        resCmd.Parameters.AddWithValue("@raw", res.RawAnswers);
                        resCmd.Parameters.AddWithValue("@score", res.Score);
                        resCmd.Parameters.AddWithValue("@corr", res.CorrectCount);
                        resCmd.Parameters.AddWithValue("@inc", res.IncorrectCount);
                        resCmd.Parameters.AddWithValue("@emp", res.EmptyCount);
                        resCmd.Parameters.AddWithValue("@qjson", JsonSerializer.Serialize(res.QuestionResults));
                        resCmd.ExecuteNonQuery();
                    }
                    transaction.Commit();
                    return examId;
                }
            }
        }

        public List<StudentResult> GetResultsForExam(int examId)
        {
            var results = new List<StudentResult>();
            using (var connection = new SqliteConnection($"Data Source={_dbPath}"))
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = @"SELECT StudentId, FullName, BookletType, RawAnswers, Score, CorrectCount, IncorrectCount, EmptyCount, QuestionResultsJson 
                                      FROM ExamResults WHERE ExamId = @eid";
                command.Parameters.AddWithValue("@eid", examId);
                using (var reader = command.ExecuteReader())
                {
                    int counter = 1;
                    while (reader.Read())
                    {
                        var res = new StudentResult
                        {
                            RowNumber = counter++,
                            StudentId = reader.GetString(0),
                            FullName = reader.GetString(1),
                            BookletType = reader.GetString(2),
                            RawAnswers = reader.GetString(3),
                            Score = reader.GetDouble(4),
                            CorrectCount = reader.GetInt32(5),
                            IncorrectCount = reader.GetInt32(6),
                            EmptyCount = reader.GetInt32(7)
                        };
                        res.Answers = new System.Collections.Generic.List<string>();
                        foreach(char c in res.RawAnswers) res.Answers.Add(c.ToString());
                        var qjson = reader.GetString(8);
                        res.QuestionResults = JsonSerializer.Deserialize<List<bool>>(qjson) ?? new List<bool>();
                        
                        // Recalculate ColoredAnswers for display
                        foreach(char c in res.RawAnswers)
                        {
                            res.ColoredAnswers.Add(new AnswerItem { Character = c == ' ' ? '_' : c, State = AnswerState.NotEvaluated });
                        }
                        
                        results.Add(res);
                    }
                }
            }
            return results;
        }

        public void DeleteExam(int examId)
        {
            using (var connection = new SqliteConnection($"Data Source={_dbPath}"))
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = "DELETE FROM Exams WHERE Id = @id";
                command.Parameters.AddWithValue("@id", examId);
                command.ExecuteNonQuery();
            }
        }
    }
}
