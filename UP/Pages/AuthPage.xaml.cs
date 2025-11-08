using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

// GDI+ для рисования капчи
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;

namespace UP.Pages
{
    public partial class AuthPage : Page
    {
        private Users currentUser;
        private readonly Random _rnd = new Random();
        private string _captchaCode = string.Empty;

        public AuthPage()
        {
            InitializeComponent();
            GenerateCaptcha();
        }

        // ===== ХЭШ ПАРОЛЯ (SHA1) =====
        public static string GetHash(string password)
        {
            using (var hash = SHA1.Create())
            {
                return string.Concat(hash.ComputeHash(Encoding.UTF8.GetBytes(password))
                    .Select(x => x.ToString("X2")));
            }
        }

        // ===== ОБРАБОТЧИКИ КНОПОК И ПОЛЕЙ =====
        private void ButtonAuth_Click(object sender, RoutedEventArgs e)
        {
            // 1) Проверка капчи (обязательна)
            var captchaInput = (TextBoxCaptcha.Text ?? "").Trim();
            if (!string.Equals(captchaInput, _captchaCode, StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show("Неверно введен код с картинки.", "Проверка капчи",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                GenerateCaptcha();
                TextBoxCaptcha.Clear();
                return;
            }

            // 2) Проверка логина/пароля
            if (string.IsNullOrWhiteSpace(TextBoxLogin.Text) ||
                string.IsNullOrWhiteSpace(PasswordBox.Password))
            {
                MessageBox.Show("Введите логин и пароль.", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string hashedPassword = GetHash(PasswordBox.Password);

            using (var db = new Entities()) // замени на Entities.GetContext(), если используешь синглтон
            {
                var user = db.Users.AsNoTracking()
                    .FirstOrDefault(u => u.Login == TextBoxLogin.Text &&
                                         u.Password == hashedPassword);

                if (user == null)
                {
                    MessageBox.Show("Пользователь с такими данными не найден.", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);

                    // при ошибке — обновим капчу
                    GenerateCaptcha();
                    TextBoxCaptcha.Clear();
                    return;
                }

                MessageBox.Show($"Добро пожаловать, {user.FIO}!", "Успех",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                currentUser = user;
                NavigateByRole(user.Role);
            }
        }

        private void ButtonReg_Click(object sender, RoutedEventArgs e)
        {
            NavigationService?.Navigate(new RegPage());
        }

        private void ButtonChangePassword_Click(object sender, RoutedEventArgs e)
        {
            NavigationService?.Navigate(new ChangePassPage());
        }

        private void ButtonRefreshCaptcha_Click(object sender, RoutedEventArgs e)
        {
            GenerateCaptcha();
            TextBoxCaptcha.Clear();
        }

        private void TextBoxLogin_TextChanged(object sender, TextChangedEventArgs e) { }

        // ===== КАПЧА: генерация кода и картинки =====
        private void GenerateCaptcha()
        {
            _captchaCode = MakeCode(5);    // длина кода
            var bmp = DrawCaptcha(_captchaCode);

            var ms = new MemoryStream();
            bmp.Save(ms, ImageFormat.Png);
            ms.Position = 0;

            var img = new BitmapImage();
            img.BeginInit();
            img.StreamSource = ms;
            img.CacheOption = BitmapCacheOption.OnLoad;
            img.EndInit();

            CaptchaImage.Source = img;
        }

        private string MakeCode(int len)
        {
            const string alphabet =
                "ABCDEFGHJKLMNPQRSTUVWXYZ" +   // без I/O
                "abcdefghijkmnpqrstuvwxyz" +   // без l/o
                "23456789";

            var buf = new char[len];
            for (int i = 0; i < len; i++)
                buf[i] = alphabet[_rnd.Next(alphabet.Length)];

            return new string(buf);
        }

        private Bitmap DrawCaptcha(string text)
        {
            int width = 90, height = 34;
            var bmp = new Bitmap(width, height);
            using (var g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(System.Drawing.Color.White);

                // шумовые линии
                for (int i = 0; i < 8; i++)
                {
                    var p1 = new System.Drawing.Point(_rnd.Next(width), _rnd.Next(height));
                    var p2 = new System.Drawing.Point(_rnd.Next(width), _rnd.Next(height));
                    using (var pen = new Pen(System.Drawing.Color.FromArgb(
                               _rnd.Next(80, 160), _rnd.Next(80, 160), _rnd.Next(80, 160)), 1))
                    {
                        g.DrawLine(pen, p1, p2);
                    }
                }

                // текст
                using (var font = new System.Drawing.Font("Arial", 16, System.Drawing.FontStyle.Bold))
                using (var brush = new SolidBrush(System.Drawing.Color.FromArgb(
                               _rnd.Next(20, 120), _rnd.Next(20, 120), _rnd.Next(20, 120))))
                {
                    int x = 6 + _rnd.Next(0, 8);
                    int y = 4 + _rnd.Next(-2, 3);
                    g.DrawString(text, font, brush, x, y);
                }
            }
            return bmp;
        }

        // ===== Навигация по роли =====
        private void NavigateByRole(string role)
        {
            switch (role)
            {
                case "User":
                    NavigationService?.Navigate(new UserPage(currentUser));
                    break;
                case "Admin":
                    NavigationService?.Navigate(new AdminPage());
                    break;
                default:
                    MessageBox.Show("Неизвестная роль пользователя", "Ошибка");
                    break;
            }
        }
    }
}
