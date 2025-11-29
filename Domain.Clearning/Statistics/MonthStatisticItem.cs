namespace Domain.Statistics
{
    public record MonthStatisticItem
    {
        public required int Year { get; set; }
        public required int Month { get; set; }
        public required int Count { get; set; }

        public string GetMonthName()
        {
            var date = new DateTime(Year, Month, 1);
            return date.ToString("MMM yyyy");
        }
    }
}