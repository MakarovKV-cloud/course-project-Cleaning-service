using Cleaning.Data.Interfaces;
using Domain.Cleaning;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace Cleaning.Data.JsonStorage
{
    public class PaymentsRepository : IPaymentsRepository
    {
        private readonly List<Payment> _payments = ReadPayments();
        private static int _nextId = GetNextId();

        public int Add(Payment payment)
        {
            payment.Id = _nextId++;
            _payments.Add(payment);
            SavePayments();
            return payment.Id;
        }

        public bool Delete(int id)
        {
            var paymentToDelete = _payments.FirstOrDefault(p => p.Id == id);
            if (paymentToDelete != null)
            {
                _payments.Remove(paymentToDelete);
                SavePayments();
                return true;
            }
            return false;
        }

        public List<Payment> GetAll()
        {
            return _payments.ToList();
        }

        public Payment? GetById(int id)
        {
            return _payments.FirstOrDefault(p => p.Id == id);
        }

        public Payment? GetByRequestId(int requestId)
        {
            return _payments.FirstOrDefault(p => p.RequestId == requestId);
        }

        public bool Update(Payment payment)
        {
            var existingPayment = _payments.FirstOrDefault(p => p.Id == payment.Id);
            if (existingPayment != null)
            {
                existingPayment.RequestId = payment.RequestId;
                existingPayment.CardNumberMasked = payment.CardNumberMasked;
                existingPayment.PaymentDate = payment.PaymentDate;
                existingPayment.Amount = payment.Amount;
                existingPayment.Status = payment.Status;
                existingPayment.TransactionId = payment.TransactionId;
                SavePayments();
                return true;
            }
            return false;
        }

        private static List<Payment> ReadPayments()
        {
            try
            {
                var paymentsJson = File.ReadAllText("database-payments.json");
                if (string.IsNullOrWhiteSpace(paymentsJson))
                {
                    return new List<Payment>();
                }
                return JsonSerializer.Deserialize<List<Payment>>(paymentsJson) ?? new List<Payment>();
            }
            catch (FileNotFoundException)
            {
                File.WriteAllText("database-payments.json", "[]");
                return new List<Payment>();
            }
            catch (Exception)
            {
                File.WriteAllText("database-payments.json", "[]");
                return new List<Payment>();
            }
        }

        private void SavePayments()
        {
            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                var paymentsJson = JsonSerializer.Serialize(_payments, options);
                File.WriteAllText("database-payments.json", paymentsJson);
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка при сохранении платежей: {ex.Message}");
            }
        }

        private static int GetNextId()
        {
            try
            {
                var payments = ReadPayments();
                return payments.Count > 0 ? payments.Max(p => p.Id) + 1 : 1;
            }
            catch
            {
                return 1;
            }
        }
    }
}