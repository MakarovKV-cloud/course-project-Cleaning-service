using Cleaning.Data.JsonStorage;
using Domain.Cleaning;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace UI.Cleaning
{
    public partial class AddRequestsForm : Window
    {
        private readonly ServicesRepository _servicesRepository;
        private readonly CitiesRepository _citiesRepository;
        private User _currentUser;

        private List<ServiceViewModel> _services;
        private decimal _totalCost = 0;

        public AddRequestsForm()
        {
            InitializeComponent();
            _servicesRepository = new ServicesRepository();
            _citiesRepository = new CitiesRepository();
            _currentUser = (User)Application.Current.Properties["CurrentUser"];

            LoadData();
        }

        private void LoadData()
        {
            var services = _servicesRepository.GetAll();
            _services = services.Select(s => new ServiceViewModel
            {
                Service = s,
                IsSelected = false,
                DisplayName = s.RequiresArea ?
                    $"{s.Name} - {s.PricePerSquareMeter} руб./м²" :
                    $"{s.Name} - {s.PricePerSquareMeter} руб. (фиксированно)"
            }).ToList();

            ServicesItemsControl.ItemsSource = _services;

            var cities = _citiesRepository.GetAll();
            CityComboBox.ItemsSource = cities;
        }

        private void CalculateTotalCost()
        {
            _totalCost = 0;
            decimal area = 0;

            if (decimal.TryParse(AreaTextBox.Text, out area) && area > 0)
            {
                foreach (var serviceVm in _services.Where(s => s.IsSelected))
                {
                    if (serviceVm.Service.RequiresArea)
                    {
                        _totalCost += serviceVm.Service.PricePerSquareMeter * area;
                    }
                    else
                    {
                        _totalCost += serviceVm.Service.PricePerSquareMeter;
                    }
                }
            }

            TotalCostTextBlock.Text = $"{_totalCost} руб.";
        }

        private void AreaTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            CalculateTotalCost();
        }

        private void ServiceCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            CalculateTotalCost();
        }

        private void ProceedToPaymentButton_Click(object sender, RoutedEventArgs e)
        {
            if (!decimal.TryParse(AreaTextBox.Text, out decimal area) || area <= 0)
            {
                MessageBox.Show("Введите корректную площадь", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (_services.Count(s => s.IsSelected) == 0)
            {
                MessageBox.Show("Выберите хотя бы одну услугу", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (CleaningDatePicker.SelectedDate == null)
            {
                MessageBox.Show("Выберите дату клининга", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (CityComboBox.SelectedItem == null)
            {
                MessageBox.Show("Выберите город", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (string.IsNullOrWhiteSpace(DistrictTextBox.Text))
            {
                MessageBox.Show("Введите район", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (string.IsNullOrWhiteSpace(AddressTextBox.Text))
            {
                MessageBox.Show("Введите адрес", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Формируем список выбранных услуг для подтверждения
            string selectedServices = string.Join("\n", _services
                .Where(s => s.IsSelected)
                .Select(s => $"  • {s.DisplayName}"));

            var result = MessageBox.Show(
                $"Подтвердите данные заявки:\n\n" +
                $"Площадь: {area} м²\n" +
                $"Дата: {CleaningDatePicker.SelectedDate.Value:dd.MM.yyyy}\n" +
                $"Адрес: {((City)CityComboBox.SelectedItem).Name}, {DistrictTextBox.Text}, {AddressTextBox.Text}\n" +
                $"Выбранные услуги:\n{selectedServices}\n" +
                $"Итоговая стоимость: {_totalCost} руб.",
                "Подтверждение заявки",
                MessageBoxButton.OKCancel,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.OK)
            {
                var requestData = new RequestData
                {
                    Area = area,
                    SelectedServices = _services.Where(s => s.IsSelected).Select(s => s.Service).ToList(),
                    CleaningDate = CleaningDatePicker.SelectedDate.Value,
                    City = (City)CityComboBox.SelectedItem,
                    District = DistrictTextBox.Text,
                    Address = AddressTextBox.Text,
                    TotalCost = _totalCost
                };

                Application.Current.Properties["PendingRequest"] = requestData;

                this.Hide();
                var paymentForm = new PaymentForm();
                paymentForm.Show();
                this.Close();
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            this.Hide();
            var clientForm = new ClientViewForm();
            clientForm.Show();
            this.Close();
        }
    }

    public class ServiceViewModel
    {
        public Service Service { get; set; }
        public bool IsSelected { get; set; }
        public string DisplayName { get; set; }
    }


    public class RequestData
    {
        public decimal Area { get; set; }
        public List<Service> SelectedServices { get; set; }
        public DateTime CleaningDate { get; set; }
        public City City { get; set; }
        public string District { get; set; }
        public string Address { get; set; }
        public decimal TotalCost { get; set; }
    }
}