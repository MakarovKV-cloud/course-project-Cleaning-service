using Cleaning.Data.JsonStorage;
using Domain.Cleaning;
using System;
using System.Windows;

namespace UI.Cleaning
{
    public partial class AuthorizationForm : Window
    {
        private readonly UsersRepository _usersRepository;

        public AuthorizationForm()
        {
            try
            {
                InitializeComponent();
                _usersRepository = new UsersRepository();

                // Очищаем предыдущую авторизацию при запуске формы
                if (Application.Current.Properties.Contains("CurrentUser"))
                {
                    Application.Current.Properties.Remove("CurrentUser");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка инициализации: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string login = LoginTextBox.Text.Trim();
                string password = PasswordBox.Password;

                if (string.IsNullOrWhiteSpace(login) || string.IsNullOrWhiteSpace(password))
                {
                    MessageBox.Show("Пожалуйста, заполните все поля", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var user = _usersRepository.GetByLogin(login);
                if (user != null && user.Password == password)
                {
                    // Сохраняем текущего пользователя
                    Application.Current.Properties["CurrentUser"] = user;

                    // Проверяем, что пользователь действительно сохранился
                    var savedUser = Application.Current.Properties["CurrentUser"] as User;
                    if (savedUser == null)
                    {
                        MessageBox.Show("Ошибка сохранения сессии", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    this.Hide();

                    Window nextWindow = null;

                    switch (user.Role)
                    {
                        case "Client":
                            nextWindow = new ClientViewForm();
                            break;
                        case "Cleaner":
                            nextWindow = new CleanerRequestViewForm();
                            break;
                        case "Admin":
                            nextWindow = new AdminManagementForm();
                            break;
                        default:
                            MessageBox.Show($"Неизвестная роль пользователя: {user.Role}", "Ошибка",
                                MessageBoxButton.OK, MessageBoxImage.Error);
                            this.Show();
                            return;
                    }

                    if (nextWindow != null)
                    {
                        nextWindow.Show();
                        this.Close();
                    }
                }
                else
                {
                    MessageBox.Show("Аккаунт не найден или данные неверны", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка авторизации: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CreateAccountButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                this.Hide();
                var registrationForm = new RegistrationForm();
                registrationForm.Show();
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка перехода к регистрации: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}