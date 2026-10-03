using System.Text.Json;
using Application.FeedImports;
using Domain.FeedImports;
using Domain.Primitives;

namespace Web.Infrastructure;

// Debrid-Link API v2 (https://debrid-link.com/api_doc/v2): every answer is {"success": bool, "value" | "error"}.
internal sealed class DebridLinkClient(HttpClient httpClient) : IDebridLinkClient
{
    private static readonly Uri s_domainsUri = new("downloader/domains", UriKind.Relative);
    private static readonly Uri s_addUri = new("downloader/add", UriKind.Relative);

    private static Serilog.ILogger Log => Serilog.Log.ForContext<DebridLinkClient>();

    public async Task<Result<IReadOnlyList<string>>> GetSupportedDomainsAsync(CancellationToken cancellationToken = default)
    {
        var value = await SendAsync(() => httpClient.GetAsync(s_domainsUri, cancellationToken), cancellationToken);
        if (value.IsFailure)
        {
            return value.Error!;
        }

        if (value.Value.ValueKind != JsonValueKind.Array)
        {
            return FeedImportError.DebridLinkUnavailable;
        }

        IReadOnlyList<string> domains = value.Value.EnumerateArray()
            .Where(d => d.ValueKind == JsonValueKind.String)
            .Select(d => d.GetString()!)
            .Where(d => !string.IsNullOrWhiteSpace(d))
            .ToList();
        return Result<IReadOnlyList<string>>.Success(domains);
    }

#pragma warning disable CA1054 // URL comes from the decision links (JSON)
    public async Task<Result<DebridLinkFile>> UnlockAsync(string url, CancellationToken cancellationToken = default)
#pragma warning restore CA1054
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(url);

        var value = await SendAsync(() => httpClient.PostAsJsonAsync(s_addUri, new { url }, cancellationToken), cancellationToken);
        if (value.IsFailure)
        {
            return value.Error!;
        }

        // A folder link returns an array of links: the decision describes a single file, the first one is used.
        var link = value.Value.ValueKind == JsonValueKind.Array && value.Value.GetArrayLength() > 0
            ? value.Value[0]
            : value.Value;
        return ToFile(link);
    }

    internal static Result<DebridLinkFile> ToFile(JsonElement link)
    {
        if (link.ValueKind != JsonValueKind.Object ||
            !link.TryGetProperty("downloadUrl", out var downloadUrl) ||
            downloadUrl.ValueKind != JsonValueKind.String ||
            !Uri.TryCreate(downloadUrl.GetString(), UriKind.Absolute, out var downloadUri))
        {
            Log.Warning("Debrid-Link returned a link without a valid downloadUrl");
            return FeedImportError.DebridLinkUnavailable;
        }

        var name = link.TryGetProperty("name", out var nameElement) && nameElement.ValueKind == JsonValueKind.String
            ? nameElement.GetString() ?? string.Empty
            : string.Empty;
        long? size = link.TryGetProperty("size", out var sizeElement) && sizeElement.TryGetInt64(out var bytes) && bytes > 0
            ? bytes
            : null;
        return new DebridLinkFile(name, size, downloadUri);
    }

    // Error codes listed in the API documentation ("Errors" table).
    internal static TError MapError(string? code) => code switch
    {
        "badToken" => FeedImportError.DebridLinkUnauthorized,
        "maxLink" or "maxData" => FeedImportError.DebridLinkQuotaReached,
        "maxLinkHost" or "maxDataHost" => FeedImportError.DebridLinkHostQuotaReached,
        "fileNotFound" or "fileNotAvailable" or "infringingFile" or "badFileUrl" or "badFilePassword"
            => WithCode(FeedImportError.DebridLinkFileUnavailable, code),
        "hostNotValid" or "notFreeHost" or "maintenanceHost" or "notDebrid"
            => WithCode(FeedImportError.DebridLinkHostNotSupported, code),
        null or "" => FeedImportError.DebridLinkUnavailable,
        _ => WithCode(FeedImportError.DebridLinkUnavailable, code)
    };

    private static TError WithCode(TError error, string code) => new(error.Code, $"{error.Description} ({code})");

    private static async Task<Result<JsonElement>> SendAsync(Func<Task<HttpResponseMessage>> send, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await send();
            var body = await ReadBodyAsync(response, cancellationToken);
            if (body is { } json &&
                json.TryGetProperty("success", out var success) && success.ValueKind == JsonValueKind.True &&
                json.TryGetProperty("value", out var value))
            {
                return value.Clone();
            }

            var code = body is { } error && error.TryGetProperty("error", out var errorCode) && errorCode.ValueKind == JsonValueKind.String
                ? errorCode.GetString()
                : null;
            Log.Warning("Debrid-Link returned {StatusCode} for {Uri}: {ErrorCode}",
                (int)response.StatusCode, response.RequestMessage?.RequestUri?.AbsolutePath, code ?? "(no error code)");
            return MapError(code);
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException
                                   || (ex is TaskCanceledException && !cancellationToken.IsCancellationRequested))
        {
            Log.Warning(ex, "Debrid-Link request failed");
            return FeedImportError.DebridLinkUnavailable;
        }
    }

    private static async Task<JsonElement?> ReadBodyAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var text = await response.Content.ReadAsStringAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        using var document = JsonDocument.Parse(text);
        return document.RootElement.ValueKind == JsonValueKind.Object ? document.RootElement.Clone() : null;
    }
}
