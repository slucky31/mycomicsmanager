using Application.FeedImports.Analysis;

namespace Application.UnitTests.FeedImports;

public class DownloadLinkGrouperTests
{
    private const string ArticleTitle = "Blacksad - Tome 3";

    private static ExtractedLink Link(string url, string host, string? fileName = null, long? size = null, string? groupKey = null) =>
        new(url, host, host, fileName, size, groupKey);

    [Fact]
    public void Group_Should_ReturnOneCandidateWithMirrors_WhenUnnamedLinksAreOnDistinctHosts()
    {
        var result = DownloadLinkGrouper.Group(
            [Link("https://1fichier.com/?a", "1fichier.com", size: 100), Link("https://rapidgator.net/file/b", "rapidgator.net")],
            ArticleTitle);

        result.IsAmbiguous.Should().BeFalse();
        var candidate = result.Candidates.Should().ContainSingle().Subject;
        candidate.Label.Should().Be(ArticleTitle);
        candidate.SizeBytes.Should().Be(100);
        candidate.Mirrors.Select(m => m.Host).Should().Equal("1fichier.com", "rapidgator.net");
    }

    [Fact]
    public void Group_Should_BeAmbiguous_WhenUnnamedLinksShareAHost()
    {
        var result = DownloadLinkGrouper.Group(
            [Link("https://1fichier.com/?a", "1fichier.com"), Link("https://1fichier.com/?b", "1fichier.com")],
            ArticleTitle);

        result.IsAmbiguous.Should().BeTrue();
        result.AmbiguityReason.Should().Contain("même hébergeur");
        result.Candidates.Should().HaveCount(2);
    }

    [Fact]
    public void Group_Should_ReturnOneCandidatePerFile_WhenLinksAreNamed()
    {
        var result = DownloadLinkGrouper.Group(
            [
                Link("https://1fichier.com/?a", "1fichier.com", "Blacksad_T01.cbz"),
                Link("https://rapidgator.net/file/a/Blacksad_T01.cbz.html", "rapidgator.net", "Blacksad T01.cbz"),
                Link("https://1fichier.com/?b", "1fichier.com", "Blacksad_T02.cbz")
            ],
            "Blacksad - Tomes 1 à 2");

        result.IsAmbiguous.Should().BeFalse();
        result.Candidates.Should().HaveCount(2);
        result.Candidates[0].Label.Should().Be("Blacksad_T01");
        result.Candidates[0].Mirrors.Should().HaveCount(2);
        result.Candidates[1].FileName.Should().Be("Blacksad_T02.cbz");
    }

    [Fact]
    public void Group_Should_AttachUnnamedMirrors_WhenSingleNamedFileAndHostsAreDistinct()
    {
        var result = DownloadLinkGrouper.Group(
            [Link("https://rapidgator.net/file/a/Blacksad_T03.cbz.html", "rapidgator.net", "Blacksad_T03.cbz"), Link("https://1fichier.com/?a", "1fichier.com")],
            ArticleTitle);

        result.IsAmbiguous.Should().BeFalse();
        result.Candidates.Should().ContainSingle().Which.Mirrors.Should().HaveCount(2);
    }

    [Fact]
    public void Group_Should_BeAmbiguous_WhenNamedAndUnnamedLinksCannotBePaired()
    {
        var result = DownloadLinkGrouper.Group(
            [
                Link("https://1fichier.com/?a", "1fichier.com", "Blacksad_T01.cbz"),
                Link("https://1fichier.com/?b", "1fichier.com", "Blacksad_T02.cbz"),
                Link("https://rapidgator.net/file/c", "rapidgator.net")
            ],
            ArticleTitle);

        result.IsAmbiguous.Should().BeTrue();
        result.Candidates.Should().HaveCount(3);
    }

    [Fact]
    public void Group_Should_UseGroupKeys_WhenSiteExtractorProvidesThem()
    {
        var result = DownloadLinkGrouper.Group(
            [
                Link("https://1fichier.com/?a", "1fichier.com", groupKey: "T1"),
                Link("https://1fichier.com/?b", "1fichier.com", groupKey: "T2"),
                Link("https://rapidgator.net/file/a", "rapidgator.net", groupKey: "T1")
            ],
            ArticleTitle);

        result.IsAmbiguous.Should().BeFalse();
        result.Candidates.Should().HaveCount(2);
        result.Candidates[0].Mirrors.Should().HaveCount(2);
    }

    [Fact]
    public void Group_Should_ReturnNoCandidate_WhenNoLink()
    {
        DownloadLinkGrouper.Group([], ArticleTitle).Candidates.Should().BeEmpty();
    }
}
