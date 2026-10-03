using System.Text;
using Application.FeedImports;
using Domain.FeedImports;
using Domain.Primitives;

namespace Web.Infrastructure;

internal sealed class ArticlePageFetcher(HttpClient httpClient) : IArticlePageFetcher
{
    internal const int MaxPageBytes = 5 * 1024 * 1024;

    private static Serilog.ILogger Log => Serilog.Log.ForContext<ArticlePageFetcher>();

    public async Task<Result<string>> GetHtmlAsync(Uri pageUri, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(pageUri);
        try
        {
            using var response = await httpClient.GetAsync(pageUri, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                Log.Warning("Article page {Url} returned {StatusCode}", pageUri, (int)response.StatusCode);
                return new TError(FeedImportError.PageUnavailable.Code, $"{FeedImportError.PageUnavailable.Description} (HTTP {(int)response.StatusCode})");
            }

            if (response.Content.Headers.ContentLength > MaxPageBytes)
            {
                return new TError(FeedImportError.PageUnavailable.Code, "La page de l'article est trop volumineuse.");
            }

            return await ReadLimitedAsync(response.Content, cancellationToken);
        }
        catch (Exception ex) when (ex is HttpRequestException || (ex is TaskCanceledException && !cancellationToken.IsCancellationRequested))
        {
            Log.Warning(ex, "Article page {Url} could not be fetched", pageUri);
            return FeedImportError.PageUnavailable;
        }
    }

    // The page is read with a hard cap: a missing or lying Content-Length cannot exhaust memory.
    private static async Task<Result<string>> ReadLimitedAsync(HttpContent content, CancellationToken cancellationToken)
    {
        await using var stream = await content.ReadAsStreamAsync(cancellationToken);
        using var buffer = new MemoryStream();
        var chunk = new byte[81920];
        int read;
        while ((read = await stream.ReadAsync(chunk, cancellationToken)) > 0)
        {
            if (buffer.Length + read > MaxPageBytes)
            {
                return new TError(FeedImportError.PageUnavailable.Code, "La page de l'article est trop volumineuse.");
            }
            await buffer.WriteAsync(chunk.AsMemory(0, read), cancellationToken);
        }

        var encoding = GetEncoding(content.Headers.ContentType?.CharSet);
        return encoding.GetString(buffer.GetBuffer(), 0, (int)buffer.Length);
    }

    private static Encoding GetEncoding(string? charSet)
    {
        if (string.IsNullOrWhiteSpace(charSet))
        {
            return Encoding.UTF8;
        }

        try
        {
            return Encoding.GetEncoding(charSet.Trim('"'));
        }
        catch (ArgumentException)
        {
            return Encoding.UTF8;
        }
    }
}
