namespace Domain.Extensions;

public static class HostExtension
{
    // True when host is one of the domains, or a subdomain of one (cdn.1fichier.com matches 1fichier.com).
    public static bool IsSameOrSubdomainOf(this string? host, IEnumerable<string> domains)
    {
        if (string.IsNullOrWhiteSpace(host) || domains is null)
        {
            return false;
        }

        var normalizedHost = host.Trim().TrimEnd('.');
        foreach (var domain in domains)
        {
            var normalizedDomain = domain?.Trim().TrimEnd('.');
            if (string.IsNullOrEmpty(normalizedDomain))
            {
                continue;
            }

            if (normalizedHost.Equals(normalizedDomain, StringComparison.OrdinalIgnoreCase) ||
                normalizedHost.EndsWith("." + normalizedDomain, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
