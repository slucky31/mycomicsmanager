namespace Application.Interfaces;

// A book found by a comic source (Bedetheque, Bubble) that gives the series and the volume apart,
// unlike IBookSearchResult whose title mixes them.
public interface IStructuredBookResult
{
    string Title { get; }
    string Serie { get; }
    int VolumeNumber { get; }
    IReadOnlyList<string> Authors { get; }
    IReadOnlyList<string> Publishers { get; }
    DateOnly? PublishDate { get; }
    int? NumberOfPages { get; }
    Uri? CoverUrl { get; }
    bool Found { get; }

    // The source could not be searched (error, timeout, blocked): unlike !Found, it may know the book.
    bool Failed { get; }
}
