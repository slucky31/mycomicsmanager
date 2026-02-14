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
);
#pragma warning restore CA1054, CA1056

public interface IComicSearchService
{
    Task<ComicSearchResult> SearchByIsbnAsync(string isbn, CancellationToken cancellationToken = default);
}
