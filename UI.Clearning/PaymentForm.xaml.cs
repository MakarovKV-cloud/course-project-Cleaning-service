using Cleaning.Data.JsonStorage;
using Domain.Cleaning;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;

namespace UI.Cleaning
{
    public partial class PaymentForm : Window
    {
        private readonly RequestsRepository _requestsRepository;
        private readonly PaymentsRepository _paymentsRepository;
        private readonly RequestServicesRepository _requestServicesRepository;
        private User _currentUser;
        private RequestData _requestData;

        public PaymentForm()
        {
            InitializeComponent();
            _requestsRepository = new RequestsRepository();
            _paymentsRepository = new PaymentsRepository();
            _requestServicesRepository = new RequestServicesRepository();
            _currentUser = (User)Application.Current.Properties["CurrentUser"];

            LoadRequestData();
        }

        private void LoadRequestData()
        {
            if (Application.Current.Properties.Contains("PendingRequest"))
            {
                _requestData = (RequestData)Application.Current.Properties["PendingRequest"];
                AmountTextBlock.Text = $"{_requestData.TotalCost} руб.";

                RequestDetailsTextBlock.Text =
                    $"Площадь: {_requestData.Area} м²\n" +
                    $"Дата: {_requestData.CleaningDate:dd.MM.yyyy}\n" +
                    $"Адрес: {_requestData.City.Name}, {_requestData.District}, {_requestData.Address}\n" +
                    $"Услуги: {string.Join(", ", _requestData.SelectedServices.Select(s => s.Name))}";
            }
            else
            {
                MessageBox.Show("Данные заявки не найдены", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                GoBack();
            }
        }

        private void CardNumberTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string text = CardNumberTextBox.Text.Replace(" ", "");
            if (text.Length > 0 && !Regex.IsMatch(text, @"^\d+$"))
            {
                text = new string(text.Where(char.IsDigit).ToArray());
            }

            if (text.Length > 16) text = text.Substring(0, 16);

            string formatted = "";
            for (int i = 0; i < text.Length; i++)
            {
                if (i > 0 && i % 4 == 0) formatted += " ";
                formatted += text[i];
            }

            CardNumberTextBox.Text = formatted;
            CardNumberTextBox.CaretIndex = formatted.Length;
        }

        private void ExpiryDateTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string text = ExpiryDateTextBox.Text.Replace("/", "");
            if (text.Length > 0 && !Regex.IsMatch(text, @"^\d+$"))
            {
                text = new string(text.Where(char.IsDigit).ToArray());
            }

            if (text.Length > 4) text = text.Substring(0, 4);

            if (text.Length >= 2)
            {
                text = text.Insert(2, "/");
            }

            ExpiryDateTextBox.Text = text;
            ExpiryDateTextBox.CaretIndex = text.Length;
        }

        private bool ValidatePaymentData()
        {
            string cardNumber = CardNumberTextBox.Text.Replace(" ", "");
            string expiryDate = ExpiryDateTextBox.Text;
            string cvv = CvvTextBox.Text;

            if (cardNumber.Length != 16 || !cardNumber.All(char.IsDigit))
            {
                MessageBox.Show("Введите корректный номер карты (16 цифр)", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            if (!Regex.IsMatch(expiryDate, @"^\d{2}/\d{2}$"))
            {
                MessageBox.Show("Введите корректный срок действия (ММ/ГГ)", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            if (cvv.Length != 3 || !cvv.All(char.IsDigit))
            {
                MessageBox.Show("Введите корректный CVV код (3 цифры)", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            var parts = expiryDate.Split('/');
            if (parts.Length == 2)
            {
                int month = int.Parse(parts[0]);
                int year = int.Parse(parts[1]) + 2000;

                if (month < 1 || month > 12)
                {
                    MessageBox.Show("Неверный месяц срока действия", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
                }

                if (year < DateTime.Now.Year || (year == DateTime.Now.Year && month < DateTime.Now.Month))
                {
                    MessageBox.Show("Срок действия карты истек", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
                }
            }

            return true;
        }

        private void PayButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidatePaymentData())
                return;

            try
            {
                var request = new Request
                {
                    UserId = _currentUser.Id,
                    Area = _requestData.Area,
                    CleaningDate = _requestData.CleaningDate,
                    CityId = _requestData.City.Id,
                    District = _requestData.District,
                    Address = _requestData.Address,
                    TotalCost = _requestData.TotalCost,
                    Status = "Новая",
                    CleanerId = null,
                    PaymentId = 0
                };

                int requestId = _requestsRepository.Add(request);

                foreach (var service in _requestData.SelectedServices)
                {
                    var requestService = new RequestService
                    {
                        RequestId = requestId,
                        ServiceId = service.Id
                    };
                    _requestServicesRepository.Add(requestService);
                }

                var payment = new Payment
                {
                    RequestId = requestId,
                    CardNumberMasked = $"**** **** **** {CardNumberTextBox.Text.Replace(" ", "").Substring(12)}",
                    Amount = _requestData.TotalCost,
                    Status = "Успех",
                    TransactionId = Guid.NewGuid().ToString()
                };

                int paymentId = _paymentsRepository.Add(payment);

                request.PaymentId = paymentId;
                _requestsRepository.Update(request);

                Application.Current.Properties.Remove("PendingRequest");

                MessageBox.Show("Оплата прошла успешно! Заявка создана.", "Успех",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                this.Hide();
                var clientForm = new ClientViewForm();
                clientForm.Show();
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при создании заявки: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            GoBack();
        }

        private void GoBack()
        {
            this.Hide();
            var addRequestForm = new AddRequestsForm();
            addRequestForm.Show();
            this.Close();
        }
    }
}