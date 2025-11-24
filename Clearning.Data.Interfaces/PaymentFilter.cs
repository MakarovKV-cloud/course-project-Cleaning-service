namespace Cleaning.Data.Interfaces
{
    public record PaymentFilter
    {
        public static PaymentFilter Empty => new();

        public DateOnly? StartDate { get; init; }
        public DateOnly? EndDate { get; init; }
        public string? Status { get; init; }
        public int? RequestId { get; init; }
    }
}