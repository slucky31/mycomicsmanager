using System.Net;
using System.Text;
using Application.ComicInfoSearch;
using Application.Interfaces;
using Microsoft.Extensions.Options;

namespace Application.UnitTests.ComicInfoSearch;

public sealed class BnfCatalogueServiceTests : IDisposable
{
    private readonly List<IDisposable> _disposables = [];

    private const string Isbn = "9782917237359";

    // The answer of the BnF catalogue for 9782917237359.
    private const string TangenteResponse = """
        <?xml version="1.0" encoding="UTF-8"?><srw:searchRetrieveResponse xmlns:srw="http://www.loc.gov/zing/srw/">
        <srw:numberOfRecords>1</srw:numberOfRecords>
        <srw:records><srw:record><srw:recordData>
        <oai_dc:dc xmlns:oai_dc="http://www.openarchives.org/OAI/2.0/oai_dc/" xmlns:dc="http://purl.org/dc/elements/1.1/">
          <dc:identifier>http://catalogue.bnf.fr/ark:/12148/cb427384865</dc:identifier>
          <dc:title>Tangente / Céline Wagner</dc:title>
          <dc:creator>Wagner, Céline (1975-....). Auteur du texte</dc:creator>
          <dc:publisher>des Ronds dans l'O (Vincennes)</dc:publisher>
          <dc:date>2012</dc:date>
          <dc:identifier>ISBN 9782917237359</dc:identifier>
          <dc:format>1 vol. (82 p.) : ill., couv. ill. ; 24 cm</dc:format>
          <dc:language>fre</dc:language>
        </oai_dc:dc>
        </srw:recordData></srw:record></srw:records>
        </srw:searchRetrieveResponse>
        """;

    private const string NoRecordResponse = """
        <?xml version="1.0" encoding="UTF-8"?><srw:searchRetrieveResponse xmlns:srw="http://www.loc.gov/zing/srw/">
        <srw:numberOfRecords>0</srw:numberOfRecords></srw:searchRetrieveResponse>
        """;

    public void Dispose()
    {
        foreach (var disposable in _disposables)
        {
            disposable.Dispose();
        }
    }

    private (BnfCatalogueService Service, StubHandler Handler) CreateService(Func<HttpRequestMessage, HttpResponseMessage> respond)
    {
        var handler = new StubHandler(respond);
        var client = new HttpClient(handler);
        _disposables.Add(client);
        _disposables.Add(handler);
        var service = new BnfCatalogueService(
            client,
            Options.Create(new BnfCatalogueSettings { BaseUrl = new Uri("https://catalogue.bnf.fr") }),
            NullLogger<BnfCatalogueService>.Instance);
        return (service, handler);
    }

    private static HttpResponseMessage Xml(string body) =>
        new(HttpStatusCode.OK) { Content = new StringContent(body, Encoding.UTF8, "application/xml") };

    [Fact]
    public async Task SearchByIsbnAsync_Should_ReadTheDublinCoreRecord_WhenTheBnfKnowsTheBook()
    {
        var (service, handler) = CreateService(_ => Xml(TangenteResponse));

        var result = await service.SearchByIsbnAsync("978-2-917237-35-9", TestContext.Current.CancellationToken);

        result.Should().BeEquivalentTo(new BnfBookResult(
            "Tangente", null, ["Céline Wagner"], ["des Ronds dans l'O"], new DateOnly(2012, 1, 1), 82, null, Found: true));
        handler.LastRequest!.RequestUri!.AbsoluteUri.Should().Contain("bib.fuzzyISBN%20all%20%229782917237359%22")
            .And.Contain("recordSchema=dublincore");
    }

    [Fact]
    public async Task SearchByIsbnAsync_Should_ReturnNotFound_WhenTheBnfHasNoRecord()
    {
        var (service, _) = CreateService(_ => Xml(NoRecordResponse));

        (await service.SearchByIsbnAsync(Isbn, TestContext.Current.CancellationToken)).Found.Should().BeFalse();
    }

    [Fact]
    public async Task SearchByIsbnAsync_Should_ReturnNotFound_WhenTheBnfAnswersWithAnError()
    {
        var (service, _) = CreateService(_ => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));

        (await service.SearchByIsbnAsync(Isbn, TestContext.Current.CancellationToken)).Found.Should().BeFalse();
    }

    [Fact]
    public async Task SearchByIsbnAsync_Should_ReturnNotFound_WhenTheAnswerIsNotXml()
    {
        var (service, _) = CreateService(_ => Xml("<html><body>maintenance"));

        (await service.SearchByIsbnAsync(Isbn, TestContext.Current.CancellationToken)).Found.Should().BeFalse();
    }

    [Fact]
    public async Task SearchByIsbnAsync_Should_ReturnNotFound_WhenTheRequestFails()
    {
        var (service, _) = CreateService(_ => throw new HttpRequestException("Network unreachable"));

        (await service.SearchByIsbnAsync(Isbn, TestContext.Current.CancellationToken)).Found.Should().BeFalse();
    }

    [Fact]
    public async Task SearchByIsbnAsync_Should_ReturnNotFound_WhenTheRequestTimesOut()
    {
        var (service, _) = CreateService(_ => throw new TaskCanceledException("Timeout"));

        (await service.SearchByIsbnAsync(Isbn, TestContext.Current.CancellationToken)).Found.Should().BeFalse();
    }

    [Theory]
    [InlineData("Tangente / Céline Wagner", "Tangente", null)]
    [InlineData("Les Schtroumpfs. 12, Le schtroumpfissime : une histoire / Peyo", "Les Schtroumpfs. 12, Le schtroumpfissime", "une histoire")]
    [InlineData("Sans auteur", "Sans auteur", null)]
    public void ParseTitle_Should_DropTheStatementOfResponsibility(string value, string title, string? subtitle)
    {
        BnfCatalogueService.ParseTitle(value).Should().Be((title, subtitle));
    }

    [Theory]
    [InlineData("Wagner, Céline (1975-....). Auteur du texte", "Céline Wagner")]
    [InlineData("Dupont, Jean. Illustrateur", "Jean Dupont")]
    [InlineData("Peyo (1928-1992). Auteur du texte", "Peyo")]
    [InlineData("Studio Peyo. Illustrateur", "Studio Peyo")]
    [InlineData("Collectif", "Collectif")]
    public void ToPersonName_Should_PutTheGivenNameFirst(string value, string expected)
    {
        BnfCatalogueService.ToPersonName(value).Should().Be(expected);
    }

    [Theory]
    [InlineData("des Ronds dans l'O (Vincennes)", "des Ronds dans l'O")]
    [InlineData("Dupuis", "Dupuis")]
    public void StripTrailingPlace_Should_DropThePlaceOfPublication(string value, string expected)
    {
        BnfCatalogueService.StripTrailingPlace(value).Should().Be(expected);
    }

    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        public HttpRequestMessage? LastRequest { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            return Task.FromResult(respond(request));
        }
    }
}
