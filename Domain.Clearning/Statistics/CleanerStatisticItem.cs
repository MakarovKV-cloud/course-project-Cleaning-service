namespace Domain.Statistics
{
    public record CleanerStatisticItem
    {
        public required string CleanerName { get; set; }
        public required int Count { get; set; }
    }
}