using System;
using System.Collections.Generic;
using System.Linq;
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
    /// Логика взаимодействия для CategoryTabPage.xaml
    /// </summary>
    public partial class CategoryTabPage : Page
    {
        public CategoryTabPage()
        {
            InitializeComponent();
            LoadCategories();
            this.IsVisibleChanged += Page_IsVisibleChanged;
        }

        private void LoadCategories()
        {
            try
            {
                using (var db = new Entities())
                {
                    DataGridCategory.ItemsSource = db.Category.ToList();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки категорий: {ex.Message}", "Ошибка");
            }
        }

        private void Page_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (Visibility == Visibility.Visible)
            {
                LoadCategories();
            }
        }

        private void ButtonAdd_Click(object sender, RoutedEventArgs e)
        {
            NavigationService?.Navigate(new AddCategoryPage(null));
        }

        private void ButtonEdit_Click(object sender, RoutedEventArgs e)
        {
            if (DataGridCategory.SelectedItem is Category selectedCategory)
            {
                NavigationService?.Navigate(new AddCategoryPage(selectedCategory));
            }
            else
            {
                MessageBox.Show("Выберите категорию для редактирования", "Информация");
            }
        }

        private void ButtonDel_Click(object sender, RoutedEventArgs e)
        {
            if (DataGridCategory.SelectedItem is Category selectedCategory)
            {
                var result = MessageBox.Show($"Вы точно хотите удалить категорию '{selectedCategory.Name}'?",
                    "Подтверждение удаления", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        using (var db = new Entities())
                        {
                            var categoryToDelete = db.Category.FirstOrDefault(c => c.ID == selectedCategory.ID);
                            if (categoryToDelete != null)
                            {
                                db.Category.Remove(categoryToDelete);
                                db.SaveChanges();
                                MessageBox.Show("Категория успешно удалена", "Успех");
                                LoadCategories();
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка удаления: {ex.Message}", "Ошибка");
                    }
                }
            }
            else
            {
                MessageBox.Show("Выберите категорию для удаления", "Информация");
            }
        }

        private void ButtonRefresh_Click(object sender, RoutedEventArgs e)
        {
            LoadCategories();
        }
    }
}
