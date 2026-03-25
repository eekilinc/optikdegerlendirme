using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Data.Sqlite;
using OptikFormApp.Models;
using System.Text.Json;

namespace OptikFormApp.Services
{
    /// <summary>
    /// SQLite veritabanı erişim katmanı.
    /// IDisposable ile tek bir connection paylaşılır; her metod
    /// bu connection üzerinde çalışır. Uygulama ömrü boyunca
    /// tek bir DatabaseService instance kullanılması önerilir.
    /// </summary>
    public class DatabaseService : IDisposable
    {
        private readonly string _dbPath;
        private readonly SqliteConnection _connection;
        private bool _disposed;

        public DatabaseService()
        {
            _dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "optik.db");
            _connection = new SqliteConnection($"Data Source={_dbPath}");
            _connection.Open();
            EnableForeignKeys();
            InitializeDatabase();
        }

        // ── Yardımcı ──────────────────────────────────────────────────────────

        private void EnableForeignKeys()
        {
            using var cmd = _connection.CreateCommand();
            cmd.CommandText = "PRAGMA foreign_keys = ON;";
            cmd.ExecuteNonQuery();
        }

        private SqliteCommand CreateCommand(string sql)
        {
            var cmd = _connection.CreateCommand();
            cmd.CommandText = sql;
            return cmd;
        }

        private void InitializeDatabase()
        {
            using var cmd = CreateCommand(@"
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
                );");
            cmd.ExecuteNonQuery();
        }

        // ── Courses ───────────────────────────────────────────────────────────

        public List<Course> GetCourses()
        {
            var courses = new List<Course>();
            using var cmd = CreateCommand("SELECT Id, Code, Name FROM Courses ORDER BY Name");
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                courses.Add(new Course
                {
                    Id = reader.GetInt32(0),
                    Code = reader.IsDBNull(1) ? "" : reader.GetString(1),
                    Name = reader.GetString(2)
                });
            }
            return courses;
        }

        public int SaveCourse(Course course)
        {
            if (course.Id == 0)
            {
                using var cmd = CreateCommand("INSERT INTO Courses (Code, Name) VALUES (@code, @name); SELECT last_insert_rowid();");
                cmd.Parameters.AddWithValue("@code", course.Code);
                cmd.Parameters.AddWithValue("@name", course.Name);
                return Convert.ToInt32(cmd.ExecuteScalar());
            }
            else
            {
                using var cmd = CreateCommand("UPDATE Courses SET Code=@code, Name=@name WHERE Id=@id");
                cmd.Parameters.AddWithValue("@code", course.Code);
                cmd.Parameters.AddWithValue("@name", course.Name);
                cmd.Parameters.AddWithValue("@id", course.Id);
                cmd.ExecuteNonQuery();
                return course.Id;
            }
        }

        public void DeleteCourse(int courseId)
        {
            using var cmd = CreateCommand("DELETE FROM Courses WHERE Id = @id");
            cmd.Parameters.AddWithValue("@id", courseId);
            cmd.ExecuteNonQuery();
        }

        public void RenameCourse(int courseId, string newCode, string newName)
        {
            using var cmd = CreateCommand("UPDATE Courses SET Code = @code, Name = @name WHERE Id = @id");
            cmd.Parameters.AddWithValue("@code", newCode);
            cmd.Parameters.AddWithValue("@name", newName);
            cmd.Parameters.AddWithValue("@id", courseId);
            cmd.ExecuteNonQuery();
        }

        // ── Exams ─────────────────────────────────────────────────────────────

        public void RenameExam(int examId, string newTitle)
        {
            using var cmd = CreateCommand("UPDATE Exams SET Title = @title WHERE Id = @id");
            cmd.Parameters.AddWithValue("@title", newTitle);
            cmd.Parameters.AddWithValue("@id", examId);
            cmd.ExecuteNonQuery();
        }

        public List<ExamEntry> GetExamsForCourse(int courseId)
        {
            var exams = new List<ExamEntry>();
            using var cmd = CreateCommand("SELECT Id, CourseId, Title, ExamDate, ConfigJson FROM Exams WHERE CourseId = @cid ORDER BY ExamDate DESC");
            cmd.Parameters.AddWithValue("@cid", courseId);
            using var reader = cmd.ExecuteReader();
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
            return exams;
        }

        public int SaveExam(ExamEntry exam, List<StudentResult> results)
        {
            using var transaction = _connection.BeginTransaction();
            try
            {
                using var cmd = CreateCommand(
                    "INSERT INTO Exams (CourseId, Title, ExamDate, ConfigJson) VALUES (@cid, @title, @date, @config); SELECT last_insert_rowid();");
                cmd.Transaction = transaction;
                cmd.Parameters.AddWithValue("@cid", exam.CourseId);
                cmd.Parameters.AddWithValue("@title", exam.Title);
                cmd.Parameters.AddWithValue("@date", exam.Date.ToString("o"));
                cmd.Parameters.AddWithValue("@config", exam.ConfigJson);
                int examId = Convert.ToInt32(cmd.ExecuteScalar());

                InsertResults(transaction, examId, results);
                transaction.Commit();
                return examId;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public void UpdateExam(ExamEntry exam, List<StudentResult> results)
        {
            using var transaction = _connection.BeginTransaction();
            try
            {
                using (var updateCmd = CreateCommand("UPDATE Exams SET Title=@title, ConfigJson=@config WHERE Id=@id"))
                {
                    updateCmd.Transaction = transaction;
                    updateCmd.Parameters.AddWithValue("@title", exam.Title);
                    updateCmd.Parameters.AddWithValue("@config", exam.ConfigJson);
                    updateCmd.Parameters.AddWithValue("@id", exam.Id);
                    updateCmd.ExecuteNonQuery();
                }

                using (var delCmd = CreateCommand("DELETE FROM ExamResults WHERE ExamId = @eid"))
                {
                    delCmd.Transaction = transaction;
                    delCmd.Parameters.AddWithValue("@eid", exam.Id);
                    delCmd.ExecuteNonQuery();
                }

                InsertResults(transaction, exam.Id, results);
                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        private void InsertResults(SqliteTransaction transaction, int examId, List<StudentResult> results)
        {
            const string sql = @"
                INSERT INTO ExamResults
                    (ExamId, StudentId, FullName, BookletType, RawAnswers, Score, CorrectCount, IncorrectCount, EmptyCount, QuestionResultsJson)
                VALUES
                    (@eid, @sid, @name, @booklet, @raw, @score, @corr, @inc, @emp, @qjson)";

            foreach (var res in results)
            {
                using var cmd = CreateCommand(sql);
                cmd.Transaction = transaction;
                cmd.Parameters.AddWithValue("@eid", examId);
                cmd.Parameters.AddWithValue("@sid", res.StudentId);
                cmd.Parameters.AddWithValue("@name", res.FullName);
                cmd.Parameters.AddWithValue("@booklet", res.BookletType);
                cmd.Parameters.AddWithValue("@raw", res.RawAnswers);
                cmd.Parameters.AddWithValue("@score", res.Score);
                cmd.Parameters.AddWithValue("@corr", res.CorrectCount);
                cmd.Parameters.AddWithValue("@inc", res.IncorrectCount);
                cmd.Parameters.AddWithValue("@emp", res.EmptyCount);
                cmd.Parameters.AddWithValue("@qjson", JsonSerializer.Serialize(res.QuestionResults));
                cmd.ExecuteNonQuery();
            }
        }

        public List<StudentResult> GetResultsForExam(int examId)
        {
            var results = new List<StudentResult>();
            using var cmd = CreateCommand(@"
                SELECT StudentId, FullName, BookletType, RawAnswers, Score,
                       CorrectCount, IncorrectCount, EmptyCount, QuestionResultsJson
                FROM ExamResults WHERE ExamId = @eid");
            cmd.Parameters.AddWithValue("@eid", examId);
            using var reader = cmd.ExecuteReader();
            int counter = 1;
            while (reader.Read())
            {
                var res = new StudentResult
                {
                    RowNumber    = counter++,
                    StudentId    = reader.GetString(0),
                    FullName     = reader.GetString(1),
                    BookletType  = reader.GetString(2),
                    RawAnswers   = reader.GetString(3),
                    Score        = reader.GetDouble(4),
                    CorrectCount = reader.GetInt32(5),
                    IncorrectCount = reader.GetInt32(6),
                    EmptyCount   = reader.GetInt32(7)
                };

                foreach (char c in res.RawAnswers)
                {
                    res.Answers.Add(c.ToString());
                    res.ColoredAnswers.Add(new AnswerItem { Character = c == ' ' ? '_' : c, State = AnswerState.NotEvaluated });
                }

                var qjson = reader.GetString(8);
                res.QuestionResults = JsonSerializer.Deserialize<List<bool>>(qjson) ?? new List<bool>();

                results.Add(res);
            }
            return results;
        }

        public void DeleteExam(int examId)
        {
            using var cmd = CreateCommand("DELETE FROM Exams WHERE Id = @id");
            cmd.Parameters.AddWithValue("@id", examId);
            cmd.ExecuteNonQuery();
        }

        // ── IDisposable ───────────────────────────────────────────────────────

        public void Dispose()
        {
            if (_disposed) return;
            _connection?.Close();
            _connection?.Dispose();
            _disposed = true;
            GC.SuppressFinalize(this);
        }
    }
}
