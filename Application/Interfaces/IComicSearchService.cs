namespace Application.Interfaces;

#pragma warning disable CA1054, CA1056 // URI parameters/properties should not be strings
public record ComicSearchResult(
    string Title,
    string Serie,
    string Isbn,
    int VolumeNumber,
    string ImageUrl,
    string Authors,
    string Publishers,
    DateOnly? PublishDate,
    int? NumberOfPages,
    bool Found
)
{
    // The catalogue that knew the book ("BnF", "Google Books"...), null when it was not found.
    public string? Source { get; init; }
}
#pragma warning restore CA1054, CA1056

public interface IComicSearchService
{
    Task<ComicSearchResult> SearchByIsbnAsync(string isbn, CancellationToken cancellationToken = default);
    Task<ComicSearchResult> SearchByIsbnWithLocalCoverAsync(
        string isbn,
        Stream? coverStream,
        string? coverFileName,
        CancellationToken cancellationToken = default);
    // The volume is null when the source does not tell it.
    (string Title, string Serie, int? VolumeNumber) ParseTitleInfo(IBookSearchResult result);
    Task<string> UploadCoverAsync(Uri coverUrl, string isbn, CancellationToken cancellationToken = default);
}
