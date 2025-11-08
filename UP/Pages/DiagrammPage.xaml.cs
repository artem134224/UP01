using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms.DataVisualization.Charting;
using Excel = Microsoft.Office.Interop.Excel;
using Word = Microsoft.Office.Interop.Word;

namespace UP.Pages
{
    public partial class DiagrammPage : Page
    {
        private readonly Entities _db = Entities.GetContext();
        private readonly CultureInfo _ru = new CultureInfo("ru-RU");

        public DiagrammPage()
        {
            InitializeComponent();
            InitChart();
            LoadFilters();
            BuildChart();
        }

        #region Chart init + data

        private void InitChart()
        {
            ChartPayments.Series.Clear();

            var series = new Series("Платежи по категориям")
            {
                ChartType = SeriesChartType.Column,
                IsValueShownAsLabel = true,
                LabelFormat = "C0",
                BorderWidth = 2,
                Color = Color.SteelBlue,
                MarkerStyle = MarkerStyle.Circle,
                MarkerSize = 7
            };
            series.ToolTip = "#VALX: #VALY{C0}"; // тултип "Категория: 12 345 ₽"

            ChartPayments.Series.Add(series);

            // чарта слегка светлая (чтобы хорошо смотрелась в тёмной теме)
            var area = ChartPayments.ChartAreas["mainArea"];
            area.BackColor = Color.White;
            area.AxisX.MajorGrid.Enabled = true;
            area.AxisY.MajorGrid.Enabled = true;
            area.AxisX.MajorGrid.LineColor = Color.Gainsboro;
            area.AxisY.MajorGrid.LineColor = Color.Gainsboro;
            area.AxisX.Interval = 1;

            ChartPayments.Legends["legend"].Enabled = true;
            ChartPayments.Titles.Clear();
            ChartPayments.Titles.Add("Платежи по категориям");
        }

        private void LoadFilters()
        {
            try
            {
                var users = _db.Users.ToList();
                users.Insert(0, new Users { ID = 0, FIO = "— Все пользователи —" });
                CmbUser.ItemsSource = users;
                CmbUser.SelectedIndex = 0;

                var types = new List<SeriesChartType>
                {
                    SeriesChartType.Column,
                    SeriesChartType.Bar,
                    SeriesChartType.Pie,
                    SeriesChartType.Line,
                    SeriesChartType.Area
                };
                CmbDiagram.ItemsSource = types;
                CmbDiagram.SelectedItem = SeriesChartType.Column;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки фильтров: " + ex.Message, "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private sealed class CatSum
        {
            public string Category { get; set; } = "";
            public decimal Total { get; set; }
        }

        private List<CatSum> GetCategorySums(int selectedUserId)
        {
            // все категории (чтобы показывать и нулевые)
            var cats = _db.Category
                          .Select(c => c.Name)
                          .ToList();

            var q = _db.Payment.AsQueryable();
            if (selectedUserId > 0)
                q = q.Where(p => p.UserID == selectedUserId);

            var sums = q.GroupBy(p => p.Category.Name)
                        .Select(g => new CatSum
                        {
                            Category = g.Key,
                            Total = g.Sum(p => p.Price * p.Num)
                        })
                        .ToList();

            // дополняем отсутствующие нулём
            var map = sums.ToDictionary(s => s.Category ?? "Без категории", s => s.Total);
            var result = new List<CatSum>();
            foreach (var c in cats)
            {
                var key = string.IsNullOrWhiteSpace(c) ? "Без категории" : c;
                result.Add(new CatSum
                {
                    Category = key,
                    Total = map.ContainsKey(key) ? map[key] : 0m
                });
            }

            // если в БД встретились платежи с пустой категорией, тоже учтём
            if (!string.IsNullOrWhiteSpace(null) && map.ContainsKey("Без категории") && !result.Any(r => r.Category == "Без категории"))
                result.Add(new CatSum { Category = "Без категории", Total = map["Без категории"] });

            return result.OrderByDescending(x => x.Total).ToList();
        }

        private void BuildChart()
        {
            try
            {
                var series = ChartPayments.Series[0];
                var area = ChartPayments.ChartAreas["mainArea"];
                series.Points.Clear();

                int selectedUserId = (CmbUser.SelectedValue is int) ? (int)CmbUser.SelectedValue : 0;

                var chartType = CmbDiagram.SelectedItem is SeriesChartType t ? t : SeriesChartType.Column;
                series.ChartType = chartType;

                var data = GetCategorySums(selectedUserId);

                foreach (var item in data)
                {
                    var dp = new DataPoint();
                    dp.SetValueXY(item.Category, item.Total);
                    dp.Label = item.Total.ToString("C0", _ru);
                    dp.Font = new Font("Segoe UI", 8f);
                    series.Points.Add(dp);
                }

                // заголовок с суммой
                decimal grand = data.Sum(d => d.Total);
                var selectedUser = CmbUser.SelectedItem as Users;
                string who = (selectedUser != null && selectedUser.ID != 0) ? selectedUser.FIO : "все пользователи";
                ChartPayments.Titles[0].Text = $"Платежи ({who}) — всего {grand.ToString("C0", _ru)}";

                // Pie — без сетки/осей
                bool isPie = chartType == SeriesChartType.Pie;
                area.AxisX.MajorGrid.Enabled = !isPie;
                area.AxisY.MajorGrid.Enabled = !isPie;
                area.AxisX.LabelStyle.Enabled = !isPie;
                area.AxisY.LabelStyle.Enabled = !isPie;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка построения диаграммы: " + ex.Message, "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!IsInitialized) return;
            BuildChart();
        }

        #endregion

        #region Export helpers

        private Tuple<Users, List<Category>, List<Payment>> PrepareExportData()
        {
            int userId = (CmbUser.SelectedValue is int) ? (int)CmbUser.SelectedValue : 0;

            Users user = null;
            if (userId > 0)
                user = _db.Users.FirstOrDefault(u => u.ID == userId);

            if (user == null)
                user = new Users { ID = 0, FIO = "Все пользователи" };

            List<Category> categories = _db.Category.OrderBy(c => c.Name).ToList();
            List<Payment> payments = _db.Payment
                                          .Where(p => userId == 0 || p.UserID == userId)
                                          .OrderBy(p => p.Date)
                                          .ToList();

            return Tuple.Create(user, categories, payments);
        }


        private static void ReleaseCom(object o)
        {
            try
            {
                if (o != null && Marshal.IsComObject(o))
                    Marshal.ReleaseComObject(o);
            }
            catch { /* ignore */ }
        }


        #endregion

        #region Export to Excel (по образцу)

        private void BtnExportExcel_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var (user, categories, payments) = PrepareExportData();

                var xl = new Excel.Application { Visible = true };
                Excel.Workbook wb = xl.Workbooks.Add();
                Excel.Worksheet ws = (Excel.Worksheet)wb.ActiveSheet;
                ws.Name = "Платежи";

                // заголовки
                int row = 1;
                ws.Cells[row, 1] = "Дата платежа";
                ws.Cells[row, 2] = "Название";
                ws.Cells[row, 3] = "Стоимость, руб.";
                ws.Cells[row, 4] = "Количество";
                ws.Cells[row, 5] = "Сумма";
                var header = ws.Range["A1", "E1"];
                header.Font.Bold = true;
                header.Interior.Color = ColorTranslator.ToOle(Color.FromArgb(230, 230, 230));
                header.Borders.LineStyle = Excel.XlLineStyle.xlContinuous;

                ws.Columns["A"].ColumnWidth = 14;
                ws.Columns["B"].ColumnWidth = 36;
                ws.Columns["C"].ColumnWidth = 14;
                ws.Columns["D"].ColumnWidth = 12;
                ws.Columns["E"].ColumnWidth = 14;

                row++;

                // по категориям
                foreach (var cat in categories)
                {
                    var items = payments.Where(p => p.CategoryID == cat.ID).ToList();
                    if (items.Count == 0) continue;

                    // заголовок раздела
                    ws.Range[$"A{row}", $"E{row}"].Merge();
                    ws.Cells[row, 1] = cat.Name;
                    var sec = ws.Range[$"A{row}", $"E{row}"];
                    sec.Font.Bold = true;
                    sec.Interior.Color = ColorTranslator.ToOle(Color.FromArgb(242, 242, 242));
                    sec.Borders.LineStyle = Excel.XlLineStyle.xlContinuous;
                    row++;

                    decimal subtotal = 0m;
                    foreach (var p in items)
                    {
                        decimal sum = p.Price * p.Num;
                        subtotal += sum;

                        ws.Cells[row, 1] = p.Date.ToString("dd.MM.yyyy");
                        ws.Cells[row, 2] = p.Name;
                        ws.Cells[row, 3] = p.Price;
                        ws.Cells[row, 4] = p.Num;
                        ws.Cells[row, 5] = sum;

                        var rng = ws.Range[$"A{row}", $"E{row}"];
                        rng.Borders.LineStyle = Excel.XlLineStyle.xlContinuous;
                        row++;
                    }

                    // строка ИТОГО
                    ws.Cells[row, 4] = "ИТОГО:";
                    ws.Cells[row, 5] = subtotal;
                    var tot = ws.Range[$"A{row}", $"E{row}"];
                    tot.Font.Bold = true;
                    tot.Interior.Color = ColorTranslator.ToOle(Color.FromArgb(235, 245, 255));
                    tot.Borders.LineStyle = Excel.XlLineStyle.xlContinuous;
                    row += 2;
                }

                // подпись пользователя
                ws.Range[$"A{row}", $"C{row}"].Merge();
                ws.Cells[row, 1] = user.FIO;
                var sign = ws.Range[$"A{row}", $"C{row}"];
                sign.Interior.Color = ColorTranslator.ToOle(Color.FromArgb(214, 233, 198)); // мягко-зелёный
                sign.Borders.LineStyle = Excel.XlLineStyle.xlContinuous;
                sign.Font.Bold = true;

                // форматы
                ws.Range["C2", $"C{row}"].NumberFormat = "#,##0.00";
                ws.Range["E2", $"E{row}"].NumberFormat = "#,##0.00";
                ws.Range["D2", $"D{row}"].NumberFormat = "0";

                // готово, Excel уже видим
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка экспорта в Excel: " + ex.Message, "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Export to Word (по образцу)

        private void BtnExportWord_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var (user, categories, payments) = PrepareExportData();

                var wrd = new Word.Application { Visible = true };
                Word.Document doc = wrd.Documents.Add();

                // Заголовок (ФИО)
                var pTitle = doc.Paragraphs.Add();
                pTitle.Range.Text = user.FIO;
                pTitle.Range.Font.Name = "Times New Roman";
                pTitle.Range.Font.Size = 28;
                pTitle.Range.Bold = 1;
                pTitle.Alignment = Word.WdParagraphAlignment.wdAlignParagraphCenter;
                pTitle.Range.InsertParagraphAfter();

                // Таблица Категория/Сумма расходов
                var data = categories
                    .Select(c => new
                    {
                        Category = c.Name,
                        Total = payments.Where(p => p.CategoryID == c.ID)
                                        .Sum(p => p.Price * p.Num)
                    })
                    .ToList();

                int rows = data.Count + 1; // + header
                int cols = 2;
                Word.Table tbl = doc.Tables.Add(doc.Paragraphs.Add().Range, rows, cols);
                tbl.Borders.Enable = 1;
                tbl.Range.Font.Name = "Times New Roman";
                tbl.Range.Font.Size = 12;

                // Заголовки
                tbl.Cell(1, 1).Range.Text = "Категория";
                tbl.Cell(1, 2).Range.Text = "Сумма расходов";
                tbl.Rows[1].Range.Bold = 1;
                tbl.Rows[1].Range.ParagraphFormat.Alignment = Word.WdParagraphAlignment.wdAlignParagraphCenter;

                // Строки
                for (int i = 0; i < data.Count; i++)
                {
                    tbl.Cell(i + 2, 1).Range.Text = data[i].Category;
                    tbl.Cell(i + 2, 2).Range.Text = data[i].Total.ToString("N2", _ru) + " руб.";
                }

                // Самый дорогой / дешёвый платёж
                if (payments.Count > 0)
                {
                    var maxPay = payments
                        .OrderByDescending(p => p.Price * p.Num)
                        .First();
                    var minPay = payments
                        .OrderBy(p => p.Price * p.Num)
                        .First();

                    string maxText =
                        $"Самый дорогостоящий платеж — {maxPay.Name} за {(maxPay.Price * maxPay.Num).ToString("N2", _ru)} руб., от {maxPay.Date:dd.MM.yyyy}.";
                    string minText =
                        $"Самый дешевый платеж — {minPay.Name} на {(minPay.Price * minPay.Num).ToString("N2", _ru)} руб., от {minPay.Date:dd.MM.yyyy}.";

                    var p1 = doc.Paragraphs.Add();
                    p1.Range.Text = maxText;
                    p1.Range.Font.Color = Word.WdColor.wdColorDarkRed;
                    p1.Range.InsertParagraphAfter();

                    var p2 = doc.Paragraphs.Add();
                    p2.Range.Text = minText;
                    p2.Range.Font.Color = Word.WdColor.wdColorDarkBlue;
                    p2.Range.InsertParagraphAfter();
                }

                // авто-подбор содержимого
                doc.Content.ParagraphFormat.SpaceAfter = 8;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка экспорта в Word: " + ex.Message, "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion
    }
}
