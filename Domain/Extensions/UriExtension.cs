namespace Domain.Extensions;

public static class UriExtension
{
    public static bool IsAllowedHttpsHost(this Uri? uri, IReadOnlySet<string> allowedHosts) =>
        uri.IsAllowedHost(allowedHosts, allowHttp: false);

    // allowHttp is only meant for internal services reached over a private network (e.g. Miniflux on the Docker network).
    // allowSubdomains accepts subdomains of the allowed hosts (file hosters often serve from cdn.<host>).
    public static bool IsAllowedHost(this Uri? uri, IReadOnlySet<string> allowedHosts, bool allowHttp, bool allowSubdomains = false) =>
        uri is not null
        && (uri.Scheme == Uri.UriSchemeHttps || (allowHttp && uri.Scheme == Uri.UriSchemeHttp))
        && (allowedHosts.Contains(uri.Host) || (allowSubdomains && uri.Host.IsSameOrSubdomainOf(allowedHosts)));
}
