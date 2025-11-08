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
    /// Логика взаимодействия для AddUserPage.xaml
    /// </summary>
    public partial class AddUserPage : Page
    {
        private Users _currentUser = new Users();

        public AddUserPage(Users selectedUser)
        {
            InitializeComponent();

            if (selectedUser != null)
            {
                _currentUser = selectedUser;
                Title = "Редактирование пользователя";
            }
            else
            {
                Title = "Добавление пользователя";
            }

            DataContext = _currentUser;
        }

        public static string GetHash(string password)
        {
            using (var hash = SHA1.Create())
            {
                return string.Concat(hash.ComputeHash(Encoding.UTF8.GetBytes(password))
                    .Select(x => x.ToString("X2")));
            }
        }

        private void ButtonSave_Click(object sender, RoutedEventArgs e)
        {
            StringBuilder errors = new StringBuilder();

            if (string.IsNullOrWhiteSpace(_currentUser.Login))
                errors.AppendLine("Укажите логин");

            if (string.IsNullOrWhiteSpace(TBPassword.Password) && _currentUser.ID == 0)
                errors.AppendLine("Укажите пароль");

            if (string.IsNullOrWhiteSpace(_currentUser.FIO))
                errors.AppendLine("Укажите ФИО");

            if (cmbRole.SelectedItem == null)
                errors.AppendLine("Выберите роль");

            if (errors.Length > 0)
            {
                MessageBox.Show(errors.ToString(), "Ошибка валидации",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using (var db = new Entities())
                {
                    if (!string.IsNullOrWhiteSpace(TBPassword.Password))
                    {
                        _currentUser.Password = GetHash(TBPassword.Password);
                    }

                    if (_currentUser.ID == 0)
                    {
                        db.Users.Add(_currentUser);
                    }
                    else
                    {
                        var existingUser = db.Users.FirstOrDefault(u => u.ID == _currentUser.ID);
                        if (existingUser != null)
                        {
                            existingUser.Login = _currentUser.Login;
                            existingUser.FIO = _currentUser.FIO;
                            existingUser.Role = (cmbRole.SelectedItem as ComboBoxItem)?.Content.ToString();
                            existingUser.Photo = _currentUser.Photo;

                            if (!string.IsNullOrWhiteSpace(TBPassword.Password))
                            {
                                existingUser.Password = _currentUser.Password;
                            }
                        }
                    }

                    db.SaveChanges();
                    MessageBox.Show("Данные успешно сохранены", "Успех");
                    NavigationService?.GoBack();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения: {ex.Message}", "Ошибка");
            }
        }

        private void ButtonCancel_Click(object sender, RoutedEventArgs e)
        {
            NavigationService?.GoBack();
        }
    }
}
