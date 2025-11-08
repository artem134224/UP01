using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace UP.Pages
{
    /// <summary>
    /// Логика взаимодействия для RegPage.xaml
    /// </summary>
    public partial class RegPage : Page
    {
        public RegPage()
        {
            InitializeComponent();
            comboBxRole.SelectedIndex = 0;
        }

        public static string GetHash(string password)
        {
            using (var hash = SHA1.Create())
            {
                return string.Concat(hash.ComputeHash(Encoding.UTF8.GetBytes(password))
                    .Select(x => x.ToString("X2")));
            }
        }

        private void regButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(txtbxLog.Text) ||
                string.IsNullOrEmpty(txtbxFIO.Text) ||
                string.IsNullOrEmpty(passBxFrst.Password) ||
                string.IsNullOrEmpty(passBxScnd.Password))
            {
                MessageBox.Show("Заполните все поля!", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            using (var db = new Entities())
            {
                var user = db.Users
                    .AsNoTracking()
                    .FirstOrDefault(u => u.Login == txtbxLog.Text);

                if (user != null)
                {
                    MessageBox.Show("Пользователь с таким логином уже существует!", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }

            if (!ValidatePassword(passBxFrst.Password))
            {
                return;
            }

            if (passBxFrst.Password != passBxScnd.Password)
            {
                MessageBox.Show("Пароли не совпадают!", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                using (var db = new Entities())
                {
                    Users newUser = new Users
                    {
                        FIO = txtbxFIO.Text,
                        Login = txtbxLog.Text,
                        Password = GetHash(passBxFrst.Password),
                        Role = (comboBxRole.SelectedItem as ComboBoxItem)?.Content.ToString()
                    };

                    db.Users.Add(newUser);
                    db.SaveChanges();

                    MessageBox.Show("Пользователь успешно зарегистрирован!", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);

                    txtbxLog.Clear();
                    passBxFrst.Clear();
                    passBxScnd.Clear();
                    comboBxRole.SelectedIndex = 0;
                    txtbxFIO.Clear();

                    NavigationService?.Navigate(new AuthPage());
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при регистрации: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool ValidatePassword(string password)
        {
            if (password.Length < 6)
            {
                MessageBox.Show("Пароль слишком короткий, должно быть минимум 6 символов!",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            bool hasEnglish = true;
            bool hasNumber = false;

            foreach (char c in password)
            {
                if (c >= '0' && c <= '9')
                    hasNumber = true;
                else if (!((c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z')))
                    hasEnglish = false;
            }

            if (!hasEnglish)
            {
                MessageBox.Show("Используйте только английскую раскладку!", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (!hasNumber)
            {
                MessageBox.Show("Добавьте хотя бы одну цифру!", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            return true;
        }

        private void txtbxLog_TextChanged(object sender, TextChangedEventArgs e)
        {
            // placeholder
        }

        private void txtbxFIO_TextChanged(object sender, TextChangedEventArgs e)
        {
            // placeholder
        }

        private void passBxFrst_PasswordChanged(object sender, RoutedEventArgs e)
        {
            // placeholder
        }

        private void passBxScnd_PasswordChanged(object sender, RoutedEventArgs e)
        {
            // placeholder
        }
    }
}