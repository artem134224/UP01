using System;
// EF6 Include по лямбдам:
using System.Data.Entity;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace UP.Pages
{
    public partial class PaymentTabPage : Page
    {
        public PaymentTabPage()
        {
            InitializeComponent();
            LoadPayments();
            IsVisibleChanged += Page_IsVisibleChanged;
        }

        private void LoadPayments()
        {
            try
            {
                var db = Entities.GetContext(); // единый контекст

                var payments = db.Payment
                    .Include(p => p.Users)     // ВАЖНО: Users (а не User)
                    .Include(p => p.Category)
                    .ToList();

                DataGridPayment.ItemsSource = payments;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки платежей: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Page_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (Visibility == Visibility.Visible)
                LoadPayments();
        }

        private void ButtonAdd_Click(object sender, RoutedEventArgs e)
        {
            NavigationService?.Navigate(new AddPaymentPage(null));
        }

        private void ButtonEdit_Click(object sender, RoutedEventArgs e)
        {
            if (DataGridPayment.SelectedItem is Payment selected)
            {
                // передаём сущность на редактирование (или можно передать её ID)
                NavigationService?.Navigate(new AddPaymentPage(selected));
            }
            else
            {
                MessageBox.Show("Выберите платеж для редактирования", "Информация",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void ButtonDel_Click(object sender, RoutedEventArgs e)
        {
            if (DataGridPayment.SelectedItem is Payment selected)
            {
                var result = MessageBox.Show(
                    $"Удалить платеж «{selected.Name}»?",
                    "Подтверждение удаления",
                    MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        var db = Entities.GetContext();

                        var toRemove = db.Payment.FirstOrDefault(p => p.ID == selected.ID);
                        if (toRemove != null)
                        {
                            db.Payment.Remove(toRemove);
                            db.SaveChanges();
                            LoadPayments();
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка удаления: {ex.Message}", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            else
            {
                MessageBox.Show("Выберите платеж для удаления", "Информация",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void ButtonRefresh_Click(object sender, RoutedEventArgs e) => LoadPayments();
    }

    /// <summary>
    /// Конвертер для вычисления суммы: Price * Num (без VM)
    /// </summary>
    

    public class MultiplyConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length < 2) return null;
            try
            {
                decimal price = System.Convert.ToDecimal(values[0] ?? 0);
                decimal num = System.Convert.ToDecimal(values[1] ?? 0);
                return price * num;
            }
            catch
            {
                return null;
            }
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
