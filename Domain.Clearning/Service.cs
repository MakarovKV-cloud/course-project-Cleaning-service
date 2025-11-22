namespace Domain.Cleaning
{
    public class Service
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal PricePerSquareMeter { get; set; }
        public bool RequiresArea { get; set; }
    }
}