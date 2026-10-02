using Application.FeedImports.Analysis;

namespace Application.UnitTests.FeedImports;

public class GenericDownloadLinkExtractorTests
{
    private static readonly Uri s_page = new("https://planete-bd.org/blacksad-tome-3");
    private static readonly string[] s_hosts = ["1fichier.com", "rapidgator.net"];

    private readonly GenericDownloadLinkExtractor _extractor = new();

    [Fact]
    public void Extract_Should_KeepOnlyLinksToAllowedHosts_InPageOrder()
    {
        const string html = """
            <html><body>
              <a href="/category/bd">Catégorie</a>
              <a href="https://evil.example/file">Autre</a>
              <a href="javascript:alert(1)">JS</a>
              <p>Blacksad T03 (52,5 Mo) : <a href="https://1fichier.com/?abc123">1fichier</a></p>
              <a href="https://cdn.rapidgator.net/file/xyz/Blacksad_T03.cbz.html">Rapidgator</a>
              <a href="https://1fichier.com/?abc123">doublon</a>
            </body></html>
            """;

        var links = _extractor.Extract(html, s_page, s_hosts);

        links.Should().HaveCount(2);
        links[0].Url.Should().Be("https://1fichier.com/?abc123");
        links[0].Host.Should().Be("1fichier.com");
        links[0].FileName.Should().BeNull();
        links[0].SizeBytes.Should().Be((long)(52.5 * 1024 * 1024));
        links[1].Host.Should().Be("cdn.rapidgator.net");
        links[1].FileName.Should().Be("Blacksad_T03.cbz");
    }

    [Fact]
    public void Extract_Should_ReturnEmpty_WhenNoAllowedHostOrNoAnchor()
    {
        _extractor.Extract("<p>Pas de lien</p>", s_page, s_hosts).Should().BeEmpty();
        _extractor.Extract("<a href=\"https://1fichier.com/?a\">x</a>", s_page, []).Should().BeEmpty();
        _extractor.Extract(string.Empty, s_page, s_hosts).Should().BeEmpty();
    }

    [Fact]
    public void Extract_Should_ReadFileNameFromLinkText_WhenPresent()
    {
        const string html = "<a href=\"https://1fichier.com/?a\">Blacksad - T03 - Âme rouge.cbr</a>";

        var link = _extractor.Extract(html, s_page, s_hosts).Should().ContainSingle().Subject;

        link.FileName.Should().Be("Blacksad - T03 - Âme rouge.cbr");
    }
}
