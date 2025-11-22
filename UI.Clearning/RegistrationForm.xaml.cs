using Cleaning.Data.JsonStorage;
using Domain.Cleaning;
using System.Text;
using System.Windows;

namespace UI.Cleaning
{
    public partial class RegistrationForm : Window
    {
        private readonly UsersRepository _usersRepository;

        public RegistrationForm()
        {
            InitializeComponent();
            _usersRepository = new UsersRepository();
        }

        private void CreateAccountButton_Click(object sender, RoutedEventArgs e)
        {
            string lastName = LastNameTextBox.Text;
            string firstName = FirstNameTextBox.Text;
            string middleName = MiddleNameTextBox.Text;
            string login = LoginTextBox.Text;
            string password = PasswordBox.Password;
            string confirmPassword = ConfirmPasswordBox.Password;

            StringBuilder errors = new StringBuilder();

            if (string.IsNullOrWhiteSpace(lastName))
                errors.AppendLine("Фамилия обязательна для заполнения");
            if (string.IsNullOrWhiteSpace(firstName))
                errors.AppendLine("Имя обязательно для заполнения");
            if (string.IsNullOrWhiteSpace(login))
                errors.AppendLine("Логин обязателен для заполнения");
            if (string.IsNullOrWhiteSpace(password))
                errors.AppendLine("Пароль обязателен для заполнения");
            if (password != confirmPassword)
                errors.AppendLine("Пароли не совпадают");
            if (password.Length < 6)
                errors.AppendLine("Пароль должен содержать минимум 6 символов");

            var existingUser = _usersRepository.GetByLogin(login);
            if (existingUser != null)
                errors.AppendLine("Пользователь с таким логином уже существует");

            if (errors.Length > 0)
            {
                MessageBox.Show(errors.ToString(), "Ошибка валидации",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var newUser = new User
            {
                LastName = lastName.Trim(),
                FirstName = firstName.Trim(),
                MiddleName = string.IsNullOrWhiteSpace(middleName) ? null : middleName.Trim(),
                Login = login.Trim(),
                Password = password,
                Role = "Client"
            };

            _usersRepository.Add(newUser);

            MessageBox.Show("Аккаунт успешно создан!", "Успех",
                MessageBoxButton.OK, MessageBoxImage.Information);

            this.Hide();
            var authForm = new AuthorizationForm();
            authForm.Show();
            this.Close();
        }
    }
}