using System.Net;
using System.Text;
using Application.ComicInfoSearch;
using Application.Interfaces;
using Microsoft.Extensions.Options;

namespace Application.UnitTests.ComicInfoSearch;

public sealed class BnfCatalogueServiceTests : IDisposable
{
    private readonly List<IDisposable> _disposables = [];

    private const string Isbn = "220330507X";

    // The answer of the BnF catalogue for 220330507X, trimmed; &#x98; and &#x9C; frame the article to skip when sorting.
    private const string AdeleResponse = """
        <?xml version="1.0" encoding="UTF-8"?><srw:searchRetrieveResponse xmlns:srw="http://www.loc.gov/zing/srw/">
        <srw:numberOfRecords>1</srw:numberOfRecords>
        <srw:records><srw:record><srw:recordData>
        <mxc:record xmlns:mxc="info:lc/xmlns/marcxchange-v2" format="UNIMARC" id="ark:/12148/cb34835486s" type="Bibliographic">
          <mxc:controlfield tag="001">FRBNF348354860000002</mxc:controlfield>
          <mxc:datafield tag="010" ind1=" " ind2=" "><mxc:subfield code="a">2-203-30507-X</mxc:subfield></mxc:datafield>
          <mxc:datafield tag="200" ind1="1" ind2=" ">
            <mxc:subfield code="a">&#x98;Le &#x9C;Noyé à deux têtes</mxc:subfield>
            <mxc:subfield code="b">Texte imprimé</mxc:subfield>
            <mxc:subfield code="f">texte et dessin, Jacques Tardi</mxc:subfield>
          </mxc:datafield>
          <mxc:datafield tag="210" ind1=" " ind2=" ">
            <mxc:subfield code="a">Paris</mxc:subfield><mxc:subfield code="c">Casterman</mxc:subfield><mxc:subfield code="d">1985</mxc:subfield>
          </mxc:datafield>
          <mxc:datafield tag="215" ind1=" " ind2=" "><mxc:subfield code="a">47 p.</mxc:subfield><mxc:subfield code="d">30 cm</mxc:subfield></mxc:datafield>
          <mxc:datafield tag="225" ind1="1" ind2="9">
            <mxc:subfield code="a">Les Aventures extraordinaires d'Adèle Blanc-Sec (collection)</mxc:subfield><mxc:subfield code="v">[6]</mxc:subfield>
          </mxc:datafield>
          <mxc:datafield tag="461" ind1=" " ind2="0">
            <mxc:subfield code="0">34306096</mxc:subfield>
            <mxc:subfield code="t">Les Aventures extraordinaires d'Adèle Blanc-Sec</mxc:subfield><mxc:subfield code="v">[6]</mxc:subfield>
          </mxc:datafield>
          <mxc:datafield tag="700" ind1=" " ind2="|">
            <mxc:subfield code="a">Tardi</mxc:subfield><mxc:subfield code="b">Jacques</mxc:subfield><mxc:subfield code="4">440</mxc:subfield>
          </mxc:datafield>
        </mxc:record>
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

    private (BnfCatalogueService Service, StubHandler Handler) CreateService(Func<HttpRequestMessage, HttpResponseMessage> respond, bool enabled = true)
    {
        var handler = new StubHandler(respond);
        var client = new HttpClient(handler);
        _disposables.Add(client);
        _disposables.Add(handler);
        var service = new BnfCatalogueService(
            client,
            Options.Create(new BnfCatalogueSettings { BaseUrl = new Uri("https://catalogue.bnf.fr") }),
            FeatureTogglesStub.All(enabled),
            NullLogger<BnfCatalogueService>.Instance);
        return (service, handler);
    }

    private static HttpResponseMessage Xml(string body) =>
        new(HttpStatusCode.OK) { Content = new StringContent(body, Encoding.UTF8, "application/xml") };

    // A record of the BnF catalogue holding the given UNIMARC fields.
    private static string Record(string fields) => $"""
        <?xml version="1.0" encoding="UTF-8"?><srw:searchRetrieveResponse xmlns:srw="http://www.loc.gov/zing/srw/">
        <srw:records><srw:record><srw:recordData>
        <mxc:record xmlns:mxc="info:lc/xmlns/marcxchange-v2" format="UNIMARC">{fields}</mxc:record>
        </srw:recordData></srw:record></srw:records>
        </srw:searchRetrieveResponse>
        """;

    private static string Field(string tag, params (string Code, string Value)[] subfields) =>
        $"<mxc:datafield tag=\"{tag}\">{string.Concat(subfields.Select(s => $"<mxc:subfield code=\"{s.Code}\">{s.Value}</mxc:subfield>"))}</mxc:datafield>";

    [Fact]
    public async Task SearchByIsbnAsync_Should_ReadTheSeriesAndTheVolume_WhenTheBnfKnowsTheBook()
    {
        var (service, handler) = CreateService(_ => Xml(AdeleResponse));

        var result = await service.SearchByIsbnAsync("2-203-30507-X", TestContext.Current.CancellationToken);

        result.Should().BeEquivalentTo(new BnfBookResult(
            "Le Noyé à deux têtes", null, ["Jacques Tardi"], ["Casterman"], new DateOnly(1985, 1, 1), 47, null, Found: true,
            Serie: "Les Aventures extraordinaires d'Adèle Blanc-Sec", VolumeNumber: 6));
        handler.LastRequest!.RequestUri!.AbsoluteUri.Should().Contain("bib.fuzzyISBN%20all%20%22220330507X%22")
            .And.Contain("recordSchema=unimarcXchange");
    }

    [Fact]
    public async Task SearchByIsbnAsync_Should_ReadTheSeriesStatement_WhenTheRecordLinksNoSeries()
    {
        var (service, _) = CreateService(_ => Xml(Record(
            Field("200", ("a", "Tangente"), ("e", "roman graphique")) +
            Field("214", ("c", "des Ronds dans l'O"), ("d", "DL 2012")) +
            Field("215", ("a", "1 vol. (82 p.)")) +
            Field("225", ("a", "Ronds dans l'O jeunesse")) +
            Field("701", ("a", "Wagner"), ("b", "Céline")) +
            Field("702", ("a", "Peyo")) +
            Field("710", ("a", "Studio Peyo")))));

        var result = await service.SearchByIsbnAsync(Isbn, TestContext.Current.CancellationToken);

        result.Should().BeEquivalentTo(new BnfBookResult(
            "Tangente", "roman graphique", ["Céline Wagner", "Peyo", "Studio Peyo"], ["des Ronds dans l'O"], new DateOnly(2012, 1, 1), 82, null, Found: true,
            Serie: "Ronds dans l'O jeunesse", VolumeNumber: null));
    }

    [Fact]
    public async Task SearchByIsbnAsync_Should_ReadTheVolumeFromTheTitle_WhenTheTitleNamesThePart()
    {
        var (service, _) = CreateService(_ => Xml(Record(
            Field("200", ("a", "Les Schtroumpfs"), ("h", "12"), ("i", "Le schtroumpfissime")))));

        var result = await service.SearchByIsbnAsync(Isbn, TestContext.Current.CancellationToken);

        result.Title.Should().Be("Le schtroumpfissime");
        result.Serie.Should().Be("Les Schtroumpfs");
        result.VolumeNumber.Should().Be(12);
        result.Authors.Should().BeEmpty();
        result.PublishDate.Should().BeNull();
        result.NumberOfPages.Should().BeNull();
    }

    [Fact]
    public async Task SearchByIsbnAsync_Should_ReturnNotFound_WhenTheRecordHasNoTitle()
    {
        var (service, _) = CreateService(_ => Xml(Record(Field("210", ("c", "Casterman")))));

        var result = await service.SearchByIsbnAsync(Isbn, TestContext.Current.CancellationToken);

        result.Found.Should().BeFalse();
        result.Serie.Should().BeNull();
    }

    [Fact]
    public async Task SearchByIsbnAsync_Should_SearchNothing_WhenTheBnfIsTurnedOff()
    {
        var (service, handler) = CreateService(_ => Xml(NoRecordResponse), enabled: false);

        var result = await service.SearchByIsbnAsync("9782917237359", TestContext.Current.CancellationToken);

        service.IsEnabled.Should().BeFalse();
        result.Found.Should().BeFalse();
        handler.LastRequest.Should().BeNull();
    }

    [Fact]
    public async Task SearchByIsbnAsync_Should_ReturnNotFound_WhenTheBnfHasNoRecord()
    {
        var (service, _) = CreateService(_ => Xml(NoRecordResponse));

        var result = await service.SearchByIsbnAsync(Isbn, TestContext.Current.CancellationToken);

        result.Found.Should().BeFalse();
        result.Failed.Should().BeFalse();
    }

    [Fact]
    public async Task SearchByIsbnAsync_Should_ReturnFailed_WhenTheBnfAnswersWithAnError()
    {
        var (service, _) = CreateService(_ => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));

        var result = await service.SearchByIsbnAsync(Isbn, TestContext.Current.CancellationToken);

        result.Found.Should().BeFalse();
        result.Failed.Should().BeTrue();
    }

    [Fact]
    public async Task SearchByIsbnAsync_Should_ReturnFailed_WhenTheAnswerIsNotXml()
    {
        var (service, _) = CreateService(_ => Xml("<html><body>maintenance"));

        var result = await service.SearchByIsbnAsync(Isbn, TestContext.Current.CancellationToken);

        result.Found.Should().BeFalse();
        result.Failed.Should().BeTrue();
    }

    [Fact]
    public async Task SearchByIsbnAsync_Should_ReturnFailed_WhenTheRequestFails()
    {
        var (service, _) = CreateService(_ => throw new HttpRequestException("Network unreachable"));

        var result = await service.SearchByIsbnAsync(Isbn, TestContext.Current.CancellationToken);

        result.Found.Should().BeFalse();
        result.Failed.Should().BeTrue();
    }

    [Fact]
    public async Task SearchByIsbnAsync_Should_ReturnFailed_WhenTheRequestTimesOut()
    {
        var (service, _) = CreateService(_ => throw new TaskCanceledException("Timeout"));

        var result = await service.SearchByIsbnAsync(Isbn, TestContext.Current.CancellationToken);

        result.Found.Should().BeFalse();
        result.Failed.Should().BeTrue();
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
