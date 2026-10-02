using Domain.Primitives;

namespace Application.FeedImports;

public interface IArticlePageFetcher
{
    // Returns the HTML of an article page; implementations only reach FeedImport:AllowedSourceHosts.
    Task<Result<string>> GetHtmlAsync(Uri pageUri, CancellationToken cancellationToken = default);
}
