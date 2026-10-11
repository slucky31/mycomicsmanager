using Domain.Extensions;
using Domain.FeedImports;

namespace Application.FeedImports.Analysis;

// Turns the links of an article into books (candidates), each with its mirrors.
// Mirrors = same file on several hosts; several books = different file names (or groups given by a site extractor).
public static class DownloadLinkGrouper
{
    public static LinkGroupingResult Group(IReadOnlyList<ExtractedLink> links, string articleTitle)
    {
        ArgumentNullException.ThrowIfNull(links);
        if (links.Count == 0)
        {
            return new LinkGroupingResult([], IsAmbiguous: false, AmbiguityReason: null);
        }

        if (links.All(l => !string.IsNullOrEmpty(l.GroupKey)))
        {
            var groups = links.GroupBy(l => l.GroupKey!, StringComparer.Ordinal).Select(g => g.ToList()).ToList();
            return new LinkGroupingResult(groups.Select(g => ToCandidate(g, articleTitle)).ToList(), IsAmbiguous: false, AmbiguityReason: null);
        }

        var named = links
            .Where(l => l.FileName is not null)
            .GroupBy(l => NormalizeFileName(l.FileName!), StringComparer.Ordinal)
            .Select(g => g.ToList())
            .ToList();
        var unnamed = links.Where(l => l.FileName is null).ToList();

        if (named.Count == 0)
        {
            return HasDistinctHosts(unnamed)
                ? new LinkGroupingResult([ToCandidate(unnamed, articleTitle)], IsAmbiguous: false, AmbiguityReason: null)
                : Ambiguous(unnamed.Select(l => ToCandidate([l], articleTitle)).ToList(),
                    "Ambiguous grouping: several links without a file name on the same host.");
        }

        if (unnamed.Count == 0)
        {
            return new LinkGroupingResult(named.Select(g => ToCandidate(g, articleTitle)).ToList(), IsAmbiguous: false, AmbiguityReason: null);
        }

        // One named file plus unnamed links on other hosts: the unnamed links are most likely its mirrors.
        if (named.Count == 1 && HasDistinctHosts([.. named[0], .. unnamed]))
        {
            return new LinkGroupingResult([ToCandidate([.. named[0], .. unnamed], articleTitle)], IsAmbiguous: false, AmbiguityReason: null);
        }

        var candidates = named.Select(g => ToCandidate(g, articleTitle))
            .Concat(unnamed.Select(l => ToCandidate([l], articleTitle)))
            .ToList();
        return Ambiguous(candidates, "Ambiguous grouping: named and unnamed links mixed.");
    }

    private static LinkGroupingResult Ambiguous(IReadOnlyList<DownloadCandidate> candidates, string reason) =>
        new(candidates, IsAmbiguous: true, AmbiguityReason: reason);

    private static bool HasDistinctHosts(List<ExtractedLink> links) =>
        links.Select(l => l.Host).Distinct(StringComparer.OrdinalIgnoreCase).Count() == links.Count;

    private static DownloadCandidate ToCandidate(List<ExtractedLink> links, string articleTitle)
    {
        var fileName = links.Select(l => l.FileName).FirstOrDefault(f => f is not null);
        var unnamedLabel = links.Count == 1 ? $"{articleTitle} ({links[0].Host})" : articleTitle;
        var label = fileName is not null ? Path.GetFileNameWithoutExtension(fileName) : unnamedLabel;
        var mirrors = links
            .DistinctBy(l => l.Url, StringComparer.Ordinal)
            .Select(l => new DownloadMirror(l.Url, l.Host))
            .ToList();
        return new DownloadCandidate(label, fileName, links.Select(l => l.SizeBytes).FirstOrDefault(s => s.HasValue), mirrors);
    }

    internal static string NormalizeFileName(string fileName) =>
        new(Path.GetFileNameWithoutExtension(fileName).RemoveDiacritics().ToUpperInvariant().Where(char.IsLetterOrDigit).ToArray());
}
