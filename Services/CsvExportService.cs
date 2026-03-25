using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using OptikFormApp.Models;

namespace OptikFormApp.Services
{
    public class CsvExportService
    {
        /// <summary>
        /// Öğrenci sonuçlarını UTF-8 BOM'lu CSV olarak kaydeder.
        /// BOM sayesinde Excel Türkçe karakterleri doğru açar.
        /// </summary>
        public void ExportToCsv(List<StudentResult> students, string filePath)
        {
            var sb = new StringBuilder();

            // Başlık satırı
            sb.AppendLine("Sıra (Derece);Öğrenci No;Ad Soyad;Kitapçık;Doğru;Yanlış;Boş;Net;Puan;Cevaplar");

            foreach (var s in students)
            {
                sb.AppendLine(string.Join(";",
                    s.Rank,
                    Escape(s.StudentId),
                    Escape(s.FullName),
                    Escape(s.BookletType),
                    s.CorrectCount,
                    s.IncorrectCount,
                    s.EmptyCount,
                    s.NetCount.ToString("F2"),
                    s.Score.ToString("F2"),
                    Escape(s.RawAnswers)
                ));
            }

            // UTF-8 BOM — Excel'in Türkçe karakterleri düzgün okuyabilmesi için
            File.WriteAllText(filePath, sb.ToString(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
        }

        /// <summary>
        /// Yalnızca öğrenci numarası ve puanı içeren not listesi CSV'si.
        /// Sisteme aktarım veya not giriş formu için idealdir.
        /// </summary>
        public void ExportGradeList(List<StudentResult> students, string filePath)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Öğrenci No;Puan");

            foreach (var s in students)
            {
                sb.AppendLine($"{Escape(s.StudentId)};{s.Score.ToString("F2")}");
            }

            File.WriteAllText(filePath, sb.ToString(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
        }

        private static string Escape(string value)
        {
            if (string.IsNullOrEmpty(value)) return "";
            // Noktalı virgül veya tırnak içeriyorsa çift tırnak içine al
            if (value.Contains(';') || value.Contains('"') || value.Contains('\n'))
                return "\"" + value.Replace("\"", "\"\"") + "\"";
            return value;
        }
    }
}
