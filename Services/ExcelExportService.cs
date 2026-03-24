using System;
using System.Collections.Generic;
using ClosedXML.Excel;
using OptikFormApp.Models;

namespace OptikFormApp.Services
{
    public class ExcelExportService
    {
        public void ExportToExcel(List<StudentResult> students, string filePath)
        {
            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Sınav Sonuçları");

                // Headers
                worksheet.Cell(1, 1).Value = "TC / Öğrenci No";
                worksheet.Cell(1, 2).Value = "Ad Soyad";
                worksheet.Cell(1, 3).Value = "Kitapçık";
                worksheet.Cell(1, 4).Value = "Doğru";
                worksheet.Cell(1, 5).Value = "Yanlış";
                worksheet.Cell(1, 6).Value = "Boş";
                worksheet.Cell(1, 7).Value = "Puan";

                // Styling Headers
                var headerRange = worksheet.Range("A1:G1");
                headerRange.Style.Font.Bold = true;
                headerRange.Style.Fill.BackgroundColor = XLColor.AirForceBlue;
                headerRange.Style.Font.FontColor = XLColor.White;

                // Data
                int row = 2;
                foreach (var student in students)
                {
                    worksheet.Cell(row, 1).Value = student.StudentId;
                    worksheet.Cell(row, 2).Value = student.FullName;
                    worksheet.Cell(row, 3).Value = student.BookletType;
                    worksheet.Cell(row, 4).Value = student.CorrectCount;
                    worksheet.Cell(row, 5).Value = student.IncorrectCount;
                    worksheet.Cell(row, 6).Value = student.EmptyCount;
                    
                    // ClosedXML handles numbering format easily
                    worksheet.Cell(row, 7).Value = student.Score;
                    worksheet.Cell(row, 7).Style.NumberFormat.Format = "0.00";

                    row++;
                }

                worksheet.Columns().AdjustToContents();
                workbook.SaveAs(filePath);
            }
        }
    }
}
