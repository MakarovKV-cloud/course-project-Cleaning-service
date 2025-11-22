using System;

namespace Domain.Cleaning
{
    public class Request
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public decimal Area { get; set; }
        public DateTime CleaningDate { get; set; }
        public int CityId { get; set; }
        public string District { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public decimal TotalCost { get; set; }
        public string Status { get; set; } = "Новая";
        public int? CleanerId { get; set; }
        public int PaymentId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}