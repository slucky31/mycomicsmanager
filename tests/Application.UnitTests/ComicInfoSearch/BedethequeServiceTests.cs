using System.Net;
using System.Text.Json;
using Application.ComicInfoSearch;
using Application.Interfaces;
using Domain.Settings;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Application.UnitTests.ComicInfoSearch;

public sealed class BedethequeServiceTests : IDisposable
{
    private const string ValidIsbn = "9782205057317";
    private const string ValidIsbnWithDashes = "978-2-205-05731-7";
    private const string PageUrl = "https://www.bedetheque.com/BD-Biguden-Tome-1-LAnkou-224335.html";
    private const string ExpectedCoverUrl = "https://www.bedetheque.com/media/Couvertures/Couv_224335.jpg";

    // Fixtures are extracts of real pages fetched through Cloudflare Browser Run, saved in ComicInfoSearch/Fixtures.
    private const string OkkoIsbn = "2847891641";
    private const string OkkoAlbumUrl = "https://www.bedetheque.com/BD-Okko-Tome-1-Le-cycle-de-l-eau-I-41693.html";
    private const string OkkoSerieUrl = "https://www.bedetheque.com/serie-10529-BD-Okko.html";
    private const string OkkoAllAlbumsUrl = "https://www.bedetheque.com/serie-10529-BD-Okko__10000.html";
    private const string BrowserRunContentUrl = "https://api.cloudflare.com/client/v4/accounts/account-id/browser-run/content";

    private static readonly BedethequeSettings BrowserRunSettings = new()
    {
        SerpApiKey = "test-key",
        SerpApiBaseUrl = new Uri("https://serpapi.com"),
        BaseUrl = new Uri("https://www.bedetheque.com"),
        CloudflareAccountId = "account-id",
        CloudflareApiToken = "api-token"
    };

    private static string Fixture(string name) =>
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "ComicInfoSearch", "Fixtures", name));

    private static readonly BedethequeSettings DefaultSettings = new()
    {
        SerpApiKey = "test-key",
        SerpApiBaseUrl = new Uri("https://serpapi.com"),
        BaseUrl = new Uri("https://www.bedetheque.com")
    };

    // Minimal album sheet matching the real Bedetheque structure ("Informations sur l'album")
    private const string FullAlbumHtml = """
        <html><body>
        <dl class="bdt-sheet">
          <dt>Série</dt><dd>Biguden</dd>
          <dt>Titre</dt><dd>L'Ankou</dd>
          <dt>Tome</dt><dd>1</dd>
          <dt>Scénario</dt><dd><a href="/auteur-1">Silas, Stan</a></dd>
          <dt>Dessin</dt><dd><a href="/auteur-2">Dupont, Jean</a></dd>
          <dt>Dépot légal</dt><dd>08/2014 <span class="bdt-dim">· parution 27/08/2014</span></dd>
          <dt>Editeur</dt><dd>EP Media</dd>
          <dt>Planches</dt><dd>62</dd>
        </dl>
        </body></html>
        """;

    // ── Disposal tracker ─────────────────────────────────────────────

    private readonly List<IDisposable> _disposables = [];

    public void Dispose()
    {
        foreach (var d in _disposables)
        {
            d.Dispose();
        }

        GC.SuppressFinalize(this);
    }

    private T Track<T>(T disposable) where T : IDisposable
    {
        _disposables.Add(disposable);
        return disposable;
    }

    // ── Factories / helpers ───────────────────────────────────────────

    private static BedethequeService CreateService(
        IHttpClientFactory factory,
        IIsbnBedethequeCacheRepository? cache = null,
        BedethequeSettings? settings = null,
        BedethequeCircuit? circuit = null,
        bool enabled = true)
    {
        cache ??= EmptyCache();
        var options = Options.Create(settings ?? DefaultSettings);
        var featureToggles = Substitute.For<IFeatureToggles>();
        featureToggles.IsEnabled(FeatureToggle.Bedetheque).Returns(enabled);
        return new BedethequeService(factory, cache, options, circuit ?? new BedethequeCircuit(TimeProvider.System),
            featureToggles, NullLogger<BedethequeService>.Instance);
    }

    private static IIsbnBedethequeCacheRepository EmptyCache()
    {
        var cache = Substitute.For<IIsbnBedethequeCacheRepository>();
        cache.GetUrlByIsbnAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
             .Returns((string?)null);
        return cache;
    }

    private static IIsbnBedethequeCacheRepository CacheWithUrl(string isbn, string url)
    {
        var cache = Substitute.For<IIsbnBedethequeCacheRepository>();
        cache.GetUrlByIsbnAsync(isbn, Arg.Any<CancellationToken>()).Returns(url);
        return cache;
    }

    private IHttpClientFactory FactoryWith(
        HttpMessageHandler? serpHandler = null,
        HttpMessageHandler? bedeHandler = null,
        HttpMessageHandler? browserRunHandler = null)
    {
        var factory = Substitute.For<IHttpClientFactory>();
        if (serpHandler != null)
        {
            factory.CreateClient("SerpApi").Returns(Track(new HttpClient(serpHandler)));
        }
        if (bedeHandler != null)
        {
            factory.CreateClient("Bedetheque").Returns(Track(new HttpClient(bedeHandler)));
        }
        if (browserRunHandler != null)
        {
            factory.CreateClient("BrowserRun").Returns(Track(new HttpClient(browserRunHandler)));
        }
        return factory;
    }

    private static HttpResponseMessage BrowserRunResponse(string html) =>
        JsonResponse(JsonSerializer.Serialize(new { success = true, result = html, errors = Array.Empty<object>() }));

    // Factory that serves a page directly (used with CacheWithUrl)
    private IHttpClientFactory PageFactory(string html = FullAlbumHtml) =>
        FactoryWith(bedeHandler: Track(new FakeHttpMessageHandler(_ => HtmlResponse(html))));

    // Factory with both SerpApi + Bedetheque page handlers
    private IHttpClientFactory SerpAndPageFactory(string serpJson, string html = FullAlbumHtml) =>
        FactoryWith(
            Track(new FakeHttpMessageHandler(_ => JsonResponse(serpJson))),
            Track(new FakeHttpMessageHandler(_ => HtmlResponse(html))));

    private static HttpResponseMessage JsonResponse(string json) =>
        new(HttpStatusCode.OK)
        {
            Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
        };

    private static HttpResponseMessage HtmlResponse(string html) =>
        new(HttpStatusCode.OK)
        {
            Content = new StringContent(html, System.Text.Encoding.UTF8, "text/html")
        };

    private static string SerpApiJson(params string[] links)
    {
        var items = string.Join(",", links.Select(l => $$"""{"link":"{{l}}"}"""));
        return $$"""{"organic_results":[{{items}}]}""";
    }

    // ── SearchByIsbnAsync — URL resolution ───────────────────────────

    [Fact]
    public async Task SearchByIsbnAsync_Should_ReturnResult_WhenCacheHit()
    {
        var cache = CacheWithUrl(ValidIsbn, PageUrl);
        var service = CreateService(PageFactory(), cache);

        var result = await service.SearchByIsbnAsync(ValidIsbn, TestContext.Current.CancellationToken);

        result.Found.Should().BeTrue();
        result.Title.Should().Be("L'Ankou");
        result.Serie.Should().Be("Biguden");
    }

    [Fact]
    public async Task SearchByIsbnAsync_Should_NotCallSerpApi_WhenCacheHit()
    {
        var cache = CacheWithUrl(ValidIsbn, PageUrl);
        var factory = PageFactory();
        var service = CreateService(factory, cache);

        var result = await service.SearchByIsbnAsync(ValidIsbn, TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        factory.DidNotReceive().CreateClient("SerpApi");
    }

    [Fact]
    public async Task SearchByIsbnAsync_Should_ReturnNotFound_WhenSerpApiKeyIsEmpty()
    {
        var settings = new BedethequeSettings
        {
            SerpApiKey = "",
            BaseUrl = new Uri("https://www.bedetheque.com"),
            SerpApiBaseUrl = new Uri("https://serpapi.com")
        };
        var service = CreateService(Substitute.For<IHttpClientFactory>(), settings: settings);

        var result = await service.SearchByIsbnAsync(ValidIsbn, TestContext.Current.CancellationToken);

        result.Found.Should().BeFalse();
    }

    [Fact]
    public async Task SearchByIsbnAsync_Should_ReturnNotFound_WhenSerpApiKeyIsWhitespace()
    {
        var settings = new BedethequeSettings
        {
            SerpApiKey = "   ",
            BaseUrl = new Uri("https://www.bedetheque.com"),
            SerpApiBaseUrl = new Uri("https://serpapi.com")
        };
        var service = CreateService(Substitute.For<IHttpClientFactory>(), settings: settings);

        var result = await service.SearchByIsbnAsync(ValidIsbn, TestContext.Current.CancellationToken);

        result.Found.Should().BeFalse();
    }

    [Fact]
    public async Task SearchByIsbnAsync_Should_ReturnNotFound_WhenSerpApiReturnsNonSuccess()
    {
        var factory = FactoryWith(Track(new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.Unauthorized))));
        var service = CreateService(factory);

        var result = await service.SearchByIsbnAsync(ValidIsbn, TestContext.Current.CancellationToken);

        result.Found.Should().BeFalse();
    }

    [Fact]
    public async Task SearchByIsbnAsync_Should_ReturnNotFound_WhenNoBdLinksInSerpApiResponse()
    {
        var serpJson = SerpApiJson("https://www.amazon.com/book", "https://www.fnac.com/book");
        var service = CreateService(FactoryWith(Track(new FakeHttpMessageHandler(_ => JsonResponse(serpJson)))));

        var result = await service.SearchByIsbnAsync(ValidIsbn, TestContext.Current.CancellationToken);

        result.Found.Should().BeFalse();
        result.Failed.Should().BeFalse();
    }

    [Fact]
    public async Task SearchByIsbnAsync_Should_ReturnNotFound_WhenMultipleBdLinksInSerpApiResponse()
    {
        var serpJson = SerpApiJson(
            "https://www.bedetheque.com/BD-Series-Tome-1-Title-111.html",
            "https://www.bedetheque.com/BD-Series-Tome-2-Title-222.html");
        var service = CreateService(FactoryWith(Track(new FakeHttpMessageHandler(_ => JsonResponse(serpJson)))));

        var result = await service.SearchByIsbnAsync(ValidIsbn, TestContext.Current.CancellationToken);

        result.Found.Should().BeFalse();
    }

    [Fact]
    public async Task SearchByIsbnAsync_Should_ReturnNotFound_WhenSerpApiHasNullOrganicResults()
    {
        var factory = FactoryWith(Track(new FakeHttpMessageHandler(_ => JsonResponse("""{"organic_results":null}"""))));
        var service = CreateService(factory);

        var result = await service.SearchByIsbnAsync(ValidIsbn, TestContext.Current.CancellationToken);

        result.Found.Should().BeFalse();
    }

    [Fact]
    public async Task SearchByIsbnAsync_Should_SaveUrlToCache_WhenExactlyOneBdLinkFound()
    {
        var cache = EmptyCache();
        var service = CreateService(SerpAndPageFactory(SerpApiJson(PageUrl)), cache);

        await service.SearchByIsbnAsync(ValidIsbn, TestContext.Current.CancellationToken);

        await cache.Received(1).SaveAsync(ValidIsbn, PageUrl, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SearchByIsbnAsync_Should_ReturnFailed_WhenPageFetchFails()
    {
        var factory = FactoryWith(
            Track(new FakeHttpMessageHandler(_ => JsonResponse(SerpApiJson(PageUrl)))),
            Track(new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.NotFound))));
        var service = CreateService(factory);

        var result = await service.SearchByIsbnAsync(ValidIsbn, TestContext.Current.CancellationToken);

        result.Found.Should().BeFalse();
        result.Failed.Should().BeTrue();
    }

    [Fact]
    public async Task SearchByIsbnAsync_Should_NormalizeIsbn_WhenIsbnContainsDashes()
    {
        // Cache is keyed on the cleaned ISBN (no dashes)
        var cache = CacheWithUrl(ValidIsbn, PageUrl);
        var service = CreateService(PageFactory(), cache);

        var result = await service.SearchByIsbnAsync(ValidIsbnWithDashes, TestContext.Current.CancellationToken);

        result.Found.Should().BeTrue();
    }

    [Fact]
    public async Task SearchByIsbnAsync_Should_ReturnFailed_WhenHttpRequestFails()
    {
        var factory = FactoryWith(Track(new FakeHttpMessageHandler(new HttpRequestException("Network error"))));
        var service = CreateService(factory);

        var result = await service.SearchByIsbnAsync(ValidIsbn, TestContext.Current.CancellationToken);

        result.Found.Should().BeFalse();
        result.Failed.Should().BeTrue();
    }

    [Fact]
    public async Task SearchByIsbnAsync_Should_ReturnFailed_WhenJsonParsingFails()
    {
        var factory = FactoryWith(Track(new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("invalid json {{{", System.Text.Encoding.UTF8, "application/json")
        })));
        var service = CreateService(factory);

        var result = await service.SearchByIsbnAsync(ValidIsbn, TestContext.Current.CancellationToken);

        result.Found.Should().BeFalse();
        result.Failed.Should().BeTrue();
    }

    [Fact]
    public async Task SearchByIsbnAsync_Should_ReturnFailed_WhenTimeoutOccurs()
    {
        // TaskCanceledException without a cancelled caller token → internal timeout
        var factory = FactoryWith(Track(new FakeHttpMessageHandler(new TaskCanceledException("Timeout"))));
        var service = CreateService(factory);

        var result = await service.SearchByIsbnAsync(ValidIsbn, TestContext.Current.CancellationToken);

        result.Found.Should().BeFalse();
        result.Failed.Should().BeTrue();
    }

    [Fact]
    public async Task SearchByIsbnAsync_Should_PropagateCancellation_WhenCallerCancels()
    {
        using var cts = new CancellationTokenSource();
        var cache = CacheWithUrl(ValidIsbn, PageUrl);
        var factory = PageFactory();
        var service = CreateService(factory, cache);
        await cts.CancelAsync();

        await Assert.ThrowsAsync<TaskCanceledException>(
            () => service.SearchByIsbnAsync(ValidIsbn, cts.Token));
    }

    // ── SearchByIsbnAsync — HTML parsing ─────────────────────────────

    [Fact]
    public async Task SearchByIsbnAsync_Should_ParseTitle_WhenTitreFieldPresent()
    {
        var cache = CacheWithUrl(ValidIsbn, PageUrl);
        var result = await CreateService(PageFactory(), cache).SearchByIsbnAsync(ValidIsbn, TestContext.Current.CancellationToken);

        result.Title.Should().Be("L'Ankou");
    }

    [Fact]
    public async Task SearchByIsbnAsync_Should_ReturnEmptyTitle_WhenTitreFieldMissing()
    {
        var html = """<html><body><dl class="bdt-sheet"><dt>Série</dt><dd>OnlySerie</dd></dl></body></html>""";
        var cache = CacheWithUrl(ValidIsbn, PageUrl);
        var result = await CreateService(PageFactory(html), cache).SearchByIsbnAsync(ValidIsbn, TestContext.Current.CancellationToken);

        result.Title.Should().BeEmpty();
    }

    [Fact]
    public async Task SearchByIsbnAsync_Should_ParseSerie_WhenSerieFieldPresent()
    {
        var cache = CacheWithUrl(ValidIsbn, PageUrl);
        var result = await CreateService(PageFactory(), cache).SearchByIsbnAsync(ValidIsbn, TestContext.Current.CancellationToken);

        result.Serie.Should().Be("Biguden");
    }

    [Fact]
    public async Task SearchByIsbnAsync_Should_ParseVolumeNumber_WhenTomeFieldPresent()
    {
        var cache = CacheWithUrl(ValidIsbn, PageUrl);
        var result = await CreateService(PageFactory(), cache).SearchByIsbnAsync(ValidIsbn, TestContext.Current.CancellationToken);

        result.VolumeNumber.Should().Be(1);
    }

    [Fact]
    public async Task SearchByIsbnAsync_Should_DefaultVolumeNumberToOne_WhenTomeFieldMissing()
    {
        var html = """<html><body><dl class="bdt-sheet"><dt>Titre</dt><dd>Album</dd></dl></body></html>""";
        var cache = CacheWithUrl(ValidIsbn, PageUrl);
        var result = await CreateService(PageFactory(html), cache).SearchByIsbnAsync(ValidIsbn, TestContext.Current.CancellationToken);

        result.VolumeNumber.Should().Be(1);
    }

    [Fact]
    public async Task SearchByIsbnAsync_Should_ParseBothAuthors_WhenScenarioAndDessinAreDifferent()
    {
        var cache = CacheWithUrl(ValidIsbn, PageUrl);
        var result = await CreateService(PageFactory(), cache).SearchByIsbnAsync(ValidIsbn, TestContext.Current.CancellationToken);

        result.Authors.Should().HaveCount(2);
        result.Authors.Should().Contain("Silas, Stan");
        result.Authors.Should().Contain("Dupont, Jean");
    }

    [Fact]
    public async Task SearchByIsbnAsync_Should_DeduplicateAuthors_WhenScenarioAndDessinAreSamePerson()
    {
        var html = """
            <html><body><dl class="bdt-sheet">
              <dt>Titre</dt><dd>Album</dd>
              <dt>Scénario</dt><dd><a href="/auteur-1">Silas, Stan</a></dd>
              <dt>Dessin</dt><dd><a href="/auteur-1">Silas, Stan</a></dd>
            </dl></body></html>
            """;
        var cache = CacheWithUrl(ValidIsbn, PageUrl);
        var result = await CreateService(PageFactory(html), cache).SearchByIsbnAsync(ValidIsbn, TestContext.Current.CancellationToken);

        result.Authors.Should().ContainSingle().Which.Should().Be("Silas, Stan");
    }

    [Fact]
    public async Task SearchByIsbnAsync_Should_ParseMultipleAuthors_WhenMultipleAnchorsUnderScenarioLabel()
    {
        var html = """
            <html><body><dl class="bdt-sheet">
              <dt>Titre</dt><dd>Album</dd>
              <dt>Scénario</dt><dd><a href="/auteur-1">Alpha, Jean</a><a href="/auteur-2">Beta, Marie</a></dd>
            </dl></body></html>
            """;
        var cache = CacheWithUrl(ValidIsbn, PageUrl);
        var result = await CreateService(PageFactory(html), cache).SearchByIsbnAsync(ValidIsbn, TestContext.Current.CancellationToken);

        result.Authors.Should().HaveCount(2);
        result.Authors.Should().Contain("Alpha, Jean");
        result.Authors.Should().Contain("Beta, Marie");
    }

    [Fact]
    public async Task SearchByIsbnAsync_Should_ParseMultipleAuthors_WhenMultipleAnchorsUnderDessinLabel()
    {
        var html = """
            <html><body><dl class="bdt-sheet">
              <dt>Titre</dt><dd>Album</dd>
              <dt>Dessin</dt><dd><a href="/auteur-1">Alpha, Jean</a><a href="/auteur-2">Beta, Marie</a></dd>
            </dl></body></html>
            """;
        var cache = CacheWithUrl(ValidIsbn, PageUrl);
        var result = await CreateService(PageFactory(html), cache).SearchByIsbnAsync(ValidIsbn, TestContext.Current.CancellationToken);

        result.Authors.Should().HaveCount(2);
        result.Authors.Should().Contain("Alpha, Jean");
        result.Authors.Should().Contain("Beta, Marie");
    }

    [Fact]
    public async Task SearchByIsbnAsync_Should_ReturnEmptyAuthors_WhenNoAuthorFields()
    {
        var html = """<html><body><dl class="bdt-sheet"><dt>Titre</dt><dd>Album</dd></dl></body></html>""";
        var cache = CacheWithUrl(ValidIsbn, PageUrl);
        var result = await CreateService(PageFactory(html), cache).SearchByIsbnAsync(ValidIsbn, TestContext.Current.CancellationToken);

        result.Authors.Should().BeEmpty();
    }

    [Fact]
    public async Task SearchByIsbnAsync_Should_ParsePublisher_WhenEditeurFieldPresent()
    {
        var cache = CacheWithUrl(ValidIsbn, PageUrl);
        var result = await CreateService(PageFactory(), cache).SearchByIsbnAsync(ValidIsbn, TestContext.Current.CancellationToken);

        result.Publishers.Should().ContainSingle().Which.Should().Be("EP Media");
    }

    [Fact]
    public async Task SearchByIsbnAsync_Should_ReturnEmptyPublishers_WhenNoEditeurField()
    {
        var html = """<html><body><dl class="bdt-sheet"><dt>Titre</dt><dd>Album</dd></dl></body></html>""";
        var cache = CacheWithUrl(ValidIsbn, PageUrl);
        var result = await CreateService(PageFactory(html), cache).SearchByIsbnAsync(ValidIsbn, TestContext.Current.CancellationToken);

        result.Publishers.Should().BeEmpty();
    }

    [Fact]
    public async Task SearchByIsbnAsync_Should_ParsePublishDate_FromParutionLeDate()
    {
        var cache = CacheWithUrl(ValidIsbn, PageUrl);
        var result = await CreateService(PageFactory(), cache).SearchByIsbnAsync(ValidIsbn, TestContext.Current.CancellationToken);

        result.PublishDate.Should().Be(new DateOnly(2014, 8, 27));
    }

    [Fact]
    public async Task SearchByIsbnAsync_Should_ParsePublishDate_FromMonthYearFallback()
    {
        var html = """
            <html><body><dl class="bdt-sheet">
              <dt>Titre</dt><dd>Album</dd>
              <dt>Dépot légal</dt><dd>11/2025</dd>
            </dl></body></html>
            """;
        var cache = CacheWithUrl(ValidIsbn, PageUrl);
        var result = await CreateService(PageFactory(html), cache).SearchByIsbnAsync(ValidIsbn, TestContext.Current.CancellationToken);

        result.PublishDate.Should().Be(new DateOnly(2025, 11, 1));
    }

    [Fact]
    public async Task SearchByIsbnAsync_Should_ReturnNullPublishDate_WhenNoDateField()
    {
        var html = """<html><body><dl class="bdt-sheet"><dt>Titre</dt><dd>Album</dd></dl></body></html>""";
        var cache = CacheWithUrl(ValidIsbn, PageUrl);
        var result = await CreateService(PageFactory(html), cache).SearchByIsbnAsync(ValidIsbn, TestContext.Current.CancellationToken);

        result.PublishDate.Should().BeNull();
    }

    [Fact]
    public async Task SearchByIsbnAsync_Should_ParseNumberOfPages_WhenPlanchesFieldPresent()
    {
        var cache = CacheWithUrl(ValidIsbn, PageUrl);
        var result = await CreateService(PageFactory(), cache).SearchByIsbnAsync(ValidIsbn, TestContext.Current.CancellationToken);

        result.NumberOfPages.Should().Be(62);
    }

    [Fact]
    public async Task SearchByIsbnAsync_Should_ReturnNullNumberOfPages_WhenPlanchesMissing()
    {
        var html = """<html><body><dl class="bdt-sheet"><dt>Titre</dt><dd>Album</dd></dl></body></html>""";
        var cache = CacheWithUrl(ValidIsbn, PageUrl);
        var result = await CreateService(PageFactory(html), cache).SearchByIsbnAsync(ValidIsbn, TestContext.Current.CancellationToken);

        result.NumberOfPages.Should().BeNull();
    }

    [Fact]
    public async Task SearchByIsbnAsync_Should_BuildCoverUrl_WhenPageUrlContainsNumericId()
    {
        var cache = CacheWithUrl(ValidIsbn, PageUrl);
        var result = await CreateService(PageFactory(), cache).SearchByIsbnAsync(ValidIsbn, TestContext.Current.CancellationToken);

        result.CoverUrl.Should().Be(new Uri(ExpectedCoverUrl));
    }

    [Fact]
    public async Task SearchByIsbnAsync_Should_ReturnNullCoverUrl_WhenPageUrlHasNoNumericId()
    {
        var noIdUrl = "https://www.bedetheque.com/BD-Biguden-LAnkou.html";
        var cache = CacheWithUrl(ValidIsbn, noIdUrl);
        var result = await CreateService(PageFactory(), cache).SearchByIsbnAsync(ValidIsbn, TestContext.Current.CancellationToken);

        result.CoverUrl.Should().BeNull();
    }

    [Fact]
    public async Task SearchByIsbnAsync_Should_ReturnFoundTrue_WhenTitlePresent()
    {
        var cache = CacheWithUrl(ValidIsbn, PageUrl);
        var result = await CreateService(PageFactory(), cache).SearchByIsbnAsync(ValidIsbn, TestContext.Current.CancellationToken);

        result.Found.Should().BeTrue();
    }

    [Fact]
    public async Task SearchByIsbnAsync_Should_ReturnFoundTrue_WhenOnlySeriePresent()
    {
        var html = """<html><body><dl class="bdt-sheet"><dt>Série</dt><dd>OnlySerie</dd></dl></body></html>""";
        var cache = CacheWithUrl(ValidIsbn, PageUrl);
        var result = await CreateService(PageFactory(html), cache).SearchByIsbnAsync(ValidIsbn, TestContext.Current.CancellationToken);

        result.Found.Should().BeTrue();
        result.Serie.Should().Be("OnlySerie");
        result.Title.Should().BeEmpty();
    }

    [Fact]
    public async Task SearchByIsbnAsync_Should_ReturnFoundFalse_WhenBothTitleAndSerieEmpty()
    {
        var html = """<html><body><dl class="bdt-sheet"></dl></body></html>""";
        var cache = CacheWithUrl(ValidIsbn, PageUrl);
        var result = await CreateService(PageFactory(html), cache).SearchByIsbnAsync(ValidIsbn, TestContext.Current.CancellationToken);

        result.Found.Should().BeFalse();
    }

    // ── SearchByIsbnAsync — caching ──────────────────────────────────

    [Fact]
    public async Task SearchByIsbnAsync_Should_NotSaveToCache_WhenCacheHit()
    {
        var cache = CacheWithUrl(ValidIsbn, PageUrl);
        var service = CreateService(PageFactory(), cache);

        await service.SearchByIsbnAsync(ValidIsbn, TestContext.Current.CancellationToken);

        await cache.DidNotReceive().SaveAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    // ── SearchByIsbnAsync — URL deduplication ────────────────────────

    [Fact]
    public async Task SearchByIsbnAsync_Should_ReturnFound_WhenDuplicateBdLinksDeduplicatedToOne()
    {
        var serpJson = SerpApiJson(PageUrl, PageUrl);
        var service = CreateService(SerpAndPageFactory(serpJson));

        var result = await service.SearchByIsbnAsync(ValidIsbn, TestContext.Current.CancellationToken);

        result.Found.Should().BeTrue();
    }

    // ── SearchByIsbnAsync — serie-bd link filtering ──────────────────

    [Fact]
    public async Task SearchByIsbnAsync_Should_ReturnNotFound_WhenOnlySerieBdLinksPresent()
    {
        var serpJson = SerpApiJson("https://www.bedetheque.com/serie-bd-Foo-123.html");
        var factory = FactoryWith(Track(new FakeHttpMessageHandler(_ => JsonResponse(serpJson))));
        var service = CreateService(factory);

        var result = await service.SearchByIsbnAsync(ValidIsbn, TestContext.Current.CancellationToken);

        result.Found.Should().BeFalse();
    }

    // ── SearchByIsbnAsync — serie page resolution ────────────────────

    [Theory]
    [InlineData("https://schema.org/Book")]
    [InlineData("http://schema.org/Book")]
    public async Task SearchByIsbnAsync_Should_ReturnFound_WhenSeriePageContainsMatchingIsbn(string itemType)
    {
        var serieUrl = "https://www.bedetheque.com/serie-Biguden-123.html";
        var serieHtml = $"""
            <html><body><ul>
              <li itemscope itemtype="{itemType}">
                <span itemprop="isbn">{ValidIsbn}</span>
                <a itemprop="url" class="titre" href="{PageUrl}">L'Ankou</a>
              </li>
            </ul></body></html>
            """;
        var serpJson = SerpApiJson(serieUrl);
        var factory = FactoryWith(
            Track(new FakeHttpMessageHandler(_ => JsonResponse(serpJson))),
            Track(new FakeHttpMessageHandler(req =>
                req.RequestUri!.ToString().Contains("serie-", StringComparison.OrdinalIgnoreCase)
                    ? HtmlResponse(serieHtml)
                    : HtmlResponse(FullAlbumHtml))));
        var service = CreateService(factory);

        var result = await service.SearchByIsbnAsync(ValidIsbn, TestContext.Current.CancellationToken);

        result.Found.Should().BeTrue();
        result.Title.Should().Be("L'Ankou");
    }

    [Fact]
    public async Task SearchByIsbnAsync_Should_ReturnNotFound_WhenSeriePageHasNoMatchingIsbn()
    {
        var serieUrl = "https://www.bedetheque.com/serie-Biguden-123.html";
        var serieHtml = """
            <html><body><ul>
              <li itemscope itemtype="https://schema.org/Book">
                <span itemprop="isbn">9780000000000</span>
                <a itemprop="url" class="titre" href="https://www.bedetheque.com/BD-Other-1.html">Other</a>
              </li>
            </ul></body></html>
            """;
        var serpJson = SerpApiJson(serieUrl);
        var factory = FactoryWith(
            Track(new FakeHttpMessageHandler(_ => JsonResponse(serpJson))),
            Track(new FakeHttpMessageHandler(_ => HtmlResponse(serieHtml))));
        var service = CreateService(factory);

        var result = await service.SearchByIsbnAsync(ValidIsbn, TestContext.Current.CancellationToken);

        result.Found.Should().BeFalse();
    }

    [Fact]
    public async Task SearchByIsbnAsync_Should_ReturnNotFound_WhenSeriePageFetchFails()
    {
        var serieUrl = "https://www.bedetheque.com/serie-Biguden-123.html";
        var serpJson = SerpApiJson(serieUrl);
        var factory = FactoryWith(
            Track(new FakeHttpMessageHandler(_ => JsonResponse(serpJson))),
            Track(new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.NotFound))));
        var service = CreateService(factory);

        var result = await service.SearchByIsbnAsync(ValidIsbn, TestContext.Current.CancellationToken);

        result.Found.Should().BeFalse();
    }

    [Theory]
    [InlineData(HttpStatusCode.OK, false)]
    [InlineData(HttpStatusCode.Forbidden, false)]
    [InlineData(HttpStatusCode.Forbidden, true)]
    public async Task SearchByIsbnAsync_Should_ReturnFailed_WhenBedethequeAnswersWithACloudflareChallenge(HttpStatusCode status, bool mitigatedHeader)
    {
        var serieUrl = "https://www.bedetheque.com/serie-34958-BD-Tangente.html";
        var serpJson = SerpApiJson(serieUrl);
        var challenge = mitigatedHeader
            ? "<html><body>Blocked</body></html>"
            : "<html><head><title>Just a moment...</title></head><body>Checking your browser - cloudflare</body></html>";
        var factory = FactoryWith(
            Track(new FakeHttpMessageHandler(_ => JsonResponse(serpJson))),
            Track(new FakeHttpMessageHandler(_ =>
            {
                var response = HtmlResponse(challenge);
                response.StatusCode = status;
                if (mitigatedHeader)
                {
                    response.Headers.Add("cf-mitigated", "challenge");
                }
                return response;
            })));
        var circuit = new BedethequeCircuit(TimeProvider.System);
        var service = CreateService(factory, circuit: circuit);

        var result = await service.SearchByIsbnAsync(ValidIsbn, TestContext.Current.CancellationToken);

        result.Found.Should().BeFalse();
        result.Failed.Should().BeTrue();
        circuit.PausedUntil.Should().NotBeNull("the next searches must not spend SerpApi calls while Cloudflare blocks the server");
    }

    [Fact]
    public async Task SearchByIsbnAsync_Should_NotCallAnything_WhileBedethequeIsPaused()
    {
        var requests = 0;
        var factory = FactoryWith(
            Track(new FakeHttpMessageHandler(_ => { requests++; return JsonResponse(SerpApiJson(PageUrl)); })),
            Track(new FakeHttpMessageHandler(_ => { requests++; return HtmlResponse(FullAlbumHtml); })));
        var circuit = new BedethequeCircuit(TimeProvider.System);
        circuit.Pause(TimeSpan.FromHours(24));
        var service = CreateService(factory, circuit: circuit);

        var result = await service.SearchByIsbnAsync(ValidIsbn, TestContext.Current.CancellationToken);

        result.Found.Should().BeFalse();
        result.Failed.Should().BeTrue();
        requests.Should().Be(0);
    }

    [Fact]
    public async Task SearchByIsbnAsync_Should_NotCallAnything_WhenBedethequeIsTurnedOff()
    {
        var requests = 0;
        var factory = FactoryWith(
            Track(new FakeHttpMessageHandler(_ => { requests++; return JsonResponse(SerpApiJson(PageUrl)); })),
            Track(new FakeHttpMessageHandler(_ => { requests++; return HtmlResponse(FullAlbumHtml); })));
        var service = CreateService(factory, enabled: false);

        var result = await service.SearchByIsbnAsync(ValidIsbn, TestContext.Current.CancellationToken);

        service.IsEnabled.Should().BeFalse();
        result.Found.Should().BeFalse();
        requests.Should().Be(0);
    }

    [Fact]
    public async Task SearchByIsbnAsync_Should_ReturnNotFound_WhenSeriePageHasNoBookItems()
    {
        var serieUrl = "https://www.bedetheque.com/serie-Biguden-123.html";
        var serieHtml = "<html><body><p>No books here</p></body></html>";
        var serpJson = SerpApiJson(serieUrl);
        var factory = FactoryWith(
            Track(new FakeHttpMessageHandler(_ => JsonResponse(serpJson))),
            Track(new FakeHttpMessageHandler(_ => HtmlResponse(serieHtml))));
        var service = CreateService(factory);

        var result = await service.SearchByIsbnAsync(ValidIsbn, TestContext.Current.CancellationToken);

        result.Found.Should().BeFalse();
    }

    [Fact]
    public async Task SearchByIsbnAsync_Should_SkipItem_WhenSeriePageItemHasNoIsbnSpan()
    {
        var serieUrl = "https://www.bedetheque.com/serie-Biguden-123.html";
        var serieHtml = $"""
            <html><body><ul>
              <li itemscope itemtype="https://schema.org/Book">
                <a itemprop="url" class="titre" href="{PageUrl}">L'Ankou</a>
              </li>
            </ul></body></html>
            """;
        var serpJson = SerpApiJson(serieUrl);
        var factory = FactoryWith(
            Track(new FakeHttpMessageHandler(_ => JsonResponse(serpJson))),
            Track(new FakeHttpMessageHandler(_ => HtmlResponse(serieHtml))));
        var service = CreateService(factory);

        var result = await service.SearchByIsbnAsync(ValidIsbn, TestContext.Current.CancellationToken);

        result.Found.Should().BeFalse();
    }

    // ── SearchByIsbnAsync — real album page ──────────────────────────

    [Fact]
    public async Task SearchByIsbnAsync_Should_ReadTheAlbumSheet_WhenPageIsARealBedethequeAlbum()
    {
        var cache = CacheWithUrl(OkkoIsbn, OkkoAlbumUrl);
        var service = CreateService(PageFactory(Fixture("bedetheque-album-41693.html")), cache);

        var result = await service.SearchByIsbnAsync(OkkoIsbn, TestContext.Current.CancellationToken);

        // The page also holds the reviews (schema.org author, datePublished) and the other editions
        // (compact sheets): only the album sheet is read, and the colorists are not authors.
        result.Should().BeEquivalentTo(new BedethequeBookResult(
            Title: "Le cycle de l'eau I",
            Serie: "Okko",
            VolumeNumber: 1,
            Authors: ["Hub"],
            Publishers: ["Delcourt"],
            PublishDate: new DateOnly(2005, 1, 1),
            NumberOfPages: 46,
            CoverUrl: new Uri("https://www.bedetheque.com/media/Couvertures/Couv_41693.jpg"),
            Found: true));
    }

    [Fact]
    public async Task SearchByIsbnAsync_Should_ReadEveryAuthor_WhenAFieldContinuesOnAnUnlabelledLine()
    {
        var html = """
            <html><body><dl class="bdt-sheet">
              <dt>Titre</dt><dd>Album</dd>
              <dt>Scénario</dt><dd><a href="/auteur-1">Alpha, Jean</a></dd>
              <dt></dt><dd><a href="/auteur-2">Beta, Marie</a></dd>
              <dt>Couleurs</dt><dd><a href="/auteur-3">Gamma, Paul</a></dd>
              <dt></dt><dd><a href="/auteur-4">Delta, Anne</a></dd>
            </dl></body></html>
            """;
        var cache = CacheWithUrl(ValidIsbn, PageUrl);
        var result = await CreateService(PageFactory(html), cache).SearchByIsbnAsync(ValidIsbn, TestContext.Current.CancellationToken);

        result.Authors.Should().Equal("Alpha, Jean", "Beta, Marie");
    }

    // ── SearchByIsbnAsync — publish date fallback ────────────────────

    [Fact]
    public async Task SearchByIsbnAsync_Should_ParsePublishDate_FromMonthYear_WhenParutionDateIsInvalid()
    {
        var html = """
            <html><body><dl class="bdt-sheet">
              <dt>Titre</dt><dd>Album</dd>
              <dt>Dépot légal</dt><dd>06/2020 <span class="bdt-dim">· parution 00/00/0000</span></dd>
            </dl></body></html>
            """;
        var cache = CacheWithUrl(ValidIsbn, PageUrl);
        var result = await CreateService(PageFactory(html), cache).SearchByIsbnAsync(ValidIsbn, TestContext.Current.CancellationToken);

        result.PublishDate.Should().Be(new DateOnly(2020, 6, 1));
    }


    // ── SearchByIsbnAsync — multiple ISBN format fallback ────────────

    [Fact]
    public async Task SearchByIsbnAsync_Should_TryHyphenatedIsbn_WhenPlainIsbnFindsNothing()
    {
        // First SerpApi call (plain ISBN) returns no results; second (hyphenated) returns a BD link.
        var callCount = 0;
        var factory = FactoryWith(
            Track(new FakeHttpMessageHandler(_ =>
            {
                callCount++;
                return callCount == 2
                    ? JsonResponse(SerpApiJson(PageUrl))
                    : JsonResponse(SerpApiJson());
            })),
            Track(new FakeHttpMessageHandler(_ => HtmlResponse(FullAlbumHtml))));
        var service = CreateService(factory);

        var result = await service.SearchByIsbnAsync(ValidIsbn, TestContext.Current.CancellationToken);

        result.Found.Should().BeTrue();
    }

    [Fact]
    public async Task SearchByIsbnAsync_Should_TryShortIsbn_WhenPlainAndHyphenatedFindNothing()
    {
        // First two SerpApi calls return no results; third (short ISBN) returns a BD link.
        var callCount = 0;
        var factory = FactoryWith(
            Track(new FakeHttpMessageHandler(_ =>
            {
                callCount++;
                return callCount == 3
                    ? JsonResponse(SerpApiJson(PageUrl))
                    : JsonResponse(SerpApiJson());
            })),
            Track(new FakeHttpMessageHandler(_ => HtmlResponse(FullAlbumHtml))));
        var service = CreateService(factory);

        var result = await service.SearchByIsbnAsync(ValidIsbn, TestContext.Current.CancellationToken);

        result.Found.Should().BeTrue();
    }

    // ── SearchByIsbnAsync — real serie page ──────────────────────────

    [Fact]
    public async Task SearchByIsbnAsync_Should_FindTheAlbumInThePageOfAllAlbums_WhenSerpApiFindsASeriePage()
    {
        var requested = new List<string>();
        var factory = FactoryWith(
            Track(new FakeHttpMessageHandler(_ => JsonResponse(SerpApiJson(OkkoSerieUrl)))),
            Track(new FakeHttpMessageHandler(req =>
            {
                requested.Add(req.RequestUri!.ToString());
                return HtmlResponse(req.RequestUri.ToString() == OkkoAllAlbumsUrl
                    ? Fixture("bedetheque-serie-10529-tout.html")
                    : Fixture("bedetheque-album-41693.html"));
            })));
        var service = CreateService(factory);

        var result = await service.SearchByIsbnAsync(OkkoIsbn, TestContext.Current.CancellationToken);

        // The deluxe printing TL1 shares the ISBN of the volume 1: the regular volume is the album found.
        requested.Should().Equal(OkkoAllAlbumsUrl, OkkoAlbumUrl);
        result.Found.Should().BeTrue();
        result.Title.Should().Be("Le cycle de l'eau I");
    }

    [Fact]
    public async Task SearchByIsbnAsync_Should_ReadThePageOfAllAlbumsOnce_WhenSerpApiFindsSeveralAlbumsAndPagesOfTheSameSerie()
    {
        // The real SerpApi answer for Okko T1: three album pages (the volume and its deluxe printing) and two serie pages.
        var serpJson = SerpApiJson(
            "https://www.bedetheque.com/serie-10529-BD-Okko__1.html",
            OkkoAllAlbumsUrl,
            OkkoAlbumUrl,
            "https://www.bedetheque.com/BD-Okko-Le-cycle-de-l-eau-I-II-53637.html",
            "https://www.bedetheque.com/BD-Okko-TL1-Le-cycle-de-l-eau-I-II-53637.html");
        var requested = new List<string>();
        var factory = FactoryWith(
            Track(new FakeHttpMessageHandler(_ => JsonResponse(serpJson))),
            Track(new FakeHttpMessageHandler(req =>
            {
                requested.Add(req.RequestUri!.ToString());
                return HtmlResponse(req.RequestUri.ToString() == OkkoAllAlbumsUrl
                    ? Fixture("bedetheque-serie-10529-tout.html")
                    : Fixture("bedetheque-album-41693.html"));
            })));
        var service = CreateService(factory);

        var result = await service.SearchByIsbnAsync(OkkoIsbn, TestContext.Current.CancellationToken);

        requested.Should().Equal(OkkoAllAlbumsUrl, OkkoAlbumUrl);
        result.Found.Should().BeTrue();
    }

    [Fact]
    public async Task SearchByIsbnAsync_Should_PreferTheRegularVolume_WhenADeluxePrintingSharingItsIsbnIsListedFirst()
    {
        var serieHtml = $"""
            <html><body>
              <article itemscope itemtype="https://schema.org/Book">
                <a itemprop="url" href="https://www.bedetheque.com/BD-Biguden-TL1-1.html"><span itemprop="name">TL1. Coffret</span></a>
                <span itemprop="isbn">{ValidIsbn}</span>
              </article>
              <article itemscope itemtype="https://schema.org/Book">
                <a itemprop="url" href="{PageUrl}"><span itemprop="name">1<span class="bdt-numa"></span>. L'Ankou</span></a>
                <span itemprop="isbn">{ValidIsbn}</span>
              </article>
            </body></html>
            """;
        var requested = new List<string>();
        var factory = FactoryWith(
            Track(new FakeHttpMessageHandler(_ => JsonResponse(SerpApiJson("https://www.bedetheque.com/serie-123-BD-Biguden.html")))),
            Track(new FakeHttpMessageHandler(req =>
            {
                requested.Add(req.RequestUri!.ToString());
                return HtmlResponse(req.RequestUri.ToString().Contains("serie-", StringComparison.Ordinal) ? serieHtml : FullAlbumHtml);
            })));
        var service = CreateService(factory);

        await service.SearchByIsbnAsync(ValidIsbn, TestContext.Current.CancellationToken);

        requested.Should().EndWith(PageUrl);
    }

    // ── SearchByIsbnAsync — Cloudflare Browser Run ───────────────────

    [Fact]
    public async Task SearchByIsbnAsync_Should_LoadThePageThroughBrowserRun_WhenCloudflareAccountAndTokenAreSet()
    {
        HttpRequestMessage? sent = null;
        string? sentBody = null;
        var directRequests = 0;
        var factory = FactoryWith(
            bedeHandler: Track(new FakeHttpMessageHandler(_ => { directRequests++; return HtmlResponse(FullAlbumHtml); })),
            browserRunHandler: Track(new FakeHttpMessageHandler(req =>
            {
                sent = req;
                sentBody = req.Content!.ReadAsStringAsync(TestContext.Current.CancellationToken).GetAwaiter().GetResult();
                return BrowserRunResponse(FullAlbumHtml);
            })));
        var service = CreateService(factory, CacheWithUrl(ValidIsbn, PageUrl), BrowserRunSettings);

        var result = await service.SearchByIsbnAsync(ValidIsbn, TestContext.Current.CancellationToken);

        result.Found.Should().BeTrue();
        result.Title.Should().Be("L'Ankou");
        directRequests.Should().Be(0);
        sent!.Method.Should().Be(HttpMethod.Post);
        sent.RequestUri.Should().Be(new Uri(BrowserRunContentUrl));
        sent.Headers.Authorization!.ToString().Should().Be("Bearer api-token");
        using var body = JsonDocument.Parse(sentBody!);
        body.RootElement.GetProperty("url").GetString().Should().Be(PageUrl);
        body.RootElement.GetProperty("gotoOptions").GetProperty("waitUntil").GetString().Should().Be("domcontentloaded");
    }

    [Fact]
    public async Task SearchByIsbnAsync_Should_ReturnFailed_WhenBrowserRunAnswersWithAnError()
    {
        var factory = FactoryWith(browserRunHandler: Track(new FakeHttpMessageHandler(_ =>
        {
            var response = JsonResponse("""{"success":false,"errors":[{"code":6002,"message":"A timeout was reached."}]}""");
            response.StatusCode = HttpStatusCode.BadRequest;
            return response;
        })));
        var circuit = new BedethequeCircuit(TimeProvider.System);
        var service = CreateService(factory, CacheWithUrl(ValidIsbn, PageUrl), BrowserRunSettings, circuit);

        var result = await service.SearchByIsbnAsync(ValidIsbn, TestContext.Current.CancellationToken);

        result.Failed.Should().BeTrue();
        circuit.PausedUntil.Should().BeNull("a Browser Run error is not a Cloudflare challenge of Bedetheque");
    }

    [Fact]
    public async Task SearchByIsbnAsync_Should_RetryOnce_WhenBrowserRunRateLimitIsReached()
    {
        var calls = 0;
        var factory = FactoryWith(browserRunHandler: Track(new FakeHttpMessageHandler(_ =>
        {
            calls++;
            if (calls > 1)
            {
                return BrowserRunResponse(FullAlbumHtml);
            }
            var tooMany = JsonResponse("""{"success":false,"errors":[{"code":2001,"message":"Rate limit exceeded"}]}""");
            tooMany.StatusCode = HttpStatusCode.TooManyRequests;
            tooMany.Headers.RetryAfter = new System.Net.Http.Headers.RetryConditionHeaderValue(TimeSpan.Zero);
            return tooMany;
        })));
        var service = CreateService(factory, CacheWithUrl(ValidIsbn, PageUrl), BrowserRunSettings);

        var result = await service.SearchByIsbnAsync(ValidIsbn, TestContext.Current.CancellationToken);

        calls.Should().Be(2);
        result.Found.Should().BeTrue();
    }

    [Fact]
    public async Task SearchByIsbnAsync_Should_PauseBedetheque_WhenBrowserRunGetsACloudflareChallenge()
    {
        var challenge = "<html><head><title>Just a moment...</title></head><body>Checking your browser - cloudflare</body></html>";
        var factory = FactoryWith(browserRunHandler: Track(new FakeHttpMessageHandler(_ => BrowserRunResponse(challenge))));
        var circuit = new BedethequeCircuit(TimeProvider.System);
        var service = CreateService(factory, CacheWithUrl(ValidIsbn, PageUrl), BrowserRunSettings, circuit);

        var result = await service.SearchByIsbnAsync(ValidIsbn, TestContext.Current.CancellationToken);

        result.Failed.Should().BeTrue();
        circuit.PausedUntil.Should().NotBeNull();
    }

    // ── SearchByTextAsync ────────────────────────────────────────────

    private static readonly BookTextQuery s_bigudenQuery = new("L'Ankou", "Biguden", 1);

    private static readonly string AlbumWithIsbnHtml = FullAlbumHtml.Replace("<dt>Planches</dt>", "<dt>EAN/ISBN</dt><dd>978-2-205-05731-7</dd><dt>Planches</dt>", StringComparison.Ordinal);

    [Fact]
    public async Task SearchByTextAsync_Should_ReadTheAlbumPagesFoundByGoogle_WhenTheSerieIsOnBedetheque()
    {
        var cache = EmptyCache();
        var service = CreateService(SerpAndPageFactory(SerpApiJson(PageUrl, OkkoSerieUrl), AlbumWithIsbnHtml), cache);

        var result = await service.SearchByTextAsync(s_bigudenQuery, TestContext.Current.CancellationToken);

        result.Candidates.Should().BeEquivalentTo(new BookCandidate[]
        {
            new("L'Ankou", "Biguden", 1, ["EP Media"], new DateOnly(2014, 8, 27), ValidIsbn, new Uri(ExpectedCoverUrl)),
        });
        await cache.Received(1).SaveAsync(ValidIsbn, PageUrl, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SearchByTextAsync_Should_ReadTheFirstThreeAlbumPages_WhenGoogleFindsMore()
    {
        var pagesRead = 0;
        var cache = CacheWithUrl(ValidIsbn, PageUrl);
        var factory = FactoryWith(
            Track(new FakeHttpMessageHandler(_ => JsonResponse(SerpApiJson(
                "https://www.bedetheque.com/BD-A-1.html", "https://www.bedetheque.com/BD-B-2.html",
                "https://www.bedetheque.com/BD-C-3.html", "https://www.bedetheque.com/BD-D-4.html")))),
            Track(new FakeHttpMessageHandler(_ =>
            {
                pagesRead++;
                return HtmlResponse(AlbumWithIsbnHtml);
            })));
        var service = CreateService(factory, cache);

        var result = await service.SearchByTextAsync(s_bigudenQuery, TestContext.Current.CancellationToken);

        pagesRead.Should().Be(3);
        result.Candidates.Should().HaveCount(3);
        await cache.DidNotReceive().SaveAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SearchByTextAsync_Should_SkipThePages_WhenTheyHoldNoAlbum()
    {
        var service = CreateService(SerpAndPageFactory(SerpApiJson(PageUrl), "<html><body></body></html>"));

        var result = await service.SearchByTextAsync(s_bigudenQuery, TestContext.Current.CancellationToken);

        result.Found.Should().BeFalse();
        result.Failed.Should().BeFalse();
    }

    [Fact]
    public async Task SearchByTextAsync_Should_ReturnFailed_WhenTheAlbumPagesAreBlockedByCloudflare()
    {
        var circuit = new BedethequeCircuit(TimeProvider.System);
        var service = CreateService(SerpAndPageFactory(SerpApiJson(PageUrl), "<title>Just a moment...</title> cloudflare"), circuit: circuit);

        var result = await service.SearchByTextAsync(s_bigudenQuery, TestContext.Current.CancellationToken);

        result.Failed.Should().BeTrue();
        circuit.PausedUntil.Should().NotBeNull();
    }

    [Fact]
    public async Task SearchByTextAsync_Should_ReturnFailed_WhenBedethequeIsPaused()
    {
        var circuit = new BedethequeCircuit(TimeProvider.System);
        circuit.Pause(TimeSpan.FromHours(1));
        var factory = SerpAndPageFactory(SerpApiJson(PageUrl));
        var service = CreateService(factory, circuit: circuit);

        var result = await service.SearchByTextAsync(s_bigudenQuery, TestContext.Current.CancellationToken);

        result.Failed.Should().BeTrue();
        factory.DidNotReceive().CreateClient("SerpApi");
    }

    [Fact]
    public async Task SearchByTextAsync_Should_ReturnFailed_WhenSerpApiAnswersAnError()
    {
        var factory = FactoryWith(Track(new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.TooManyRequests))));
        var service = CreateService(factory);

        var result = await service.SearchByTextAsync(s_bigudenQuery, TestContext.Current.CancellationToken);

        result.Failed.Should().BeTrue();
    }

    [Theory]
    [InlineData("network")]
    [InlineData("timeout")]
    [InlineData("json")]
    public async Task SearchByTextAsync_Should_ReturnFailed_WhenSerpApiCannotBeRead(string failure)
    {
        var factory = FactoryWith(Track(failure switch
        {
            "network" => new FakeHttpMessageHandler(new HttpRequestException("Network error")),
            "timeout" => new FakeHttpMessageHandler(new TaskCanceledException("Timeout")),
            _ => new FakeHttpMessageHandler(_ => JsonResponse("{ not json")),
        }));
        var service = CreateService(factory);

        var result = await service.SearchByTextAsync(s_bigudenQuery, TestContext.Current.CancellationToken);

        result.Failed.Should().BeTrue();
    }

    [Fact]
    public async Task SearchByTextAsync_Should_SearchNothing_WhenNoSerpApiKeyIsConfigured()
    {
        var factory = SerpAndPageFactory(SerpApiJson(PageUrl));
        var service = CreateService(factory, settings: new BedethequeSettings { SerpApiKey = "", BaseUrl = new Uri("https://www.bedetheque.com"), SerpApiBaseUrl = new Uri("https://serpapi.com") });

        var result = await service.SearchByTextAsync(s_bigudenQuery, TestContext.Current.CancellationToken);

        result.Should().Be(BookCandidatesResult.None);
        factory.DidNotReceive().CreateClient("SerpApi");
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task SearchByTextAsync_Should_SearchNothing_WhenTheQueryIsEmptyOrBedethequeIsTurnedOff(bool enabled)
    {
        var factory = SerpAndPageFactory(SerpApiJson(PageUrl));
        var service = CreateService(factory, enabled: enabled);

        var result = await service.SearchByTextAsync(enabled ? new BookTextQuery(null, null, 1) : s_bigudenQuery, TestContext.Current.CancellationToken);

        result.Should().Be(BookCandidatesResult.None);
        factory.DidNotReceive().CreateClient("SerpApi");
    }

    // ── Handler ──────────────────────────────────────────────────────

    private sealed class FakeHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage>? _factory;
        private readonly Exception? _exception;

        public FakeHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> factory)
            => _factory = factory;

        public FakeHttpMessageHandler(Exception exception)
            => _exception = exception;

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            if (_exception is not null)
            {
                throw _exception;
            }

            return Task.FromResult(_factory!(request));
        }
    }
}
