namespace Domain.Cleaning
{
    public class RequestService
    {
        public int Id { get; set; }
        public int RequestId { get; set; }
        public Request? Request { get; set; } // Навигационное свойство
        public int ServiceId { get; set; }
        public Service? Service { get; set; } // Навигационное свойство
    }
}