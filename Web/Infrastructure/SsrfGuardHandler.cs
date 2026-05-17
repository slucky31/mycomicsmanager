namespace Web.Infrastructure;

internal sealed class SsrfGuardHandler(IReadOnlySet<string> allowedHosts) : DelegatingHandler
{
    private static Serilog.ILogger Log => Serilog.Log.ForContext<SsrfGuardHandler>();

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var uri = request.RequestUri;
        if (uri is null || uri.Scheme != Uri.UriSchemeHttps || !allowedHosts.Contains(uri.Host))
        {
            Log.Warning("SSRF guard blocked outgoing request to {Uri}", uri);
            throw new HttpRequestException($"SSRF guard: request to '{uri}' is not permitted.");
        }
        return base.SendAsync(request, cancellationToken);
    }
}
