using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using UP.Pages;

namespace UP
{
    public class DatabaseInitializer
    {
        public static string GetHash(string password)
        {
            using (var hash = SHA1.Create())
            {
                return string.Concat(hash.ComputeHash(Encoding.UTF8.GetBytes(password))
                    .Select(x => x.ToString("X2")));
            }
        }

        public static void InitializeDatabase()
        {
            using (var db = new Entities())
            {
                if (!db.Database.Exists())
                {
                    db.Database.Create();

                    AddTestData(db);
                }
                else if (!db.Users.Any())
                {
                    AddTestData(db);
                }
            }
        }

        private static void AddTestData(Entities db)
        {
            var admin = new Users
            {
                Login = "admin",
                Password = GetHash("admin123"),
                Role = "Admin",
                FIO = "Администратор Системы"
            };
            db.Users.Add(admin);

            var user = new Users
            {
                Login = "user",
                Password = GetHash("user123"),
                Role = "User",
                FIO = "Иванов Иван Иванович"
            };
            db.Users.Add(user);

            var categories = new[]
            {
                new Category { Name = "Продукты питания" },
                new Category { Name = "Коммунальные услуги" },
                new Category { Name = "Транспорт" },
                new Category { Name = "Развлечения" },
                new Category { Name = "Одежда" },
                new Category { Name = "Здоровье" }
            };

            foreach (var category in categories)
            {
                db.Category.Add(category);
            }

            db.SaveChanges();

            var testPayments = new[]
            {
                new Payment
                {
                    UserID = user.ID,
                    CategoryID = categories[0].ID,
                    Date = DateTime.Now.AddDays(-10),
                    Name = "Продукты в магазине",
                    Num = 1,
                    Price = 2500.50m
                },
                new Payment
                {
                    UserID = user.ID,
                    CategoryID = categories[1].ID,
                    Date = DateTime.Now.AddDays(-5),
                    Name = "Коммунальные услуги",
                    Num = 1,
                    Price = 4500.00m
                },
                new Payment
                {
                    UserID = user.ID,
                    CategoryID = categories[2].ID,
                    Date = DateTime.Now.AddDays(-2),
                    Name = "Проездной на месяц",
                    Num = 1,
                    Price = 2500.00m
                }
            };

            foreach (var payment in testPayments)
            {
                db.Payment.Add(payment);
            }

            db.SaveChanges();
        }
    }
}