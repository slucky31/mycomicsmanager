using Application.FeedImports.Analysis;
using Application.FeedImports.Analysis.Sites;

namespace Application.UnitTests.FeedImports;

// Fixtures are real article pages (scripts and styles removed), saved in FeedImports/Fixtures.
public class DleArticleLinkExtractorTests
{
    private static readonly string[] s_hosters =
        ["fileq.net", "dailyuploads.net", "frdl.io", "katfile.biz", "trbt.cc", "rapidgator.net"];

    private static string Fixture(string name) =>
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "FeedImports", "Fixtures", name));

    private static ArticleExtraction ExtractPlaneteBd(string fixture, IReadOnlyList<string>? hosts = null) =>
        new PlaneteBdLinkExtractor(NullLogger<PlaneteBdLinkExtractor>.Instance).Extract(Fixture(fixture), new Uri("https://planete-bd.org/bd/1.html"), hosts ?? s_hosters);

    private static ArticleExtraction ExtractZoneEbook(string fixture) =>
        new ZoneEbookLinkExtractor(NullLogger<ZoneEbookLinkExtractor>.Instance).Extract(Fixture(fixture), new Uri("https://zone-ebook.com/bd-comics-mangas/1.html"), s_hosters);

    [Theory]
    [InlineData("https://planete-bd.org/bd/1.html", true, false)]
    [InlineData("http://www.planete-bd.org/bd/1.html", true, false)]
    [InlineData("https://zone-ebook.com/bd/1.html", false, true)]
    [InlineData("https://other.example/bd/1.html", false, false)]
    public void CanHandle_Should_MatchOwnSiteOnly(string address, bool planeteBd, bool zoneEbook)
    {
        new PlaneteBdLinkExtractor(NullLogger<PlaneteBdLinkExtractor>.Instance).CanHandle(new Uri(address)).Should().Be(planeteBd);
        new ZoneEbookLinkExtractor(NullLogger<ZoneEbookLinkExtractor>.Instance).CanHandle(new Uri(address)).Should().Be(zoneEbook);
    }

    [Fact]
    public void Extract_Should_ReturnMirrorsOfTheSameFile_WhenSingleBookOnPlaneteBd()
    {
        var extraction = ExtractPlaneteBd("planete-bd_151223-el-borbah-tome-1.html");

        extraction.Links.Select(l => l.Host).Should().Equal("fileq.net", "trbt.cc");
        extraction.Links.Should().AllSatisfy(l => l.FileName.Should().Be("El.Borbah.cbr"));
        extraction.Links[0].SizeBytes.Should().Be((long)(101.5 * 1024 * 1024));
        extraction.Links[1].SizeBytes.Should().BeNull();

        var grouping = DownloadLinkGrouper.Group(extraction.Links, "El Borbah - Tome 1");
        grouping.IsAmbiguous.Should().BeFalse();
        grouping.Candidates.Should().ContainSingle().Which.Mirrors.Should().HaveCount(2);
    }

    [Fact]
    public void Extract_Should_ReturnOneBookPerFile_WhenArticleHoldsSeveralTomes()
    {
        var extraction = ExtractPlaneteBd("planete-bd_149155-metronom-les-5-tomes.html");

        var grouping = DownloadLinkGrouper.Group(extraction.Links, "Metronom' - Les 5 tomes (Re-Up)");

        grouping.IsAmbiguous.Should().BeFalse();
        grouping.Candidates.Select(c => c.FileName).Should().Equal(
            "Metronom.T01.pdf", "Metronom.T03.pdf", "Metronom.T04.pdf", "Metronom.T05.pdf", "Metronom.T02.pdf");
    }

    [Fact]
    public void Extract_Should_IgnoreProfileLinks_WhenUploaderAdvertisesAllHisFiles()
    {
        var extraction = ExtractPlaneteBd("planete-bd_150687-tramp-tome-15.html");

        extraction.Links.Select(l => l.Host).Should().Equal("dailyuploads.net", "frdl.io", "fileq.net");
        DownloadLinkGrouper.Group(extraction.Links, "Tramp - Tome 15").Candidates
            .Should().ContainSingle().Which.Mirrors.Should().HaveCount(3);
    }

    [Fact]
    public void Extract_Should_OnlyReadTheArticleBlock()
    {
        // livomag.com is linked from the sidebar of every page, never from the article itself.
        var hosts = s_hosters.Append("livomag.com").ToArray();

        var siteLinks = ExtractPlaneteBd("planete-bd_151223-el-borbah-tome-1.html", hosts).Links;
        var genericLinks = new GenericDownloadLinkExtractor()
            .Extract(Fixture("planete-bd_151223-el-borbah-tome-1.html"), new Uri("https://planete-bd.org/bd/1.html"), hosts).Links;

        genericLinks.Should().Contain(l => l.Host == "livomag.com");
        siteLinks.Should().NotContain(l => l.Host == "livomag.com");
    }

    [Fact]
    public void Extract_Should_ReturnNoLink_WhenNoAllowedHosterIsLinked()
    {
        ExtractPlaneteBd("planete-bd_151223-el-borbah-tome-1.html", ["1fichier.com"]).Links.Should().BeEmpty();
    }

    [Fact]
    public void Extract_Should_ReturnMirrors_WhenSingleBookOnZoneEbook()
    {
        var extraction = ExtractZoneEbook("zone-ebook_424579-les-inventions-la-con.html");

        extraction.Links.Select(l => l.Host).Should().Equal("dailyuploads.net", "katfile.biz");
        extraction.Isbn.Should().BeNull();
        DownloadLinkGrouper.Group(extraction.Links, "Les inventions à la Con").Candidates
            .Should().ContainSingle().Which.FileName.Should().Be("Les.Invent.Con.cbr");
    }

    [Fact]
    public void Extract_Should_ReadIsbn_WhenArticleStatesIt()
    {
        var extraction = ExtractZoneEbook("zone-ebook_424424-avaler-la-lune-tome-03.html");

        extraction.Isbn.Should().Be("9782203211582");
        extraction.Links.Should().HaveCount(3);
    }

    [Fact]
    public void Extract_Should_ReturnOneBookPerFile_WhenZoneEbookArticleHoldsSeveralTomes()
    {
        var extraction = ExtractZoneEbook("zone-ebook_424544-les-vents-de-la-colere-les-2-tomes.html");

        DownloadLinkGrouper.Group(extraction.Links, "Les vents de la colère - Les 2 tomes").Candidates
            .Select(c => c.FileName).Should().Equal("Les.vents.de.la.colre.T01.cbz", "Les.vents.de.la.colre.T02.cbz");
    }

    [Fact]
    public void Extract_Should_FallBackToWholePage_WhenArticleBlockIsMissing()
    {
        var extraction = new PlaneteBdLinkExtractor(NullLogger<PlaneteBdLinkExtractor>.Instance).Extract(
            "<html><body><a href=\"https://fileq.net/abc.html\">Blacksad.T03.cbz</a></body></html>",
            new Uri("https://planete-bd.org/bd/1.html"), s_hosters);

        extraction.Links.Should().ContainSingle().Which.FileName.Should().Be("Blacksad.T03.cbz");
    }
}
