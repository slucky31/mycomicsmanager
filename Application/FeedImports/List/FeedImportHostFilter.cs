using Domain.FeedImports;

namespace Application.FeedImports.List;

public static class FeedImportHostFilter
{
    // A blank filter is no filter.
    public static string? Normalize(string? host) =>
        string.IsNullOrWhiteSpace(host) ? null : host.Trim();
}
