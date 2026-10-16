using System.Net;
using System.Text;
using Application.ComicInfoSearch;
using Microsoft.Extensions.Options;

namespace Application.UnitTests.ComicInfoSearch;

// Fixtures are real Algolia answers and album pages of bubblebd.com (scripts and images removed), saved in ComicInfoSearch/Fixtures.
public sealed class BubbleServiceTests
{
    private const string Isbn = "9791026820369";
    private const string AlgoliaUrl = "https://6a891z72zd-dsn.algolia.net/1/indexes/Albums/query";
    private const string AlbumUrl = "https://www.bubblebd.com/catwoman-selina-kyle-tome-3-entre-deux-vies/album/i2DEhZjUkw/o3UJrrCMOB";

    private static string Fixture(string name) =>
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "ComicInfoSearch", "Fixtures", name));

    private static readonly string[] UrbanComics = ["Urban Comics"];

    private static BubbleService CreateService(HttpClient client, string apiKey = "search-key", bool enabled = true) =>
        new(client, Options.Create(new BubbleSettings
        {
            BaseUrl = new Uri("https://www.bubblebd.com"),
            AlgoliaBaseUrl = new Uri("https://6a891z72zd-dsn.algolia.net"),
            ApplicationId = "6A891Z72ZD",
            ApiKey = apiKey,
        }), FeatureTogglesStub.All(enabled), NullLogger<BubbleService>.Instance);

    private static HttpResponseMessage Json(string json) =>
        new(HttpStatusCode.OK) { Content = new StringContent(json, Encoding.UTF8, "application/json") };

    private static HttpResponseMessage Html(string html) =>
        new(HttpStatusCode.OK) { Content = new StringContent(html, Encoding.UTF8, "text/html") };

    // The album of the tome 1 of the series, whose page lists its authors.
    private const string Tome1Hit = """
        {"hits":[{"objectId":"7w2yaSAyke","permalink":"catwoman-selina-kyle-tome-1-pales-copies","tome":1,"title":"Pâles copies",
        "eans":"9791026816263","imageUrl":"https://assets.bubblebd.com/img/deezclcnp2/sg2ogbtfrb.jpg",
        "serieTitle":"Catwoman - Selina Kyle","defaultSellingPrintObjectId":"ZbNBfF0t9n"}]}
        """;

    [Fact]
    public async Task SearchByIsbnAsync_Should_ReturnTheAlbumWithThePageDetails_WhenBubbleKnowsTheIsbn()
    {
        using var handler = new FakeHandler(request => request.RequestUri!.ToString() switch
        {
            AlgoliaUrl => Json(Fixture($"bubble-algolia-{Isbn}.json")),
            AlbumUrl => Html(Fixture("bubble-album-i2DEhZjUkw.html")),
            _ => new HttpResponseMessage(HttpStatusCode.NotFound),
        });
        using var client = new HttpClient(handler);

        var result = await CreateService(client).SearchByIsbnAsync("979-10-268-2036-9", TestContext.Current.CancellationToken);

        result.Should().BeEquivalentTo(new
        {
            Title = "Entre deux vies",
            Serie = "Catwoman - Selina Kyle",
            VolumeNumber = 3,
            Authors = Array.Empty<string>(),
            Publishers = UrbanComics,
            PublishDate = new DateOnly(2021, 5, 7),
            NumberOfPages = 200,
            CoverUrl = new Uri("https://assets.bubblebd.com/img/1jvlcxhupm/qhnc4tjxcz.jpg"),
            Found = true,
            Failed = false,
        });
        var (method, headers, body) = handler.Requests[0];
        method.Should().Be(HttpMethod.Post);
        headers.GetValues("X-Algolia-Application-Id").Should().Equal("6A891Z72ZD");
        headers.GetValues("X-Algolia-API-Key").Should().Equal("search-key");
        body.Should().Contain($"\"query\":\"{Isbn}\"");
    }

    [Fact]
    public async Task SearchByIsbnAsync_Should_ReadTheAuthorsAndTheReleaseDate_WhenTheAlbumPageListsThem()
    {
        using var handler = new FakeHandler(request => request.RequestUri!.ToString() switch
        {
            AlgoliaUrl => Json(Tome1Hit),
            "https://www.bubblebd.com/catwoman-selina-kyle-tome-1-pales-copies/album/7w2yaSAyke/ZbNBfF0t9n" => Html(Fixture("bubble-album-7w2yaSAyke.html")),
            _ => new HttpResponseMessage(HttpStatusCode.NotFound),
        });
        using var client = new HttpClient(handler);

        var result = await CreateService(client).SearchByIsbnAsync("9791026816263", TestContext.Current.CancellationToken);

        result.Authors.Should().Equal("Joëlle Jones", "Fernando Blanco");
        result.PublishDate.Should().Be(new DateOnly(2019, 6, 14));
        result.NumberOfPages.Should().Be(152);
        result.VolumeNumber.Should().Be(1);
    }

    [Fact]
    public async Task SearchByIsbnAsync_Should_ReturnNotFound_WhenBubbleHasNoAlbum()
    {
        using var handler = new FakeHandler(_ => Json(Fixture("bubble-algolia-9782756036670.json")));
        using var client = new HttpClient(handler);

        var result = await CreateService(client).SearchByIsbnAsync("9782756036670", TestContext.Current.CancellationToken);

        result.Found.Should().BeFalse();
        result.Failed.Should().BeFalse();
    }

    [Fact]
    public async Task SearchByIsbnAsync_Should_ReturnNotFound_WhenNoAlbumHasTheIsbnAmongItsEans()
    {
        using var handler = new FakeHandler(_ => Json(Tome1Hit));
        using var client = new HttpClient(handler);

        var result = await CreateService(client).SearchByIsbnAsync(Isbn, TestContext.Current.CancellationToken);

        result.Found.Should().BeFalse();
        handler.Requests.Should().ContainSingle();
    }

    [Fact]
    public async Task SearchByIsbnAsync_Should_SearchNothing_WhenNoApiKeyIsConfigured()
    {
        using var handler = new FakeHandler(_ => Json(Tome1Hit));
        using var client = new HttpClient(handler);
        var service = CreateService(client, apiKey: "");

        var result = await service.SearchByIsbnAsync(Isbn, TestContext.Current.CancellationToken);

        service.IsEnabled.Should().BeFalse();
        result.Found.Should().BeFalse();
        handler.Requests.Should().BeEmpty();
    }

    [Fact]
    public async Task SearchByIsbnAsync_Should_SearchNothing_WhenBubbleIsTurnedOff()
    {
        using var handler = new FakeHandler(_ => Json(Tome1Hit));
        using var client = new HttpClient(handler);
        var service = CreateService(client, enabled: false);

        var result = await service.SearchByIsbnAsync(Isbn, TestContext.Current.CancellationToken);

        service.IsEnabled.Should().BeFalse();
        result.Found.Should().BeFalse();
        handler.Requests.Should().BeEmpty();
    }

    [Theory]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.Forbidden)]
    public async Task SearchByIsbnAsync_Should_KeepTheSearchResult_WhenTheAlbumPageCannotBeRead(HttpStatusCode pageStatus)
    {
        using var handler = new FakeHandler(request => request.Method == HttpMethod.Post
            ? Json(Fixture($"bubble-algolia-{Isbn}.json"))
            : new HttpResponseMessage(pageStatus));
        using var client = new HttpClient(handler);

        var result = await CreateService(client).SearchByIsbnAsync(Isbn, TestContext.Current.CancellationToken);

        result.Found.Should().BeTrue();
        result.Serie.Should().Be("Catwoman - Selina Kyle");
        result.Publishers.Should().BeEmpty();
        result.CoverUrl.Should().Be(new Uri("https://assets.bubblebd.com/img/1jvlcxhupm/pbogxoyjrv.jpg"));
    }

    [Fact]
    public async Task SearchByIsbnAsync_Should_KeepTheSearchResult_WhenTheAlbumPageRequestFails()
    {
        using var handler = new FakeHandler(request => request.Method == HttpMethod.Post
            ? Json(Fixture($"bubble-algolia-{Isbn}.json"))
            : throw new HttpRequestException("Connection reset"));
        using var client = new HttpClient(handler);

        var result = await CreateService(client).SearchByIsbnAsync(Isbn, TestContext.Current.CancellationToken);

        result.Found.Should().BeTrue();
        result.Failed.Should().BeFalse();
    }

    [Fact]
    public async Task SearchByIsbnAsync_Should_KeepTheSearchResult_WhenTheAlbumPageTimesOut()
    {
        using var handler = new FakeHandler(request => request.Method == HttpMethod.Post
            ? Json(Fixture($"bubble-algolia-{Isbn}.json"))
            : throw new TaskCanceledException("Timeout"));
        using var client = new HttpClient(handler);

        var result = await CreateService(client).SearchByIsbnAsync(Isbn, TestContext.Current.CancellationToken);

        result.Found.Should().BeTrue();
    }

    [Fact]
    public async Task SearchByIsbnAsync_Should_ReadThePublisherFromTheCharacteristics_WhenTheSchemaDataIsUnreadable()
    {
        const string page = """
            <html><head><script type="application/ld+json">{ not json</script></head><body>
            <div><div>Editeur</div><div>Urban Comics</div></div>
            <div><div>Date de parution</div><div>1er août 2020</div></div>
            </body></html>
            """;
        using var handler = new FakeHandler(request => request.Method == HttpMethod.Post
            ? Json(Fixture($"bubble-algolia-{Isbn}.json"))
            : Html(page));
        using var client = new HttpClient(handler);

        var result = await CreateService(client).SearchByIsbnAsync(Isbn, TestContext.Current.CancellationToken);

        result.Publishers.Should().Equal("Urban Comics");
        result.PublishDate.Should().Be(new DateOnly(2020, 8, 1));
        result.NumberOfPages.Should().BeNull();
    }

    [Fact]
    public async Task SearchByIsbnAsync_Should_NotFetchThePage_WhenThePermalinkIsNotAPathSegment()
    {
        const string hit = """
            {"hits":[{"objectId":"i2DEhZjUkw","permalink":"../admin","tome":3,"title":"Entre deux vies","eans":"9791026820369","serieTitle":"Catwoman - Selina Kyle"}]}
            """;
        using var handler = new FakeHandler(_ => Json(hit));
        using var client = new HttpClient(handler);

        var result = await CreateService(client).SearchByIsbnAsync(Isbn, TestContext.Current.CancellationToken);

        result.Found.Should().BeTrue();
        result.CoverUrl.Should().BeNull();
        handler.Requests.Should().ContainSingle();
    }

    [Fact]
    public async Task SearchByIsbnAsync_Should_ReturnFailed_WhenTheSearchIsRefused()
    {
        using var handler = new FakeHandler(_ => new HttpResponseMessage(HttpStatusCode.Forbidden));
        using var client = new HttpClient(handler);

        var result = await CreateService(client).SearchByIsbnAsync(Isbn, TestContext.Current.CancellationToken);

        result.Found.Should().BeFalse();
        result.Failed.Should().BeTrue();
    }

    [Fact]
    public async Task SearchByIsbnAsync_Should_ReturnFailed_WhenTheSearchAnswerIsNotJson()
    {
        using var handler = new FakeHandler(_ => Json("<html>maintenance</html>"));
        using var client = new HttpClient(handler);

        var result = await CreateService(client).SearchByIsbnAsync(Isbn, TestContext.Current.CancellationToken);

        result.Failed.Should().BeTrue();
    }

    [Fact]
    public async Task SearchByIsbnAsync_Should_ReturnFailed_WhenTheSearchRequestFails()
    {
        using var handler = new FakeHandler(_ => throw new HttpRequestException("DNS failure"));
        using var client = new HttpClient(handler);

        var result = await CreateService(client).SearchByIsbnAsync(Isbn, TestContext.Current.CancellationToken);

        result.Failed.Should().BeTrue();
    }

    [Fact]
    public async Task SearchByIsbnAsync_Should_ReturnFailed_WhenTheSearchTimesOut()
    {
        using var handler = new FakeHandler(_ => throw new TaskCanceledException("Timeout"));
        using var client = new HttpClient(handler);

        var result = await CreateService(client).SearchByIsbnAsync(Isbn, TestContext.Current.CancellationToken);

        result.Failed.Should().BeTrue();
    }

    [Theory]
    [InlineData("07 mai 2021", 2021, 5, 7)]
    [InlineData("1er juin 2019", 2019, 6, 1)]
    [InlineData("Décembre 2020", 2020, 12, 1)]
    [InlineData("2021", 2021, 1, 1)]
    public void ParseReleaseDate_Should_ReadTheFrenchDate(string value, int year, int month, int day)
    {
        BubbleService.ParseReleaseDate(value).Should().Be(new DateOnly(year, month, day));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("à paraître")]
    [InlineData("12 brumaire 2021")]
    [InlineData("31 février 2021")]
    public void ParseReleaseDate_Should_ReturnNull_WhenTheValueIsNotADate(string? value)
    {
        BubbleService.ParseReleaseDate(value).Should().BeNull();
    }

    private sealed class FakeHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        public List<(HttpMethod Method, System.Net.Http.Headers.HttpRequestHeaders Headers, string Body)> Requests { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var body = request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken);
            Requests.Add((request.Method, request.Headers, body));
            return respond(request);
        }
    }
}
