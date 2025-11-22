using Cleaning.Data.JsonStorage;
using Domain.Cleaning;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace UI.Cleaning
{
    public partial class CleanerRequestViewForm : Window
    {
        private RequestsRepository _requestsRepository;
        private RequestServicesRepository _requestServicesRepository;
        private ServicesRepository _servicesRepository;
        private CitiesRepository _citiesRepository;
        private User _currentUser;
        private Request _selectedRequest;
        private class RequestDisplayData
        {
            public int Id { get; set; }
            public string FullAddress { get; set; }
            public DateTime CleaningDate { get; set; }
            public string Services { get; set; }
            public string Status { get; set; }
            public decimal TotalCost { get; set; }
        }

        public CleanerRequestViewForm()
        {
            try
            {
                InitializeComponent();

                // Сначала проверяем авторизацию
                if (!CheckAuthorization())
                {
                    return;
                }

                // Затем инициализируем репозитории
                if (!InitializeRepositories())
                {
                    return;
                }

                // И только потом загружаем данные
                LoadRequests();
            }
            catch (Exception ex)
            {
                // Вместо показа диалога, просто логируем ошибку и продолжаем работу
                Console.WriteLine($"Критическая ошибка инициализации: {ex.Message}");
            }
        }

        private bool CheckAuthorization()
        {
            try
            {
                if (!Application.Current.Properties.Contains("CurrentUser"))
                {
                    ShowErrorAndGoToAuth("Ошибка авторизации. Пожалуйста, войдите снова.");
                    return false;
                }

                _currentUser = Application.Current.Properties["CurrentUser"] as User;

                if (_currentUser == null)
                {
                    ShowErrorAndGoToAuth("Ошибка: данные пользователя повреждены.");
                    return false;
                }

                if (_currentUser.Role != "Cleaner")
                {
                    ShowErrorAndGoToAuth($"Ошибка доступа: у пользователя роль '{_currentUser.Role}', требуется 'Cleaner'.");
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                ShowErrorAndGoToAuth($"Ошибка проверки авторизации: {ex.Message}");
                return false;
            }
        }

        private bool InitializeRepositories()
        {
            try
            {
                _requestsRepository = new RequestsRepository();
                _requestServicesRepository = new RequestServicesRepository();
                _servicesRepository = new ServicesRepository();
                _citiesRepository = new CitiesRepository();

                // Проверяем, что репозитории созданы
                if (_requestsRepository == null || _requestServicesRepository == null ||
                    _servicesRepository == null || _citiesRepository == null)
                {
                    // Вместо показа ошибки, просто возвращаем false
                    Console.WriteLine("Ошибка создания репозиториев");
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка создания репозиториев: {ex.Message}");
                return false;
            }
        }

        private void ShowErrorAndGoToAuth(string message)
        {
            MessageBox.Show(message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            Dispatcher.BeginInvoke(new Action(GoToAuthorization));
        }

        private void LoadRequests()
        {
            try
            {
                if (_requestsRepository == null || _currentUser == null)
                {
                    // Вместо показа ошибки, просто выходим
                    Console.WriteLine("Системная ошибка: данные не инициализированы");
                    return;
                }

                // Получаем заявки назначенные текущему клинеру
                var cleanerRequests = _requestsRepository.GetByCleanerId(_currentUser.Id) ?? new List<Request>();

                // Получаем все заявки
                var allRequests = _requestsRepository.GetAll() ?? new List<Request>();

                // Получаем доступные заявки (без клинера и со статусом "Новая")
                var availableRequests = allRequests.Where(r =>
                    r.CleanerId == null &&
                    r.Status == "Новая").ToList();

                // Объединяем списки
                var allRequestsToShow = cleanerRequests.Concat(availableRequests).Distinct().ToList();

                // Применяем фильтры и сортировку
                var filteredRequests = ApplyFilters(allRequestsToShow);
                var sortedRequests = ApplySorting(filteredRequests);

                // Создаем данные для отображения
                var displayData = sortedRequests.Select(r => new RequestDisplayData
                {
                    Id = r.Id,
                    FullAddress = GetFullAddress(r),
                    CleaningDate = r.CleaningDate,
                    Services = GetServicesString(r.Id),
                    Status = r.Status ?? "Новая",
                    TotalCost = r.TotalCost
                }).ToList();

                RequestsDataGrid.ItemsSource = displayData;

                // Если заявок нет, просто продолжаем работу без сообщений
                if (displayData.Count == 0)
                {
                    Console.WriteLine("Нет доступных заявок для отображения");
                }
            }
            catch (Exception ex)
            {
                // Вместо показа ошибки, просто логируем
                Console.WriteLine($"Ошибка загрузки заявок: {ex.Message}");
            }
        }

        private List<Request> ApplyFilters(List<Request> requests)
        {
            try
            {
                var filtered = requests;

                if (StatusFilterComboBox.SelectedItem is ComboBoxItem statusItem && statusItem.Content?.ToString() != "Все")
                {
                    string selectedStatus = statusItem.Content?.ToString();
                    if (!string.IsNullOrEmpty(selectedStatus))
                    {
                        filtered = filtered.Where(r => r.Status == selectedStatus).ToList();
                    }
                }

                return filtered;
            }
            catch
            {
                return requests;
            }
        }

        private List<Request> ApplySorting(List<Request> requests)
        {
            try
            {
                var sorted = requests;

                if (SortComboBox.SelectedItem is ComboBoxItem sortItem && sortItem.Content != null)
                {
                    switch (sortItem.Content.ToString())
                    {
                        case "По дате (новые)":
                            sorted = sorted.OrderByDescending(r => r.CleaningDate).ToList();
                            break;
                        case "По дате (старые)":
                            sorted = sorted.OrderBy(r => r.CleaningDate).ToList();
                            break;
                        case "По статусу":
                            sorted = sorted.OrderBy(r => r.Status).ToList();
                            break;
                    }
                }

                return sorted;
            }
            catch
            {
                return requests;
            }
        }

        private string GetFullAddress(Request request)
        {
            try
            {
                if (request == null) return "Неизвестный адрес";

                var city = _citiesRepository?.GetById(request.CityId);
                return city != null ? $"{city.Name}, {request.District}, {request.Address}"
                                   : $"{request.District}, {request.Address}";
            }
            catch
            {
                return $"{request?.District}, {request?.Address}";
            }
        }

        private string GetServicesString(int requestId)
        {
            try
            {
                if (_requestServicesRepository == null || _servicesRepository == null)
                    return "Неизвестно";

                var serviceIds = _requestServicesRepository.GetByRequestId(requestId)
                    ?.Select(rs => rs.ServiceId) ?? new List<int>();

                var services = serviceIds.Select(id => _servicesRepository.GetById(id)?.Name)
                    .Where(name => name != null);

                return string.Join(", ", services);
            }
            catch
            {
                return "Неизвестно";
            }
        }

        private void FilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            LoadRequests();
        }

        private void RequestsDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (RequestsDataGrid.SelectedItem is RequestDisplayData selectedItem)
                {
                    _selectedRequest = _requestsRepository?.GetById(selectedItem.Id);
                    UpdateStatusControls();
                }
                else
                {
                    _selectedRequest = null;
                    StatusComboBox.IsEnabled = false;
                    UpdateStatusButton.IsEnabled = false;
                    StatusComboBox.SelectedIndex = -1;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при выборе заявки: {ex.Message}");
            }
        }

        private void UpdateStatusControls()
        {
            if (_selectedRequest == null)
            {
                StatusComboBox.IsEnabled = false;
                UpdateStatusButton.IsEnabled = false;
                return;
            }

            StatusComboBox.IsEnabled = true;
            UpdateStatusButton.IsEnabled = true;

            // Сбрасываем выбор
            StatusComboBox.SelectedIndex = -1;

            // Устанавливаем текущий статус
            foreach (ComboBoxItem item in StatusComboBox.Items)
            {
                if (item.Content?.ToString() == _selectedRequest.Status)
                {
                    StatusComboBox.SelectedItem = item;
                    break;
                }
            }

            // Блокируем для завершенных или отмененных заявок
            if (_selectedRequest.Status == "Завершена" || _selectedRequest.Status == "Отмена")
            {
                StatusComboBox.IsEnabled = false;
                UpdateStatusButton.IsEnabled = false;
            }
        }

        private void StatusComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (_selectedRequest != null &&
                    _selectedRequest.CleanerId == null &&
                    StatusComboBox.SelectedItem is ComboBoxItem selectedItem)
                {
                    var selectedStatus = selectedItem.Content?.ToString();
                    if (selectedStatus == "В работе")
                    {
                        var result = MessageBox.Show(
                            "Вы хотите взять эту заявку в работу?",
                            "Назначение заявки",
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Question);

                        if (result == MessageBoxResult.Yes)
                        {
                            _selectedRequest.CleanerId = _currentUser.Id;
                            _selectedRequest.Status = "В работе";
                            _requestsRepository.Update(_selectedRequest);

                            MessageBox.Show("Заявка назначена на вас", "Успех",
                                MessageBoxButton.OK, MessageBoxImage.Information);
                            LoadRequests();
                        }
                        else
                        {
                            StatusComboBox.SelectedIndex = -1;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при изменении статуса: {ex.Message}");
                LoadRequests();
            }
        }

        private void UpdateStatusButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_selectedRequest == null)
                {
                    MessageBox.Show("Выберите заявку для обновления статуса", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (StatusComboBox.SelectedItem is not ComboBoxItem selectedItem)
                {
                    MessageBox.Show("Выберите новый статус", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                string newStatus = selectedItem.Content?.ToString();

                if (string.IsNullOrEmpty(newStatus))
                {
                    MessageBox.Show("Неверный статус", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (!CanChangeStatus(_selectedRequest.Status, newStatus))
                {
                    MessageBox.Show($"Нельзя изменить статус с '{_selectedRequest.Status}' на '{newStatus}'", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Если клинер берет заявку в работу
                if (_selectedRequest.CleanerId == null && newStatus == "В работе")
                {
                    _selectedRequest.CleanerId = _currentUser.Id;
                }

                // Подтверждение для завершения заявки
                if (newStatus == "Завершена")
                {
                    var result = MessageBox.Show(
                        "Вы уверены, что хотите отметить заявку как завершенную?",
                        "Подтверждение завершения",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

                    if (result != MessageBoxResult.Yes)
                    {
                        return;
                    }
                }

                _selectedRequest.Status = newStatus;
                _requestsRepository.Update(_selectedRequest);

                MessageBox.Show($"Статус заявки обновлен на '{newStatus}'", "Успех",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                LoadRequests();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при обновлении статуса: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool CanChangeStatus(string currentStatus, string newStatus)
        {
            var allowedTransitions = new Dictionary<string, List<string>>
            {
                { "Новая", new List<string> { "В работе", "Отмена" } },
                { "В работе", new List<string> { "Завершена", "Отмена" } },
                { "Завершена", new List<string>() },
                { "Отмена", new List<string>() }
            };

            return allowedTransitions.ContainsKey(currentStatus) &&
                   allowedTransitions[currentStatus].Contains(newStatus);
        }

        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (Application.Current.Properties.Contains("CurrentUser"))
                {
                    Application.Current.Properties.Remove("CurrentUser");
                }
                GoToAuthorization();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при выходе: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void GoToAuthorization()
        {
            try
            {
                this.Hide();
                var authForm = new AuthorizationForm();
                authForm.Show();
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка перехода к авторизации: {ex.Message}", "Критическая ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);

                Application.Current.Shutdown();
            }
        }

        // Добавим кнопку обновления
        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            LoadRequests();
        }
    }
}