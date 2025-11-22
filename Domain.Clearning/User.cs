using System;

namespace Domain.Cleaning
{
    public class User
    {
        public int Id { get; set; }
        public string LastName { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string? MiddleName { get; set; }
        public string Login { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Role { get; set; } = "Client";
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}