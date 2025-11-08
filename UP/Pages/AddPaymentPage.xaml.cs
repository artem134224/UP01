using System;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace UP.Pages
{
    /// <summary>
    /// Логика взаимодействия для AddPaymentPage.xaml
    /// </summary>
    public partial class AddPaymentPage : Page
    {
        private Payment _currentPayment;

        public AddPaymentPage(Payment selectedPayment)
        {
            InitializeComponent();

            if (selectedPayment != null)
            {
                _currentPayment = selectedPayment;      // редактирование
                Title = "Редактирование платежа";
            }
            else
            {
                _currentPayment = new Payment();        // добавление
                _currentPayment.Date = DateTime.Now;
                Title = "Добавление платежа";
            }

            LoadComboBoxData();

            // Важно: DataContext после источников ComboBox'ов
            DataContext = _currentPayment;

            // Пересчет "Итого"
            TBNum.TextChanged += OnAmountChanged;
            TBPrice.TextChanged += OnAmountChanged;
            UpdateTotalAmount();
        }

        private void LoadComboBoxData()
        {
            try
            {
                var db = Entities.GetContext();

                CBUser.ItemsSource = db.Users.ToList();
                CBCategory.ItemsSource = db.Category.ToList();

                if (_currentPayment.Date == DateTime.MinValue)
                    DPDate.SelectedDate = DateTime.Now;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки данных: " + ex.Message, "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OnAmountChanged(object sender, TextChangedEventArgs e)
        {
            UpdateTotalAmount();
        }

        private void UpdateTotalAmount()
        {
            int num;
            decimal price;

            var numOk = int.TryParse(TBNum.Text, NumberStyles.Integer, CultureInfo.CurrentCulture, out num);
            var priceOk = decimal.TryParse(TBPrice.Text, NumberStyles.Number, CultureInfo.CurrentCulture, out price);

            if (numOk && priceOk && num > 0 && price >= 0)
                TotalAmountText.Text = "Общая сумма: " + (num * price).ToString("C", CultureInfo.CurrentCulture);
            else
                TotalAmountText.Text = "Общая сумма: 0 ₽";
        }

        private void ButtonSave_Click(object sender, RoutedEventArgs e)
        {
            var errors = new StringBuilder();

            if (CBUser.SelectedItem == null)
                errors.AppendLine("Выберите пользователя.");
            if (CBCategory.SelectedItem == null)
                errors.AppendLine("Выберите категорию.");
            if (string.IsNullOrWhiteSpace(_currentPayment.Name))
                errors.AppendLine("Укажите название платежа.");
            if (!DPDate.SelectedDate.HasValue)
                errors.AppendLine("Укажите дату.");

            int num;
            decimal price;

            if (!int.TryParse(TBNum.Text, out num) || num <= 0)
                errors.AppendLine("Укажите корректное количество (> 0).");
            if (!decimal.TryParse(TBPrice.Text, out price) || price < 0)
                errors.AppendLine("Укажите корректную цену (≥ 0).");

            if (errors.Length > 0)
            {
                MessageBox.Show(errors.ToString(), "Ошибка валидации",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var db = Entities.GetContext();

                // Обновляем поля (если бинды не успели)
                _currentPayment.UserID = (int)CBUser.SelectedValue;
                _currentPayment.CategoryID = (int)CBCategory.SelectedValue;
                _currentPayment.Num = num;
                _currentPayment.Price = price;
                _currentPayment.Date = DPDate.SelectedDate.HasValue
                    ? DPDate.SelectedDate.Value
                    : DateTime.Now;

                if (_currentPayment.ID == 0)
                {
                    // новая запись
                    db.Payment.Add(_currentPayment);
                }
                else
                {
                    // редактирование: копируем в трекаемую сущность
                    var existing = db.Payment.FirstOrDefault(p => p.ID == _currentPayment.ID);
                    if (existing != null)
                    {
                        existing.UserID = _currentPayment.UserID;
                        existing.CategoryID = _currentPayment.CategoryID;
                        existing.Name = _currentPayment.Name;
                        existing.Date = _currentPayment.Date;
                        existing.Num = _currentPayment.Num;
                        existing.Price = _currentPayment.Price;
                    }
                }

                db.SaveChanges();

                MessageBox.Show("Платёж сохранён", "Готово",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                if (NavigationService != null && NavigationService.CanGoBack)
                    NavigationService.GoBack();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка сохранения: " + ex.Message, "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ButtonCancel_Click(object sender, RoutedEventArgs e)
        {
            if (NavigationService != null && NavigationService.CanGoBack)
                NavigationService.GoBack();
        }
    }
}
