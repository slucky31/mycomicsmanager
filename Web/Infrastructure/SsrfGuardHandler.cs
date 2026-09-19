using System.Net;
using Domain.Extensions;

namespace Web.Infrastructure;

internal sealed class SsrfGuardHandler(IReadOnlySet<string> allowedHosts) : DelegatingHandler
{
    private const int MaxRedirects = 5;

    private static Serilog.ILogger Log => Serilog.Log.ForContext<SsrfGuardHandler>();

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var currentRequest = request;
        HttpRequestMessage? ownedClone = null;
        try
        {
            for (var redirectCount = 0; ; redirectCount++)
            {
                EnsureAllowed(currentRequest.RequestUri);

                var response = await base.SendAsync(currentRequest, cancellationToken);
                if (!IsRedirect(response.StatusCode) || response.Headers.Location is null)
                {
                    return response;
                }

                if (redirectCount >= MaxRedirects)
                {
                    response.Dispose();
                    throw new HttpRequestException("SSRF guard: too many redirects.");
                }

                var location = response.Headers.Location.IsAbsoluteUri
                    ? response.Headers.Location
                    : new Uri(currentRequest.RequestUri!, response.Headers.Location);
                var statusCode = response.StatusCode;
                response.Dispose();

                ownedClone?.Dispose();
                currentRequest = CloneAsRedirect(currentRequest, location, statusCode);
                ownedClone = currentRequest;
            }
        }
        finally
        {
            ownedClone?.Dispose();
        }
    }

    private void EnsureAllowed(Uri? uri)
    {
        if (!uri.IsAllowedHttpsHost(allowedHosts))
        {
            Log.Warning("SSRF guard blocked outgoing request to {Uri}", uri);
            throw new HttpRequestException($"SSRF guard: request to '{uri}' is not permitted.");
        }
    }

    private static bool IsRedirect(HttpStatusCode statusCode) =>
        statusCode is HttpStatusCode.MovedPermanently or HttpStatusCode.Found or HttpStatusCode.SeeOther
            or HttpStatusCode.TemporaryRedirect or HttpStatusCode.PermanentRedirect;

    private static readonly HashSet<string> s_crossHostSensitiveHeaders =
        new(["Authorization", "Cookie"], StringComparer.OrdinalIgnoreCase);

    // 307/308 must preserve the original method per spec; other redirect codes conventionally downgrade to GET.
    // ponytail: body isn't carried over on 307/308; fine since every client behind this handler only issues GET requests.
    private static HttpRequestMessage CloneAsRedirect(HttpRequestMessage original, Uri newUri, HttpStatusCode statusCode)
    {
        var method = statusCode is HttpStatusCode.TemporaryRedirect or HttpStatusCode.PermanentRedirect
            ? original.Method
            : HttpMethod.Get;
        var clone = new HttpRequestMessage(method, newUri);
        var crossHost = !string.Equals(original.RequestUri?.Host, newUri.Host, StringComparison.OrdinalIgnoreCase);
        foreach (var header in original.Headers)
        {
            if (crossHost && s_crossHostSensitiveHeaders.Contains(header.Key))
            {
                continue;
            }
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }
        return clone;
    }
}
