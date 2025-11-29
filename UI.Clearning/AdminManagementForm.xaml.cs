using Cleaning.Data.Interfaces;
using Cleaning.Data.JsonStorage;
using Cleaning.Services;
using Clearning.Services;
using Domain.Cleaning;
using Domain.Statistics;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Legends;
using OxyPlot.Series;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;

namespace UI.Cleaning
{
    public partial class AdminManagementForm : Window, INotifyPropertyChanged
    {
        private readonly UsersRepository _usersRepository;
        private readonly RequestsRepository _requestsRepository;
        private readonly CitiesRepository _citiesRepository;
        private readonly ServicesRepository _servicesRepository;
        private readonly RequestServicesRepository _requestServicesRepository;
        private readonly PaymentsRepository _paymentsRepository;
        private readonly AdminManagementService _adminManagementService;
        private StatisticsService _statisticsService;

        private User? _currentUser;
        private User? _selectedUser;
        private Request? _selectedRequest;
        private City? _selectedCity;

        private bool _isUpdatingUsers = false;

        private PlotModel? _monthPlotModel;
        private PlotModel? _cleanersPlotModel;

        public PlotModel? MonthPlotModel
        {
            get => _monthPlotModel;
            set
            {
                _monthPlotModel = value;
                OnPropertyChanged();
            }
        }

        public PlotModel? CleanersPlotModel
        {
            get => _cleanersPlotModel;
            set
            {
                _cleanersPlotModel = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private class UserDisplayData
        {
            public int Id { get; set; }
            public string FullName { get; set; } = string.Empty;
            public string Login { get; set; } = string.Empty;
            public string Role { get; set; } = string.Empty;
            public DateTime CreatedAt { get; set; }
        }

        private class RequestDisplayData
        {
            public int Id { get; set; }
            public string ClientName { get; set; } = string.Empty;
            public string FullAddress { get; set; } = string.Empty;
            public DateTime CleaningDate { get; set; }
            public string Status { get; set; } = string.Empty;
            public string CleanerName { get; set; } = string.Empty;
            public decimal TotalCost { get; set; }
        }

        private class CleanerDisplayData
        {
            public int Id { get; set; }
            public string FullName { get; set; } = string.Empty;
        }

        public AdminManagementForm()
        {
            try
            {
                // Устанавливаем DataContext перед инициализацией компонентов
                this.DataContext = this;

                InitializeComponent();

                // Инициализируем репозитории с безопасной инициализацией
                _usersRepository = InitializeRepository<UsersRepository>("пользователей");
                _requestsRepository = InitializeRepository<RequestsRepository>("заявок");
                _citiesRepository = InitializeRepository<CitiesRepository>("городов");
                _servicesRepository = InitializeRepository<ServicesRepository>("услуг");
                _requestServicesRepository = InitializeRepository<RequestServicesRepository>("услуг заявок");
                _paymentsRepository = InitializeRepository<PaymentsRepository>("платежей");

                // Проверяем, что все необходимые репозитории инициализированы
                if (_requestsRepository == null || _usersRepository == null || _citiesRepository == null || _paymentsRepository == null)
                {
                    MessageBox.Show("Не удалось инициализировать необходимые репозитории. Статистика будет недоступна.",
                        "Ошибка инициализации", MessageBoxButton.OK, MessageBoxImage.Error);
                }

                // Инициализация сервиса статистики
                InitializeStatisticsService();

                if (Application.Current.Properties.Contains("CurrentUser"))
                {
                    _currentUser = Application.Current.Properties["CurrentUser"] as User;
                }

                if (_currentUser == null)
                {
                    MessageBox.Show("Ошибка авторизации. Пожалуйста, войдите снова.", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    GoToAuthorization();
                    return;
                }

                // Инициализация AdminManagementService только если репозиторий пользователей доступен
                if (_usersRepository != null)
                {
                    _adminManagementService = new AdminManagementService(_currentUser, _usersRepository);
                }
                else
                {
                    MessageBox.Show("Репозиторий пользователей недоступен. Некоторые функции будут ограничены.",
                        "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                }

                // Загружаем данные только если репозитории доступны
                if (_usersRepository != null) LoadUsers();
                if (_requestsRepository != null) LoadRequests();
                if (_citiesRepository != null) LoadCities();
                if (_usersRepository != null) LoadCleaners();

                // Инициализация года по умолчанию
                if (YearComboBox != null && YearComboBox.Items.Count > 1)
                {
                    YearComboBox.SelectedIndex = 1; // 2025 год
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка инициализации: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private T InitializeRepository<T>(string repositoryName) where T : class, new()
        {
            try
            {
                var repository = new T();
                System.Diagnostics.Debug.WriteLine($"Репозиторий {repositoryName} успешно инициализирован");
                return repository;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка инициализации репозитория {repositoryName}: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }
        }

        private void InitializeStatisticsService()
        {
            try
            {
                // Проверяем, что все необходимые репозитории инициализированы
                if (_requestsRepository == null)
                {
                    throw new ArgumentNullException(nameof(_requestsRepository), "Репозиторий заявок не инициализирован");
                }
                if (_paymentsRepository == null)
                {
                    throw new ArgumentNullException(nameof(_paymentsRepository), "Репозиторий платежей не инициализирован");
                }
                if (_usersRepository == null)
                {
                    throw new ArgumentNullException(nameof(_usersRepository), "Репозиторий пользователей не инициализирован");
                }
                if (_citiesRepository == null)
                {
                    throw new ArgumentNullException(nameof(_citiesRepository), "Репозиторий городов не инициализирован");
                }

                _statisticsService = new StatisticsService(
                    _requestsRepository,
                    _paymentsRepository,
                    _usersRepository,
                    _citiesRepository);

                System.Diagnostics.Debug.WriteLine("Сервис статистики успешно инициализирован");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Не удалось инициализировать сервис статистики: {ex.Message}\nСтатистика будет недоступна.",
                    "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                _statisticsService = null;
            }
        }

        #region Пользователи Tab
        private void LoadUsers()
        {
            try
            {
                _isUpdatingUsers = true;

                if (_usersRepository == null)
                {
                    MessageBox.Show("Репозиторий пользователей недоступен", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

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
                    FullName = $"{u.LastName} {u.FirstName} {u.MiddleName ?? ""}".Trim(),
                    Login = u.Login,
                    Role = u.Role,
                    CreatedAt = u.CreatedAt
                }).ToList();

                if (UsersDataGrid != null)
                {
                    UsersDataGrid.ItemsSource = displayData;
                }
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

        private void UsersDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isUpdatingUsers) return;

            try
            {
                if (UsersDataGrid?.SelectedItem is UserDisplayData selectedItem)
                {
                    int userId = selectedItem.Id;
                    _selectedUser = _usersRepository?.GetById(userId);

                    if (_selectedUser != null && RoleComboBox != null && ApplyRoleButton != null && DeleteUserButton != null)
                    {
                        RoleComboBox.IsEnabled = true;
                        ApplyRoleButton.IsEnabled = true;
                        DeleteUserButton.IsEnabled = true;
                        RoleComboBox.SelectedItem = _selectedUser.Role;
                    }
                    else
                    {
                        ClearUserSelection();
                        if (_selectedUser == null)
                        {
                            MessageBox.Show("Пользователь не найден в базе данных", "Ошибка",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                        }
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
            if (RoleComboBox != null)
            {
                RoleComboBox.IsEnabled = false;
                RoleComboBox.SelectedIndex = -1;
            }
            if (ApplyRoleButton != null) ApplyRoleButton.IsEnabled = false;
            if (DeleteUserButton != null) DeleteUserButton.IsEnabled = false;
        }

        private void ApplyRoleButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedUser == null)
            {
                MessageBox.Show("Пользователь не выбран", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (RoleComboBox?.SelectedItem == null)
            {
                MessageBox.Show("Роль не выбрана", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                string newRole = RoleComboBox.SelectedItem.ToString() ?? string.Empty;

                if (_adminManagementService != null)
                {
                    _adminManagementService.ApplyRole(_selectedUser, newRole);
                }
                else
                {
                    MessageBox.Show("Сервис управления недоступен", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                LoadUsers();
                RestoreUserSelection(_selectedUser.Id);

                MessageBox.Show($"Роль пользователя {_selectedUser.Login} изменена на '{newRole}'",
                    "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex) when (ex is LogicException || ex is InvalidOperationException)
            {
                MessageBox.Show(ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                LoadUsers();
                ClearUserSelection();
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
            if (UsersDataGrid?.ItemsSource is IEnumerable<UserDisplayData> users)
            {
                var userToSelect = users.FirstOrDefault(u => u.Id == userId);
                if (userToSelect != null && UsersDataGrid != null)
                {
                    UsersDataGrid.SelectedItem = userToSelect;
                    UsersDataGrid.ScrollIntoView(userToSelect);
                }
            }
        }

        private void DeleteUserButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedUser == null)
            {
                MessageBox.Show("Пользователь не выбран", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                if (_currentUser != null && _selectedUser.Id == _currentUser.Id)
                {
                    MessageBox.Show("Нельзя удалить свой собственный аккаунт", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (_selectedUser.Role == "Admin")
                {
                    var adminUsers = _usersRepository?.GetAll()?.Where(u => u.Role == "Admin").ToList() ?? new List<User>();
                    if (adminUsers.Count <= 1)
                    {
                        MessageBox.Show("Нельзя удалить последнего администратора", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                }

                var result = MessageBox.Show(
                    $"Вы уверены, что хотите удалить пользователя {_selectedUser.Login}?",
                    "Подтверждение удаления",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    if (_usersRepository != null)
                    {
                        _usersRepository.Delete(_selectedUser.Id);
                        LoadUsers();
                        ClearUserSelection();
                        MessageBox.Show("Пользователь удален", "Успех",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show("Репозиторий пользователей недоступен", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
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
                if (_requestsRepository == null)
                {
                    MessageBox.Show("Репозиторий заявок недоступен", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var requests = _requestsRepository.GetAll() ?? new List<Request>();
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

                if (RequestsDataGrid != null)
                {
                    RequestsDataGrid.ItemsSource = displayData;
                }
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
                if (_usersRepository == null)
                {
                    MessageBox.Show("Репозиторий пользователей недоступен", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var cleaners = _usersRepository.GetAll()?.Where(u => u.Role == "Cleaner").ToList() ?? new List<User>();
                var displayData = cleaners.Select(c => new CleanerDisplayData
                {
                    Id = c.Id,
                    FullName = $"{c.LastName} {c.FirstName} {c.MiddleName ?? ""}".Trim()
                }).ToList();

                if (CleanerComboBox != null)
                {
                    CleanerComboBox.ItemsSource = displayData;
                }
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
                if (_usersRepository == null) return "Неизвестно";

                var user = _usersRepository.GetById(userId);
                return user != null ? $"{user.LastName} {user.FirstName}".Trim() : "Неизвестно";
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
                if (_citiesRepository == null) return $"{request.District}, {request.Address}";

                var city = _citiesRepository.GetById(request.CityId);
                return city != null ? $"{city.Name}, {request.District}, {request.Address}"
                                   : $"{request.District}, {request.Address}";
            }
            catch
            {
                return $"{request.District}, {request.Address}";
            }
        }

        private void RequestsDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (RequestsDataGrid?.SelectedItem is RequestDisplayData selectedItem)
            {
                int requestId = selectedItem.Id;
                _selectedRequest = _requestsRepository?.GetById(requestId);

                if (RequestStatusComboBox != null && CleanerComboBox != null && UpdateRequestButton != null)
                {
                    RequestStatusComboBox.IsEnabled = true;
                    CleanerComboBox.IsEnabled = true;
                    UpdateRequestButton.IsEnabled = true;

                    if (_selectedRequest != null)
                    {
                        RequestStatusComboBox.SelectedItem = _selectedRequest.Status;

                        if (_selectedRequest.CleanerId.HasValue && CleanerComboBox.ItemsSource != null)
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
                }
            }
            else
            {
                _selectedRequest = null;
                if (RequestStatusComboBox != null) RequestStatusComboBox.IsEnabled = false;
                if (CleanerComboBox != null) CleanerComboBox.IsEnabled = false;
                if (UpdateRequestButton != null) UpdateRequestButton.IsEnabled = false;
            }
        }

        private void UpdateRequestButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedRequest == null)
            {
                MessageBox.Show("Заявка не выбрана", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                if (RequestStatusComboBox?.SelectedItem is string selectedStatus)
                {
                    _selectedRequest.Status = selectedStatus;
                }

                if (CleanerComboBox?.SelectedItem is CleanerDisplayData selectedCleaner)
                {
                    _selectedRequest.CleanerId = selectedCleaner.Id;
                }
                else
                {
                    _selectedRequest.CleanerId = null;
                }

                if (_requestsRepository != null)
                {
                    _requestsRepository.Update(_selectedRequest);
                    LoadRequests();
                    MessageBox.Show("Заявка обновлена", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("Репозиторий заявок недоступен", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при обновлении заявки: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        #endregion

        #region Города Tab
        private void LoadCities()
        {
            try
            {
                if (_citiesRepository == null)
                {
                    MessageBox.Show("Репозиторий городов недоступен", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var cities = _citiesRepository.GetAll() ?? new List<City>();
                if (CitiesDataGrid != null)
                {
                    CitiesDataGrid.ItemsSource = cities;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки городов: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CitiesDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CitiesDataGrid?.SelectedItem is City selectedCity)
            {
                _selectedCity = selectedCity;
                if (DeleteCityButton != null)
                {
                    DeleteCityButton.IsEnabled = true;
                }
            }
            else
            {
                _selectedCity = null;
                if (DeleteCityButton != null)
                {
                    DeleteCityButton.IsEnabled = false;
                }
            }
        }

        private void AddCityButton_Click(object sender, RoutedEventArgs e)
        {
            string cityName = NewCityTextBox?.Text?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(cityName))
            {
                MessageBox.Show("Введите название города", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                if (_citiesRepository == null)
                {
                    MessageBox.Show("Репозиторий городов недоступен", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var existingCity = _citiesRepository.GetAll()
                    ?.FirstOrDefault(c => c.Name.ToLower() == cityName.ToLower());

                if (existingCity != null)
                {
                    MessageBox.Show("Город с таким названием уже существует", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var newCity = new City { Name = cityName };
                _citiesRepository.Add(newCity);

                if (NewCityTextBox != null)
                {
                    NewCityTextBox.Clear();
                }
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
            if (_selectedCity == null)
            {
                MessageBox.Show("Город не выбран", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                if (_citiesRepository == null)
                {
                    MessageBox.Show("Репозиторий городов недоступен", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var requestsUsingCity = _requestsRepository?.GetAll()
                    ?.Where(r => r.CityId == _selectedCity.Id).ToList() ?? new List<Request>();

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
        #endregion

        #region Статистика Tab
        private void StatisticsTab_Loaded(object sender, RoutedEventArgs e)
        {
            LoadStatistics();
        }

        private void YearComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            LoadStatistics();
        }

        private void RefreshStatisticsButton_Click(object sender, RoutedEventArgs e)
        {
            LoadStatistics();
        }

        private void LoadStatistics()
        {
            try
            {
                // Проверяем, что сервис статистики инициализирован
                if (_statisticsService == null)
                {
                    ShowEmptyStatistics();
                    return;
                }

                if (YearComboBox?.SelectedItem is ComboBoxItem selectedYearItem)
                {
                    string yearText = selectedYearItem.Content?.ToString() ?? "2025";
                    if (int.TryParse(yearText, out int year))
                    {
                        // Загрузка диаграмм
                        LoadMonthChart(year);
                        LoadCleanersChart(year);

                        // Загрузка общей статистики
                        LoadOverallStatistics(year);
                    }
                    else
                    {
                        ShowEmptyStatistics();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки статистики: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                ShowEmptyStatistics();
            }
        }

        private void ShowEmptyStatistics()
        {
            MonthPlotModel = CreateEmptyPlotModel("Нет данных для отображения");
            CleanersPlotModel = CreateEmptyPlotModel("Нет данных для отображения");

            if (TotalRequestsText != null) TotalRequestsText.Text = "Всего заявок: 0";
            if (CompletedRequestsText != null) CompletedRequestsText.Text = "Завершенных заявок: 0";
            if (TotalEarningsText != null) TotalEarningsText.Text = "Общий заработок: 0 ₽";
            if (AverageOrderText != null) AverageOrderText.Text = "Средний чек: 0 ₽";
        }

        private void LoadMonthChart(int year)
        {
            try
            {
                // Проверяем, что сервис статистики инициализирован
                if (_statisticsService == null)
                {
                    MonthPlotModel = CreateEmptyPlotModel("Сервис статистики не доступен");
                    return;
                }

                // Получаем данные из сервиса
                var ordersData = _statisticsService.GetCompletedOrdersByMonth(year) ?? new List<MonthStatisticItem>();
                var earningsData = _statisticsService.GetEarningsByMonth(year) ?? new List<EarningsStatisticItem>();

                // Если нет данных, показываем сообщение
                if (!ordersData.Any() && !earningsData.Any())
                {
                    MonthPlotModel = CreateEmptyPlotModel("Нет данных за выбранный период");
                    return;
                }

                // Создаём модель диаграммы
                var plotModel = new PlotModel
                {
                    Title = $"Заработок по месяцам за {year} год",
                    Background = OxyColors.White,
                    PlotAreaBorderColor = OxyColors.LightGray,
                    PlotAreaBorderThickness = new OxyThickness(1)
                };

                // Создаём ось категорий (месяцы)
                var categoryAxis = new CategoryAxis
                {
                    Position = AxisPosition.Bottom,
                    Title = "Месяцы",
                    Angle = -45,
                    MajorStep = 1,
                    MinorStep = 1
                };

                // Заполняем метки оси всеми месяцами года
                var monthNames = new[] { "Янв", "Фев", "Мар", "Апр", "Май", "Июн", "Июл", "Авг", "Сен", "Окт", "Ноя", "Дек" };
                foreach (var monthName in monthNames)
                {
                    categoryAxis.Labels.Add(monthName);
                }

                plotModel.Axes.Add(categoryAxis);

                // Создаём числовую ось для количества заказов
                var ordersAxis = new LinearAxis
                {
                    Position = AxisPosition.Left,
                    Title = "Количество заказов",
                    Minimum = 0,
                    MinimumPadding = 0.1,
                    MaximumPadding = 0.1
                };
                plotModel.Axes.Add(ordersAxis);

                // Создаём числовую ось для заработка
                var earningsAxis = new LinearAxis
                {
                    Position = AxisPosition.Right,
                    Title = "Заработок (руб.)",
                    Minimum = 0,
                    MinimumPadding = 0.1,
                    MaximumPadding = 0.1
                };
                plotModel.Axes.Add(earningsAxis);

                // Создаем серию для ЗАРАБОТКА (основная метка)
                var earningsSeries = new LineSeries
                {
                    Title = "Заработок",
                    Color = OxyColor.FromRgb(79, 129, 189),
                    MarkerType = MarkerType.Circle,
                    MarkerSize = 6,
                    MarkerStroke = OxyColor.FromRgb(79, 129, 189),
                    MarkerFill = OxyColors.White,
                    MarkerStrokeThickness = 2,
                    StrokeThickness = 3
                };

                // Создаем серию для ЗАВЕРШЕННЫХ ЗАКАЗОВ (вторичная метка)
                var ordersSeries = new LineSeries
                {
                    Title = "Завершенные заказы",
                    Color = OxyColor.FromRgb(192, 80, 77),
                    MarkerType = MarkerType.Square,
                    MarkerSize = 6,
                    MarkerStroke = OxyColor.FromRgb(192, 80, 77),
                    MarkerFill = OxyColors.White,
                    MarkerStrokeThickness = 2,
                    StrokeThickness = 3
                };

                // Заполняем данные для ЗАРАБОТКА
                for (int month = 1; month <= 12; month++)
                {
                    var monthData = earningsData.FirstOrDefault(x => x.Month == month && x.Year == year);
                    var earnings = monthData?.TotalEarnings ?? 0;
                    earningsSeries.Points.Add(new DataPoint(month - 1, (double)earnings));
                }

                // Заполняем данные для ЗАВЕРШЕННЫХ ЗАКАЗОВ
                for (int month = 1; month <= 12; month++)
                {
                    var monthData = ordersData.FirstOrDefault(x => x.Month == month && x.Year == year);
                    var orderCount = monthData?.Count ?? 0;
                    ordersSeries.Points.Add(new DataPoint(month - 1, orderCount));
                }

                // Добавляем серии в модель
                plotModel.Series.Add(earningsSeries);
                plotModel.Series.Add(ordersSeries);

                //  Добавляем легенду
                var legend = new Legend
                {
                    LegendPosition = LegendPosition.TopLeft,
                    LegendOrientation = LegendOrientation.Horizontal,
                    LegendPlacement = LegendPlacement.Outside,
                    LegendBorder = OxyColors.LightGray,
                    LegendBackground = OxyColor.FromArgb(200, 255, 255, 255)
                };
                plotModel.Legends.Add(legend);

                MonthPlotModel = plotModel;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки диаграммы по месяцам: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                MonthPlotModel = CreateEmptyPlotModel("Ошибка загрузки данных");
            }
        }

        private void LoadCleanersChart(int year)
        {
            try
            {
                // Проверяем, что сервис статистики инициализирован
                if (_statisticsService == null)
                {
                    CleanersPlotModel = CreateEmptyPlotModel("Сервис статистики не доступен");
                    return;
                }

                // Данные из сервиса
                var cleanersData = _statisticsService.GetCleanersCompletedOrdersStatistics(year) ?? new List<CleanerStatisticItem>();

                // Если нет данных
                if (!cleanersData.Any())
                {
                    CleanersPlotModel = CreateEmptyPlotModel("Нет данных о клинерах");
                    return;
                }

                // Модель диаграммы
                var plotModel = new PlotModel
                {
                    Title = $"Топ клинеров по выполненным заказам за {year} год",
                    Background = OxyColors.White,
                    PlotAreaBorderColor = OxyColors.LightGray,
                    PlotAreaBorderThickness = new OxyThickness(1)
                };

                var cleanersSeries = new LineSeries
                {
                    Title = "Выполненные заказы",
                    Color = OxyColor.FromRgb(155, 187, 89),
                    MarkerType = MarkerType.Diamond,
                    MarkerSize = 6,
                    MarkerStroke = OxyColor.FromRgb(155, 187, 89),
                    MarkerFill = OxyColors.White,
                    MarkerStrokeThickness = 2,
                    StrokeThickness = 3
                };

                // Ось Категорий (имена клинеров)
                var categoryAxis = new CategoryAxis
                {
                    Position = AxisPosition.Bottom,
                    Title = "Клинеры",
                    Angle = -45,
                    MajorStep = 1,
                    MinorStep = 1
                };

                // Числовая ось (количество заказов)
                var valueAxis = new LinearAxis
                {
                    Position = AxisPosition.Left,
                    Title = "Количество заказов",
                    Minimum = 0,
                    MinimumPadding = 0.1,
                    MaximumPadding = 0.1
                };

                // Добавление данных (топ 10 клинеров)
                var topCleaners = cleanersData.Take(10).ToList();
                for (int i = 0; i < topCleaners.Count; i++)
                {
                    var cleaner = topCleaners[i];
                    if (cleaner != null)
                    {
                        // Обрезаем длинные имена для лучшего отображения
                        var cleanerName = cleaner.CleanerName ?? "Неизвестный";
                        if (cleanerName.Length > 15)
                        {
                            cleanerName = cleanerName.Substring(0, 12) + "...";
                        }

                        cleanersSeries.Points.Add(new DataPoint(i, cleaner.Count));
                        categoryAxis.Labels.Add(cleanerName);
                    }
                }

                // Проверяем, есть ли данные для отображения
                if (cleanersSeries.Points.Any())
                {
                    plotModel.Axes.Add(categoryAxis);
                    plotModel.Axes.Add(valueAxis);
                    plotModel.Series.Add(cleanersSeries);

                    // Добавляем легенду
                    var legend = new Legend
                    {
                        LegendPosition = LegendPosition.TopRight,
                        LegendOrientation = LegendOrientation.Horizontal,
                        LegendPlacement = LegendPlacement.Outside
                    };
                    plotModel.Legends.Add(legend);
                }
                else
                {
                    plotModel = CreateEmptyPlotModel("Нет данных о клинерах");
                }

                CleanersPlotModel = plotModel;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки диаграммы клинеров: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                CleanersPlotModel = CreateEmptyPlotModel("Ошибка загрузки данных");
            }
        }

        private PlotModel CreateEmptyPlotModel(string message)
        {
            var plotModel = new PlotModel
            {
                Title = message,
                Background = OxyColors.White,
                TextColor = OxyColors.Gray,
                PlotAreaBorderColor = OxyColors.LightGray
            };

            // Добавляем пустые оси для корректного отображения
            plotModel.Axes.Add(new LinearAxis
            {
                Position = AxisPosition.Bottom,
                Minimum = 0,
                Maximum = 1,
                IsAxisVisible = false
            });
            plotModel.Axes.Add(new LinearAxis
            {
                Position = AxisPosition.Left,
                Minimum = 0,
                Maximum = 1,
                IsAxisVisible = false
            });

            return plotModel;
        }

        private void LoadOverallStatistics(int year)
        {
            try
            {
                if (_requestsRepository == null)
                {
                    if (TotalRequestsText != null) TotalRequestsText.Text = "Всего заявок: 0";
                    if (CompletedRequestsText != null) CompletedRequestsText.Text = "Завершенных заявок: 0";
                    if (TotalEarningsText != null) TotalEarningsText.Text = "Общий заработок: 0 ₽";
                    if (AverageOrderText != null) AverageOrderText.Text = "Средний чек: 0 ₽";
                    return;
                }

                var completedFilter = new RequestFilter
                {
                    StartDate = new DateOnly(year, 1, 1),
                    EndDate = new DateOnly(year, 12, 31),
                    Status = "Завершена"
                };

                var allFilter = new RequestFilter
                {
                    StartDate = new DateOnly(year, 1, 1),
                    EndDate = new DateOnly(year, 12, 31)
                };

                var completedRequests = _requestsRepository.GetAll(completedFilter) ?? new List<Request>();
                var allRequests = _requestsRepository.GetAll(allFilter) ?? new List<Request>();

                if (TotalRequestsText != null)
                    TotalRequestsText.Text = $"Всего заявок: {allRequests.Count}";
                if (CompletedRequestsText != null)
                    CompletedRequestsText.Text = $"Завершенных заявок: {completedRequests.Count}";

                if (TotalEarningsText != null)
                {
                    var totalEarnings = completedRequests.Sum(r => r.TotalCost);
                    TotalEarningsText.Text = $"Общий заработок: {totalEarnings:N0} ₽";
                }

                if (AverageOrderText != null)
                {
                    if (completedRequests.Any())
                    {
                        var average = completedRequests.Average(r => r.TotalCost);
                        AverageOrderText.Text = $"Средний чек: {average:N0} ₽";
                    }
                    else
                    {
                        AverageOrderText.Text = "Средний чек: 0 ₽";
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки общей статистики: {ex.Message}", "Ошибка",
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