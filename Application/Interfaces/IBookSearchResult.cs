namespace Application.Interfaces;

public interface IBookSearchResult
{
    string Title { get; }
    string? Subtitle { get; }
    IReadOnlyList<string> Authors { get; }
    IReadOnlyList<string> Publishers { get; }
    string? PublishDate { get; }
    int? NumberOfPages { get; }
    Uri? CoverUrl { get; }
    bool Found { get; }
}
