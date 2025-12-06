using System;

namespace Domain.Cleaning
{
    public class Payment
    {
        public int Id { get; set; }
        public int RequestId { get; set; }
        public Request? Request { get; set; } // Навигационное свойство
        public string CardNumberMasked { get; set; } = string.Empty;
        public DateTime PaymentDate { get; set; } = DateTime.Now;
        public decimal Amount { get; set; }
        public string Status { get; set; } = "Pending";
        public string TransactionId { get; set; } = string.Empty;
    }
}