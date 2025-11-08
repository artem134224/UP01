using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace UP
{
    public class DataExporter
    {
        public void ExportToExcel(Entities context)
        {
            try
            {

                MessageBox.Show("Экспорт в Excel будет реализован после установки необходимых библиотек", "Информация");

                var excelApp = new Microsoft.Office.Interop.Excel.Application();
                excelApp.Visible = true;
                var workbook = excelApp.Workbooks.Add();
                var worksheet = workbook.ActiveSheet;

                worksheet.Cells[1, 1] = "ФИО";
                worksheet.Cells[1, 2] = "Категория";
                worksheet.Cells[1, 3] = "Сумма платежей";

                var data = context.Users
                    .SelectMany(u => u.Payment)
                    .GroupBy(p => new { p.Users.FIO, p.Category.Name })
                    .Select(g => new
                    {
                        UserName = g.Key.FIO,
                        Category = g.Key.Name,
                        Total = g.Sum(p => p.Price * p.Num)
                    })
                    .ToList();

                int row = 2;
                foreach (var item in data)
                {
                    worksheet.Cells[row, 1] = item.UserName;
                    worksheet.Cells[row, 2] = item.Category;
                    worksheet.Cells[row, 3] = item.Total;
                    row++;
                }
                
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка экспорта в Excel: {ex.Message}");
            }
        }

        public void ExportToWord(Entities context)
        {
            try
            {

                MessageBox.Show("Экспорт в Word будет реализован после установки необходимых библиотек", "Информация");

                
                var wordApp = new Microsoft.Office.Interop.Word.Application();
                wordApp.Visible = true;
                var document = wordApp.Documents.Add();

                var paragraph = document.Paragraphs.Add();
                paragraph.Range.Text = "Отчет по платежам";
                paragraph.Range.Font.Bold = 1;
                paragraph.Range.Font.Size = 16;
                paragraph.Range.InsertParagraphAfter();

                var users = context.Users.ToList();
                foreach (var user in users)
                {
                    var userParagraph = document.Paragraphs.Add();
                    userParagraph.Range.Text = user.FIO;
                    userParagraph.Range.Font.Bold = 1;
                    userParagraph.Range.Font.Size = 14;
                    userParagraph.Range.InsertParagraphAfter();

                    var payments = user.Payment.ToList();
                    foreach (var payment in payments)
                    {
                        var paymentParagraph = document.Paragraphs.Add();
                        paymentParagraph.Range.Text = $"{payment.Category.Name}: {payment.Name} - {payment.Price * payment.Num:C}";
                        paymentParagraph.Range.InsertParagraphAfter();
                    }
                }
                
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка экспорта в Word: {ex.Message}");
            }
        }

        public void ExportToCsv(Entities context, string filePath)
        {
            try
            {
                using (var writer = new StreamWriter(filePath))
                {
                    writer.WriteLine("ФИО;Категория;Название платежа;Дата;Количество;Цена;Общая сумма");

                    var payments = context.Payment
                        .Include("User")
                        .Include("Category")
                        .ToList();

                    foreach (var payment in payments)
                    {
                        writer.WriteLine(
                            $"{payment.Users.FIO};" +
                            $"{payment.Category.Name};" +
                            $"{payment.Name};" +
                            $"{payment.Date:dd.MM.yyyy};" +
                            $"{payment.Num};" +
                            $"{payment.Price};" +
                            $"{payment.Price * payment.Num}");
                    }
                }

                MessageBox.Show($"Данные успешно экспортированы в CSV файл: {filePath}", "Успех");
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка экспорта в CSV: {ex.Message}");
            }
        }
    }
}
