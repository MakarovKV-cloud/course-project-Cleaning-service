namespace Cleaning.Data.Interfaces
{
    public record RequestFilter
    {
        public static RequestFilter Empty => new();

        public DateOnly? StartDate { get; init; }
        public DateOnly? EndDate { get; init; }
        public int? CleanerId { get; init; }
        public int? ClientId { get; init; }
        public string? Status { get; init; }
        public int? CityId { get; init; }
    }
}