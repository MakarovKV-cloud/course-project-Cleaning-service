using CleaningDB.Data.SqlServer;
using Microsoft.EntityFrameworkCore;
using System;
using System.Windows;
using UI.Clearning;

namespace UI.Cleaning
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            try
            {
                // Строка подключения для SQL Server
                string connectionString = "Server=localhost;Database=CleaningDB;Trusted_Connection=True;TrustServerCertificate=true;";

                var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
                var options = optionsBuilder
                    .UseSqlServer(connectionString,
                        sqlServerOptions => sqlServerOptions.EnableRetryOnFailure())
                    .Options;

                using (var context = new AppDbContext(options))
                {
                    // Создаем БД если ее нет
                    context.Database.EnsureCreated();

                    Console.WriteLine("База данных SQL Server успешно настроена!");

                    // Проверяем соединение
                    if (context.Database.CanConnect())
                    {
                        MessageBox.Show("Подключение к SQL Server успешно!",
                            "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при настройке SQL Server:\n{ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }

        }
    }
}