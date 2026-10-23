namespace Application.Interfaces;

public interface IBookSearchResult
{
    string Title { get; }
    string? Subtitle { get; }
    IReadOnlyList<string> Authors { get; }
    IReadOnlyList<string> Publishers { get; }
    DateOnly? PublishDate { get; }
    int? NumberOfPages { get; }
    Uri? CoverUrl { get; }
    bool Found { get; }

    // The source could not be searched (error, timeout, blocked): unlike !Found, it may know the book.
    bool Failed { get; }

    // The series and the volume when the source gives them apart from the title, null otherwise.
    string? Serie { get; }

    int? VolumeNumber { get; }
}
