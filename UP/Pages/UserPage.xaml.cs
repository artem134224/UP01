using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace UP.Pages
{
    public partial class UserPage : Page
    {
        private readonly Users _currentUser;
        private List<Users> _allUsers = new List<Users>();

        public UserPage(Users user)
        {
            InitializeComponent();
            _currentUser = user;
            LoadUsers();
        }

        private void LoadUsers()
        {
            try
            {
                var db = Entities.GetContext();   // единая точка входа
                _allUsers = db.Users.ToList();    // можно AsNoTracking(), если есть
                UpdateUsers();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки пользователей: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateUsers()
        {
            if (!IsInitialized) return;

            try
            {
                IEnumerable<Users> q = _allUsers;

                // Фильтр по ФИО
                var mask = fioFilterTextBox.Text?.Trim();
                if (!string.IsNullOrEmpty(mask))
                {
                    var m = mask.ToLowerInvariant();
                    q = q.Where(x => (x.FIO ?? string.Empty).ToLowerInvariant().Contains(m));
                }

                // Только админы
                if (onlyAdminCheckBox.IsChecked == true)
                {
                    q = q.Where(x => string.Equals(x.Role, "Admin", StringComparison.OrdinalIgnoreCase));
                }

                // Сортировка по ФИО
                q = (sortComboBox.SelectedIndex == 0)
                    ? q.OrderBy(x => x.FIO ?? "\uFFFF")       // null в конец
                    : q.OrderByDescending(x => x.FIO ?? "");  // null в начало

                ListUser.ItemsSource = q.ToList();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка обновления данных: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void fioFilterTextBox_TextChanged(object sender, TextChangedEventArgs e) => UpdateUsers();
        private void sortComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e) => UpdateUsers();
        private void onlyAdminCheckBox_Checked(object sender, RoutedEventArgs e) => UpdateUsers();
        private void onlyAdminCheckBox_Unchecked(object sender, RoutedEventArgs e) => UpdateUsers();

        private void clearFiltersButton_Click(object sender, RoutedEventArgs e)
        {
            fioFilterTextBox.Text = string.Empty;
            sortComboBox.SelectedIndex = 0;
            onlyAdminCheckBox.IsChecked = false;
            UpdateUsers();
        }
    }
}
