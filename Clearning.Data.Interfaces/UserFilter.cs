namespace Cleaning.Data.Interfaces
{
    public record UserFilter
    {
        public static UserFilter Empty => new();

        public string? Role { get; init; }
        public DateOnly? CreatedFrom { get; init; }
        public DateOnly? CreatedTo { get; init; }
    }
}