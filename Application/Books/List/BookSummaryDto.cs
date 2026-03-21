namespace Application.Books.List;

public sealed class BookSummaryDto
{
    public Guid Id { get; init; }
    public string Serie { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string ISBN { get; init; } = string.Empty;
    public int VolumeNumber { get; init; }
    public string ImageLink { get; init; } = string.Empty;
    public string Authors { get; init; } = string.Empty;
    public string Publishers { get; init; } = string.Empty;
    public DateTime? LastRead { get; init; }
    public int LastRating { get; init; }
    public int ReadCount { get; init; }
}
