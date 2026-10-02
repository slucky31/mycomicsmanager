namespace Domain.Extensions;

public static class UriExtension
{
    public static bool IsAllowedHttpsHost(this Uri? uri, IReadOnlySet<string> allowedHosts) =>
        uri.IsAllowedHost(allowedHosts, allowHttp: false);

    // allowHttp is only meant for internal services reached over a private network (e.g. Miniflux on the Docker network).
    public static bool IsAllowedHost(this Uri? uri, IReadOnlySet<string> allowedHosts, bool allowHttp) =>
        uri is not null
        && (uri.Scheme == Uri.UriSchemeHttps || (allowHttp && uri.Scheme == Uri.UriSchemeHttp))
        && allowedHosts.Contains(uri.Host);
}
