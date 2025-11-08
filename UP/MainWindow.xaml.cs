using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace UP
{
    public partial class MainWindow : Window
    {
        private DispatcherTimer _clock;

        public MainWindow()
        {
            InitializeComponent();

            // Стартовая страница
            MainFrame.Navigate(new Pages.AuthPage());
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Часы в статус-баре
            _clock = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _clock.Tick += (_, __) => DateTimeNow.Text = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss");
            _clock.Start();

            // Применяем тему из ComboBox при запуске
            if (ThemeSelector.SelectedItem is ComboBoxItem cbi && cbi.Tag is string tag)
                ApplyTheme(tag);
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // Подтверждение выхода
            var res = MessageBox.Show(
                "Вы уверены, что хотите закрыть приложение?",
                "Подтверждение",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (res == MessageBoxResult.No)
            {
                e.Cancel = true;
            }
            else
            {
                _clock?.Stop();
            }
        }

        private void BtnBack_Click(object sender, RoutedEventArgs e)
        {
            if (MainFrame.CanGoBack)
                MainFrame.GoBack();
        }

        private void MainFrame_ContentRendered(object sender, EventArgs e)
        {
            // Кнопка "Назад" видна только если есть куда вернуться
            BtnBack.Visibility = MainFrame.CanGoBack ? Visibility.Visible : Visibility.Collapsed;
        }

        private void ThemeSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ThemeSelector.SelectedItem is ComboBoxItem cbi && cbi.Tag is string tag)
                ApplyTheme(tag); // "Light" | "Dark"
        }


        private static void ApplyTheme(string theme)
        {
            var res = Application.Current.Resources;

            string prefix = string.Equals(theme, "Dark", StringComparison.OrdinalIgnoreCase) ? "Dark" : "Light";

            Color Bk = (Color)res[$"{prefix}.Background"];
            Color Fg = (Color)res[$"{prefix}.Foreground"];
            Color Sf = (Color)res[$"{prefix}.Surface"];
            Color Ac = (Color)res[$"{prefix}.Accent"];
            Color AcD = (Color)res[$"{prefix}.AccentDark"];
            Color Br = (Color)res[$"{prefix}.Border"];

            UpdateBrush(res, "AppBackground", Bk);
            UpdateBrush(res, "AppForeground", Fg);
            UpdateBrush(res, "AppSurface", Sf);
            UpdateBrush(res, "AppAccent", Ac);
            UpdateBrush(res, "AppAccentDark", AcD);
            UpdateBrush(res, "AppBorder", Br);
        }

        private static void UpdateBrush(ResourceDictionary res, string key, Color color)
        {
            if (res[key] is SolidColorBrush b)
            {
                // Если заморожена — клонируем, меняем цвет и подменяем ресурс
                if (b.IsFrozen)
                {
                    var nb = b.Clone();
                    nb.Color = color;

                    res[key] = nb;
                }
                else
                {
                    b.Color = color;
                }
            }
            else
            {
                res[key] = new SolidColorBrush(color);
            }
        }

    }
}
