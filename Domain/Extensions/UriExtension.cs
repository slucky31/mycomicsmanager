namespace Domain.Extensions;

public static class UriExtension
{
    public static bool IsAllowedHttpsHost(this Uri? uri, IReadOnlySet<string> allowedHosts) =>
        uri is not null && uri.Scheme == Uri.UriSchemeHttps && allowedHosts.Contains(uri.Host);
}
