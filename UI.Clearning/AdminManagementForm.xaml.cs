using Cleaning.Data.JsonStorage;
using Domain.Cleaning;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace UI.Cleaning
{
    public partial class AdminManagementForm : Window
    {
        private readonly UsersRepository _usersRepository;
        private readonly RequestsRepository _requestsRepository;
        private readonly CitiesRepository _citiesRepository;
        private readonly ServicesRepository _servicesRepository;
        private readonly RequestServicesRepository _requestServicesRepository;

        private User _currentUser;
        private User _selectedUser;
        private Request _selectedRequest;
        private City _selectedCity;

        private bool _isUpdatingUsers = false;

        private class UserDisplayData
        {
            public int Id { get; set; }
            public string FullName { get; set; }
            public string Login { get; set; }
            public string Role { get; set; }
            public DateTime CreatedAt { get; set; }
        }

        private class RequestDisplayData
        {
            public int Id { get; set; }
            public string ClientName { get; set; }
            public string FullAddress { get; set; }
            public DateTime CleaningDate { get; set; }
            public string Status { get; set; }
            public string CleanerName { get; set; }
            public decimal TotalCost { get; set; }
        }

        private class CleanerDisplayData
        {
            public int Id { get; set; }
            public string FullName { get; set; }
        }

        public AdminManagementForm()
        {
            try
            {
                InitializeComponent();
                _usersRepository = new UsersRepository();
                _requestsRepository = new RequestsRepository();
                _citiesRepository = new CitiesRepository();
                _servicesRepository = new ServicesRepository();
                _requestServicesRepository = new RequestServicesRepository();

                if (Application.Current.Properties.Contains("CurrentUser"))
                {
                    _currentUser = (User)Application.Current.Properties["CurrentUser"];
                }
                else
                {
                    MessageBox.Show("Ошибка авторизации. Пожалуйста, войдите снова.", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    GoToAuthorization();
                    return;
                }

                LoadUsers();
                LoadRequests();
                LoadCities();
                LoadCleaners();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка инициализации: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #region Пользователи Tab
        private void LoadUsers()
        {
            try
            {
                _isUpdatingUsers = true;

                var users = _usersRepository.GetAll();
                if (users == null)
                {
                    MessageBox.Show("Не удалось загрузить список пользователей", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var displayData = users.Select(u => new UserDisplayData
                {
                    Id = u.Id,
                    FullName = $"{u.LastName} {u.FirstName} {u.MiddleName ?? ""}",
                    Login = u.Login,
                    Role = u.Role,
                    CreatedAt = u.CreatedAt
                }).ToList();

                UsersDataGrid.ItemsSource = displayData;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки пользователей: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                _isUpdatingUsers = false;
            }
        }

        private void UsersDataGrid_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (_isUpdatingUsers) return;

            try
            {
                if (UsersDataGrid.SelectedItem is UserDisplayData selectedItem)
                {
                    int userId = selectedItem.Id;
                    _selectedUser = _usersRepository.GetById(userId);

                    if (_selectedUser != null)
                    {
                        RoleComboBox.IsEnabled = true;
                        ApplyRoleButton.IsEnabled = true;
                        DeleteUserButton.IsEnabled = true;
                        RoleComboBox.SelectedItem = _selectedUser.Role;
                    }
                    else
                    {
                        ClearUserSelection();
                        MessageBox.Show("Пользователь не найден в базе данных", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
                else
                {
                    ClearUserSelection();
                }
            }
            catch (Exception ex)
            {
                ClearUserSelection();
                MessageBox.Show($"Ошибка при выборе пользователя: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ClearUserSelection()
        {
            _selectedUser = null;
            RoleComboBox.IsEnabled = false;
            ApplyRoleButton.IsEnabled = false;
            DeleteUserButton.IsEnabled = false;
            RoleComboBox.SelectedIndex = -1;
        }

        private void ApplyRoleButton_Click(object sender, RoutedEventArgs e)
        {
            var currentSelectedUser = _selectedUser;

            if (currentSelectedUser == null)
            {
                MessageBox.Show("Пользователь не выбран", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (RoleComboBox.SelectedItem == null)
            {
                MessageBox.Show("Роль не выбрана", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            string newRole = RoleComboBox.SelectedItem.ToString();

            try
            {
                if (currentSelectedUser.Role == newRole)
                {
                    MessageBox.Show("Роль пользователя уже установлена в выбранное значение", "Информация",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                if (currentSelectedUser.Role == "Admin" && newRole != "Admin")
                {
                    var adminUsers = _usersRepository.GetAll().Where(u => u.Role == "Admin").ToList();
                    if (adminUsers.Count <= 1)
                    {
                        MessageBox.Show("Нельзя изменить роль последнего администратора", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                }

                if (currentSelectedUser.Id == _currentUser.Id)
                {
                    MessageBox.Show("Нельзя изменить свою собственную роль", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                int selectedUserId = currentSelectedUser.Id;
                currentSelectedUser.Role = newRole;
                _usersRepository.Update(currentSelectedUser);

                LoadUsers();
                RestoreUserSelection(selectedUserId);

                MessageBox.Show($"Роль пользователя {currentSelectedUser.Login} изменена на '{newRole}'",
                    "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при изменении роли: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                LoadUsers();
                ClearUserSelection();
            }
        }

        private void RestoreUserSelection(int userId)
        {
            if (UsersDataGrid.ItemsSource is IEnumerable<UserDisplayData> users)
            {
                var userToSelect = users.FirstOrDefault(u => u.Id == userId);
                if (userToSelect != null)
                {
                    UsersDataGrid.SelectedItem = userToSelect;
                    UsersDataGrid.ScrollIntoView(userToSelect);
                }
            }
        }

        private void DeleteUserButton_Click(object sender, RoutedEventArgs e)
        {
            var currentSelectedUser = _selectedUser;

            if (currentSelectedUser == null)
            {
                MessageBox.Show("Пользователь не выбран", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                if (currentSelectedUser.Id == _currentUser.Id)
                {
                    MessageBox.Show("Нельзя удалить свой собственный аккаунт", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (currentSelectedUser.Role == "Admin")
                {
                    var adminUsers = _usersRepository.GetAll().Where(u => u.Role == "Admin").ToList();
                    if (adminUsers.Count <= 1)
                    {
                        MessageBox.Show("Нельзя удалить последнего администратора", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                }

                var result = MessageBox.Show(
                    $"Вы уверены, что хотите удалить пользователя {currentSelectedUser.Login}?",
                    "Подтверждение удаления",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    _usersRepository.Delete(currentSelectedUser.Id);
                    LoadUsers();
                    ClearUserSelection();

                    MessageBox.Show("Пользователь удален", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при удалении пользователя: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        #endregion

        #region Заявки Tab
        private void LoadRequests()
        {
            try
            {
                var requests = _requestsRepository.GetAll();
                var displayData = requests.Select(r => new RequestDisplayData
                {
                    Id = r.Id,
                    ClientName = GetUserName(r.UserId),
                    FullAddress = GetFullAddress(r),
                    CleaningDate = r.CleaningDate,
                    Status = r.Status ?? "Новая",
                    CleanerName = r.CleanerId.HasValue ? GetUserName(r.CleanerId.Value) : "Не назначен",
                    TotalCost = r.TotalCost
                }).ToList();

                RequestsDataGrid.ItemsSource = displayData;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки заявок: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadCleaners()
        {
            try
            {
                var cleaners = _usersRepository.GetAll()?.Where(u => u.Role == "Cleaner").ToList() ?? new List<User>();
                var displayData = cleaners.Select(c => new CleanerDisplayData
                {
                    Id = c.Id,
                    FullName = $"{c.LastName} {c.FirstName} {c.MiddleName ?? ""}"
                }).ToList();

                CleanerComboBox.ItemsSource = displayData;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки клинеров: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string GetUserName(int userId)
        {
            try
            {
                var user = _usersRepository.GetById(userId);
                return user != null ? $"{user.LastName} {user.FirstName}" : "Неизвестно";
            }
            catch
            {
                return "Неизвестно";
            }
        }

        private string GetFullAddress(Request request)
        {
            try
            {
                var city = _citiesRepository.GetById(request.CityId);
                return city != null ? $"{city.Name}, {request.District}, {request.Address}"
                                   : $"{request.District}, {request.Address}";
            }
            catch
            {
                return $"{request.District}, {request.Address}";
            }
        }

        private void RequestsDataGrid_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (RequestsDataGrid.SelectedItem is RequestDisplayData selectedItem)
            {
                int requestId = selectedItem.Id;
                _selectedRequest = _requestsRepository.GetById(requestId);

                RequestStatusComboBox.IsEnabled = true;
                CleanerComboBox.IsEnabled = true;
                UpdateRequestButton.IsEnabled = true;

                RequestStatusComboBox.SelectedItem = _selectedRequest.Status;

                if (_selectedRequest.CleanerId.HasValue)
                {
                    foreach (var cleaner in CleanerComboBox.Items.OfType<CleanerDisplayData>())
                    {
                        if (cleaner.Id == _selectedRequest.CleanerId.Value)
                        {
                            CleanerComboBox.SelectedItem = cleaner;
                            break;
                        }
                    }
                }
                else
                {
                    CleanerComboBox.SelectedIndex = -1;
                }
            }
            else
            {
                _selectedRequest = null;
                RequestStatusComboBox.IsEnabled = false;
                CleanerComboBox.IsEnabled = false;
                UpdateRequestButton.IsEnabled = false;
            }
        }

        private void UpdateRequestButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedRequest != null)
            {
                try
                {
                    if (RequestStatusComboBox.SelectedItem is string selectedStatus)
                    {
                        _selectedRequest.Status = selectedStatus;
                    }

                    if (CleanerComboBox.SelectedItem is CleanerDisplayData selectedCleaner)
                    {
                        _selectedRequest.CleanerId = selectedCleaner.Id;
                    }
                    else
                    {
                        _selectedRequest.CleanerId = null;
                    }

                    _requestsRepository.Update(_selectedRequest);
                    LoadRequests();

                    MessageBox.Show("Заявка обновлена", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при обновлении заявки: {ex.Message}", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show("Заявка не выбрана", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        #endregion

        #region Города Tab
        private void LoadCities()
        {
            try
            {
                var cities = _citiesRepository.GetAll();
                CitiesDataGrid.ItemsSource = cities;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки городов: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CitiesDataGrid_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (CitiesDataGrid.SelectedItem is City selectedCity)
            {
                _selectedCity = selectedCity;
                DeleteCityButton.IsEnabled = true;
            }
            else
            {
                _selectedCity = null;
                DeleteCityButton.IsEnabled = false;
            }
        }

        private void AddCityButton_Click(object sender, RoutedEventArgs e)
        {
            string cityName = NewCityTextBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(cityName))
            {
                MessageBox.Show("Введите название города", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                var existingCity = _citiesRepository.GetAll()
                    .FirstOrDefault(c => c.Name.ToLower() == cityName.ToLower());

                if (existingCity != null)
                {
                    MessageBox.Show("Город с таким названием уже существует", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var newCity = new City { Name = cityName };
                _citiesRepository.Add(newCity);

                NewCityTextBox.Clear();
                LoadCities();

                MessageBox.Show($"Город '{cityName}' добавлен", "Успех",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при добавлении города: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DeleteCityButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedCity != null)
            {
                try
                {
                    var requestsUsingCity = _requestsRepository.GetAll()
                        .Where(r => r.CityId == _selectedCity.Id).ToList();

                    if (requestsUsingCity.Count > 0)
                    {
                        MessageBox.Show(
                            $"Невозможно удалить город. Он используется в {requestsUsingCity.Count} заявках.",
                            "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    var result = MessageBox.Show(
                        $"Вы уверены, что хотите удалить город '{_selectedCity.Name}'?",
                        "Подтверждение удаления",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

                    if (result == MessageBoxResult.Yes)
                    {
                        _citiesRepository.Delete(_selectedCity.Id);
                        LoadCities();
                        MessageBox.Show("Город удален", "Успех",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при удалении города: {ex.Message}", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show("Город не выбран", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        #endregion

        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Properties.Remove("CurrentUser");
            GoToAuthorization();
        }

        private void GoToAuthorization()
        {
            this.Hide();
            var authForm = new AuthorizationForm();
            authForm.Show();
            this.Close();
        }
    }
}