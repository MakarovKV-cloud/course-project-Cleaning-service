using System;

namespace Domain.Cleaning
{
    public class Request
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public User? User { get; set; } // Навигационное свойство к User
        public decimal Area { get; set; }
        public DateTime CleaningDate { get; set; }
        public int CityId { get; set; }
        public City? City { get; set; } // Навигационное свойство к City
        public string District { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public decimal TotalCost { get; set; }
        public string Status { get; set; } = "Новая";
        public int? CleanerId { get; set; }
        public User? Cleaner { get; set; } // Навигационное свойство к Cleaner (User)
        public int PaymentId { get; set; }
        public Payment? Payment { get; set; } // Навигационное свойство к Payment
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public List<RequestService>? RequestServices { get; set; }
    }
}