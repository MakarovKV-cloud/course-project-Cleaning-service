using Cleaning.Data.JsonStorage;
using Domain.Cleaning;
using System;
using System.Linq;
using System.Windows;

namespace UI.Cleaning
{
    public partial class ClientViewForm : Window
    {
        private readonly RequestsRepository _requestsRepository;
        private readonly PaymentsRepository _paymentsRepository;
        private readonly RequestServicesRepository _requestServicesRepository;
        private readonly ServicesRepository _servicesRepository;
        private readonly CitiesRepository _citiesRepository;
        private User _currentUser;

        private class RequestDisplayData
        {
            public int Id { get; set; }
            public decimal Area { get; set; }
            public DateTime CleaningDate { get; set; }
            public string FullAddress { get; set; }
            public string Services { get; set; }
            public string Status { get; set; }
            public decimal TotalCost { get; set; }
        }

        public ClientViewForm()
        {
            InitializeComponent();
            _requestsRepository = new RequestsRepository();
            _paymentsRepository = new PaymentsRepository();
            _requestServicesRepository = new RequestServicesRepository();
            _servicesRepository = new ServicesRepository();
            _citiesRepository = new CitiesRepository();

            _currentUser = (User)Application.Current.Properties["CurrentUser"];
            LoadRequests();
        }

        private void LoadRequests()
        {
            try
            {
                var userRequests = _requestsRepository.GetByUserId(_currentUser.Id);

                var displayData = userRequests.Select(r => new RequestDisplayData
                {
                    Id = r.Id,
                    Area = r.Area,
                    CleaningDate = r.CleaningDate,
                    FullAddress = GetFullAddress(r),
                    Services = GetServicesString(r.Id),
                    Status = r.Status,
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

        private string GetServicesString(int requestId)
        {
            try
            {
                var serviceIds = _requestServicesRepository.GetByRequestId(requestId)
                    .Select(rs => rs.ServiceId);
                var services = serviceIds.Select(id => _servicesRepository.GetById(id)?.Name)
                    .Where(name => name != null);
                return string.Join(", ", services);
            }
            catch
            {
                return "Неизвестно";
            }
        }

        private void CreateRequestButton_Click(object sender, RoutedEventArgs e)
        {
            this.Hide();
            var addRequestForm = new AddRequestsForm();
            addRequestForm.Show();
            this.Close();
        }

        private void DeleteRequestButton_Click(object sender, RoutedEventArgs e)
        {
            if (RequestsDataGrid.SelectedItem is RequestDisplayData selectedItem)
            {
                int requestId = selectedItem.Id;

                var request = _requestsRepository.GetById(requestId);
                if (request != null && request.Status == "Новая")
                {
                    var result = MessageBox.Show(
                        "Вы уверены, что хотите отменить эту заявку?",
                        "Подтверждение отмены",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

                    if (result == MessageBoxResult.Yes)
                    {
                        request.Status = "Отмена";
                        request.CleanerId = null;
                        _requestsRepository.Update(request);

                        var payment = _paymentsRepository.GetByRequestId(requestId);
                        if (payment != null)
                        {
                            payment.Status = "Отмена";
                            _paymentsRepository.Update(payment);
                        }

                        LoadRequests();
                        MessageBox.Show("Заявка отменена", "Успех",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                else
                {
                    MessageBox.Show("Можно отменять только заявки со статусом 'Новая'", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show("Выберите заявку для отмены", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Properties.Remove("CurrentUser");
            this.Hide();
            var authForm = new AuthorizationForm();
            authForm.Show();
            this.Close();
        }

        private void RequestsDataGrid_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            DeleteRequestButton.IsEnabled = RequestsDataGrid.SelectedItem != null;
        }
    }
}