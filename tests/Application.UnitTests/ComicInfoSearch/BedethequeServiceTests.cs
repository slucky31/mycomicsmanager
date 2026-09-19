using System.Net;
using Application.ComicInfoSearch;
using Application.Interfaces;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Application.UnitTests.ComicInfoSearch;

public sealed class BedethequeServiceTests : IDisposable
{
    private const string ValidIsbn = "9782205057317";
    private const string ValidIsbnWithDashes = "978-2-205-05731-7";
    private const string PageUrl = "https://www.bedetheque.com/BD-Biguden-Tome-1-LAnkou-224335.html";
    private const string ExpectedCoverUrl = "https://www.bedetheque.com/media/Couvertures/Couv_224335.jpg";

    private static readonly BedethequeSettings DefaultSettings = new()
    {
        SerpApiKey = "test-key",
        SerpApiBaseUrl = new Uri("https://serpapi.com"),
        BaseUrl = new Uri("https://www.bedetheque.com")
    };

    // Minimal full-featured HTML matching the real Bedetheque infos-albums structure
    private const string FullAlbumHtml = """
        <html><body>
        <ul class="infos-albums">
          <li><label>Série : </label>Biguden</li>
          <li><label>Titre : </label>L'Ankou</li>
          <li><label>Tome : </label>1</li>
          <li><label>Scénario :</label><a href="/auteur-1">Silas, Stan</a></li>
          <li><label>Dessin :</label><a href="/auteur-2">Dupont, Jean</a></li>
          <li><label>Dépot légal : </label>08/2014<span class="grise"> (Parution le 27/08/2014)</span></li>
          <li><label>Editeur : </label><span>EP Media</span></li>
          <li><label>Planches :</label><span>62</span></li>
        </ul>
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
        BedethequeSettings? settings = null)
    {
        cache ??= EmptyCache();
        var options = Options.Create(settings ?? DefaultSettings);
        return new BedethequeService(factory, cache, options);
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
        HttpMessageHandler? bedeHandler = null)
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
        return factory;
    }

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
    public async Task SearchByIsbnAsync_Should_ReturnNotFound_WhenPageFetchFails()
    {
        var factory = FactoryWith(
            Track(new FakeHttpMessageHandler(_ => JsonResponse(SerpApiJson(PageUrl)))),
            Track(new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.NotFound))));
        var service = CreateService(factory);

        var result = await service.SearchByIsbnAsync(ValidIsbn, TestContext.Current.CancellationToken);

        result.Found.Should().BeFalse();
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
    public async Task SearchByIsbnAsync_Should_ReturnNotFound_WhenHttpRequestFails()
    {
        var factory = FactoryWith(Track(new FakeHttpMessageHandler(new HttpRequestException("Network error"))));
        var service = CreateService(factory);

        var result = await service.SearchByIsbnAsync(ValidIsbn, TestContext.Current.CancellationToken);

        result.Found.Should().BeFalse();
    }

    [Fact]
    public async Task SearchByIsbnAsync_Should_ReturnNotFound_WhenJsonParsingFails()
    {
        var factory = FactoryWith(Track(new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("invalid json {{{", System.Text.Encoding.UTF8, "application/json")
        })));
        var service = CreateService(factory);

        var result = await service.SearchByIsbnAsync(ValidIsbn, TestContext.Current.CancellationToken);

        result.Found.Should().BeFalse();
    }

    [Fact]
    public async Task SearchByIsbnAsync_Should_ReturnNotFound_WhenTimeoutOccurs()
    {
        // TaskCanceledException without a cancelled caller token → internal timeout
        var factory = FactoryWith(Track(new FakeHttpMessageHandler(new TaskCanceledException("Timeout"))));
        var service = CreateService(factory);

        var result = await service.SearchByIsbnAsync(ValidIsbn, TestContext.Current.CancellationToken);

        result.Found.Should().BeFalse();
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
        var html = """<html><body><ul class="infos-albums"><li><label>Série : </label>OnlySerie</li></ul></body></html>""";
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
        var html = """<html><body><ul class="infos-albums"><li><label>Titre : </label>Album</li></ul></body></html>""";
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
            <html><body><ul class="infos-albums">
              <li><label>Titre : </label>Album</li>
              <li><label>Scénario :</label><a href="/auteur-1">Silas, Stan</a></li>
              <li><label>Dessin :</label><a href="/auteur-1">Silas, Stan</a></li>
            </ul></body></html>
            """;
        var cache = CacheWithUrl(ValidIsbn, PageUrl);
        var result = await CreateService(PageFactory(html), cache).SearchByIsbnAsync(ValidIsbn, TestContext.Current.CancellationToken);

        result.Authors.Should().ContainSingle().Which.Should().Be("Silas, Stan");
    }

    [Fact]
    public async Task SearchByIsbnAsync_Should_ParseMultipleAuthors_WhenMultipleAnchorsUnderScenarioLabel()
    {
        var html = """
            <html><body><ul class="infos-albums">
              <li><label>Titre : </label>Album</li>
              <li><label>Scénario :</label><a href="/auteur-1">Alpha, Jean</a><a href="/auteur-2">Beta, Marie</a></li>
            </ul></body></html>
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
            <html><body><ul class="infos-albums">
              <li><label>Titre : </label>Album</li>
              <li><label>Dessin :</label><a href="/auteur-1">Alpha, Jean</a><a href="/auteur-2">Beta, Marie</a></li>
            </ul></body></html>
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
        var html = """<html><body><ul class="infos-albums"><li><label>Titre : </label>Album</li></ul></body></html>""";
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
        var html = """<html><body><ul class="infos-albums"><li><label>Titre : </label>Album</li></ul></body></html>""";
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
            <html><body><ul class="infos-albums">
              <li><label>Titre : </label>Album</li>
              <li><label>Dépot légal : </label>11/2025</li>
            </ul></body></html>
            """;
        var cache = CacheWithUrl(ValidIsbn, PageUrl);
        var result = await CreateService(PageFactory(html), cache).SearchByIsbnAsync(ValidIsbn, TestContext.Current.CancellationToken);

        result.PublishDate.Should().Be(new DateOnly(2025, 11, 1));
    }

    [Fact]
    public async Task SearchByIsbnAsync_Should_ReturnNullPublishDate_WhenNoDateField()
    {
        var html = """<html><body><ul class="infos-albums"><li><label>Titre : </label>Album</li></ul></body></html>""";
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
        var html = """<html><body><ul class="infos-albums"><li><label>Titre : </label>Album</li></ul></body></html>""";
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
        var html = """<html><body><ul class="infos-albums"><li><label>Série : </label>OnlySerie</li></ul></body></html>""";
        var cache = CacheWithUrl(ValidIsbn, PageUrl);
        var result = await CreateService(PageFactory(html), cache).SearchByIsbnAsync(ValidIsbn, TestContext.Current.CancellationToken);

        result.Found.Should().BeTrue();
        result.Serie.Should().Be("OnlySerie");
        result.Title.Should().BeEmpty();
    }

    [Fact]
    public async Task SearchByIsbnAsync_Should_ReturnFoundFalse_WhenBothTitleAndSerieEmpty()
    {
        var html = """<html><body><ul class="infos-albums"></ul></body></html>""";
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

    [Fact]
    public async Task SearchByIsbnAsync_Should_ReturnFound_WhenSeriePageContainsMatchingIsbn()
    {
        var serieUrl = "https://www.bedetheque.com/serie-Biguden-123.html";
        var serieHtml = $"""
            <html><body><ul>
              <li itemscope itemtype="https://schema.org/Book">
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

    // ── SearchByIsbnAsync — publisher from text node ─────────────────

    [Fact]
    public async Task SearchByIsbnAsync_Should_ParsePublisher_WhenEditeurHasNoSpan()
    {
        var html = """
            <html><body><ul class="infos-albums">
              <li><label>Titre : </label>Album</li>
              <li><label>Editeur : </label>Publisher Without Span</li>
            </ul></body></html>
            """;
        var cache = CacheWithUrl(ValidIsbn, PageUrl);
        var result = await CreateService(PageFactory(html), cache).SearchByIsbnAsync(ValidIsbn, TestContext.Current.CancellationToken);

        result.Publishers.Should().ContainSingle().Which.Should().Be("Publisher Without Span");
    }

    // ── SearchByIsbnAsync — publish date fallback ────────────────────

    [Fact]
    public async Task SearchByIsbnAsync_Should_ParsePublishDate_FromMonthYear_WhenParutionDateIsInvalid()
    {
        var html = """
            <html><body><ul class="infos-albums">
              <li><label>Titre : </label>Album</li>
              <li><label>Dépot légal : </label>06/2020<span class="grise"> (Parution le 00/00/0000)</span></li>
            </ul></body></html>
            """;
        var cache = CacheWithUrl(ValidIsbn, PageUrl);
        var result = await CreateService(PageFactory(html), cache).SearchByIsbnAsync(ValidIsbn, TestContext.Current.CancellationToken);

        result.PublishDate.Should().Be(new DateOnly(2020, 6, 1));
    }

    // ── SearchByIsbnAsync — pages from text node ─────────────────────

    [Fact]
    public async Task SearchByIsbnAsync_Should_ParseNumberOfPages_WhenPlanchesHasNoSpan()
    {
        var html = """
            <html><body><ul class="infos-albums">
              <li><label>Titre : </label>Album</li>
              <li><label>Planches :</label>48</li>
            </ul></body></html>
            """;
        var cache = CacheWithUrl(ValidIsbn, PageUrl);
        var result = await CreateService(PageFactory(html), cache).SearchByIsbnAsync(ValidIsbn, TestContext.Current.CancellationToken);

        result.NumberOfPages.Should().Be(48);
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
