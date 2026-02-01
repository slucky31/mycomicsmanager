using System.Net;
using Application.ComicInfoSearch;

namespace Application.UnitTests.ComicInfoSearch;

public class OpenLibraryServiceTests
{
    private const string ValidIsbn = "9782205089165";
    private const string ValidIsbnWithDashes = "978-2-205-08916-5";

    [Fact]
    public async Task SearchByIsbnAsync_Should_ReturnBookResult_WhenBookFound()
    {
        // Arrange
        var jsonResponse = """
        {
            "title": "Le Jardin secret - Tome 1",
            "authors": [{"key": "/authors/OL123A"}],
            "publishers": ["DARGAUD"],
            "publish_date": "Apr 23, 2021",
            "number_of_pages": 96,
            "covers": [10874957]
        }
        """;

        var authorResponse = """{"name": "Test Author"}""";

        using var handler = new MockHttpMessageHandler(new Dictionary<string, HttpResponseMessage>
        {
            [$"https://openlibrary.org/isbn/{ValidIsbn}.json"] = CreateJsonResponse(jsonResponse),
            ["https://openlibrary.org/authors/OL123A.json"] = CreateJsonResponse(authorResponse)
        });

        using var httpClient = new HttpClient(handler);
        var service = new OpenLibraryService(httpClient);

        // Act
        var result = await service.SearchByIsbnAsync(ValidIsbn);

        // Assert
        result.Found.Should().BeTrue();
        result.Title.Should().Be("Le Jardin secret - Tome 1");
        result.Authors.Should().ContainSingle().Which.Should().Be("Test Author");
        result.Publishers.Should().ContainSingle().Which.Should().Be("DARGAUD");
        result.PublishDate.Should().Be("Apr 23, 2021");
        result.NumberOfPages.Should().Be(96);
        result.CoverUrl.Should().NotBeNull();
        result.CoverUrl!.ToString().Should().Be("https://covers.openlibrary.org/b/id/10874957-L.jpg");
    }

    [Fact]
    public async Task SearchByIsbnAsync_Should_CleanIsbn_WhenIsbnContainsDashesAndSpaces()
    {
        // Arrange
        var jsonResponse = """{"title": "Test Book", "covers": [123]}""";

        using var handler = new MockHttpMessageHandler(new Dictionary<string, HttpResponseMessage>
        {
            [$"https://openlibrary.org/isbn/{ValidIsbn}.json"] = CreateJsonResponse(jsonResponse)
        });

        using var httpClient = new HttpClient(handler);
        var service = new OpenLibraryService(httpClient);

        // Act
        var result = await service.SearchByIsbnAsync(ValidIsbnWithDashes);

        // Assert
        result.Found.Should().BeTrue();
        result.Title.Should().Be("Test Book");
    }

    [Fact]
    public async Task SearchByIsbnAsync_Should_ReturnNotFound_WhenApiReturns404()
    {
        // Arrange
        using var handler = new MockHttpMessageHandler(new Dictionary<string, HttpResponseMessage>
        {
            [$"https://openlibrary.org/isbn/{ValidIsbn}.json"] = new HttpResponseMessage(HttpStatusCode.NotFound)
        });

        using var httpClient = new HttpClient(handler);
        var service = new OpenLibraryService(httpClient);

        // Act
        var result = await service.SearchByIsbnAsync(ValidIsbn);

        // Assert
        result.Found.Should().BeFalse();
        result.Title.Should().BeEmpty();
        result.CoverUrl.Should().BeNull();
    }

    [Fact]
    public async Task SearchByIsbnAsync_Should_ReturnNotFound_WhenHttpRequestFails()
    {
        // Arrange
        using var handler = new MockHttpMessageHandler(
            new HttpRequestException("Network error"));

        using var httpClient = new HttpClient(handler);
        var service = new OpenLibraryService(httpClient);

        // Act
        var result = await service.SearchByIsbnAsync(ValidIsbn);

        // Assert
        result.Found.Should().BeFalse();
    }

    [Fact]
    public async Task SearchByIsbnAsync_Should_ReturnNotFound_WhenJsonIsInvalid()
    {
        // Arrange
        using var handler = new MockHttpMessageHandler(new Dictionary<string, HttpResponseMessage>
        {
            [$"https://openlibrary.org/isbn/{ValidIsbn}.json"] = CreateJsonResponse("invalid json {{{")
        });

        using var httpClient = new HttpClient(handler);
        var service = new OpenLibraryService(httpClient);

        // Act
        var result = await service.SearchByIsbnAsync(ValidIsbn);

        // Assert
        result.Found.Should().BeFalse();
    }

    [Fact]
    public async Task SearchByIsbnAsync_Should_ReturnResultWithoutCover_WhenNoCoversInResponse()
    {
        // Arrange
        var jsonResponse = """{"title": "Book Without Cover", "publishers": ["Publisher"]}""";

        using var handler = new MockHttpMessageHandler(new Dictionary<string, HttpResponseMessage>
        {
            [$"https://openlibrary.org/isbn/{ValidIsbn}.json"] = CreateJsonResponse(jsonResponse)
        });

        using var httpClient = new HttpClient(handler);
        var service = new OpenLibraryService(httpClient);

        // Act
        var result = await service.SearchByIsbnAsync(ValidIsbn);

        // Assert
        result.Found.Should().BeTrue();
        result.Title.Should().Be("Book Without Cover");
        result.CoverUrl.Should().BeNull();
    }

    [Fact]
    public async Task SearchByIsbnAsync_Should_ReturnResultWithoutAuthors_WhenNoAuthorsInResponse()
    {
        // Arrange
        var jsonResponse = """{"title": "Book Without Authors", "covers": [123]}""";

        using var handler = new MockHttpMessageHandler(new Dictionary<string, HttpResponseMessage>
        {
            [$"https://openlibrary.org/isbn/{ValidIsbn}.json"] = CreateJsonResponse(jsonResponse)
        });

        using var httpClient = new HttpClient(handler);
        var service = new OpenLibraryService(httpClient);

        // Act
        var result = await service.SearchByIsbnAsync(ValidIsbn);

        // Assert
        result.Found.Should().BeTrue();
        result.Authors.Should().BeEmpty();
    }

    [Fact]
    public async Task SearchByIsbnAsync_Should_ReturnMultipleAuthors_WhenMultipleAuthorsInResponse()
    {
        // Arrange
        var jsonResponse = """
        {
            "title": "Multi-Author Book",
            "authors": [{"key": "/authors/OL1A"}, {"key": "/authors/OL2A"}],
            "covers": [123]
        }
        """;

        using var handler = new MockHttpMessageHandler(new Dictionary<string, HttpResponseMessage>
        {
            [$"https://openlibrary.org/isbn/{ValidIsbn}.json"] = CreateJsonResponse(jsonResponse),
            ["https://openlibrary.org/authors/OL1A.json"] = CreateJsonResponse("""{"name": "Author One"}"""),
            ["https://openlibrary.org/authors/OL2A.json"] = CreateJsonResponse("""{"name": "Author Two"}""")
        });

        using var httpClient = new HttpClient(handler);
        var service = new OpenLibraryService(httpClient);

        // Act
        var result = await service.SearchByIsbnAsync(ValidIsbn);

        // Assert
        result.Found.Should().BeTrue();
        result.Authors.Should().HaveCount(2);
        result.Authors.Should().Contain("Author One");
        result.Authors.Should().Contain("Author Two");
    }

    [Fact]
    public async Task SearchByIsbnAsync_Should_ContinueWithOtherAuthors_WhenOneAuthorFetchFails()
    {
        // Arrange
        var jsonResponse = """
        {
            "title": "Book",
            "authors": [{"key": "/authors/OL1A"}, {"key": "/authors/OL2A"}],
            "covers": [123]
        }
        """;

        using var handler = new MockHttpMessageHandler(new Dictionary<string, HttpResponseMessage>
        {
            [$"https://openlibrary.org/isbn/{ValidIsbn}.json"] = CreateJsonResponse(jsonResponse),
            ["https://openlibrary.org/authors/OL1A.json"] = new HttpResponseMessage(HttpStatusCode.NotFound),
            ["https://openlibrary.org/authors/OL2A.json"] = CreateJsonResponse("""{"name": "Author Two"}""")
        });

        using var httpClient = new HttpClient(handler);
        var service = new OpenLibraryService(httpClient);

        // Act
        var result = await service.SearchByIsbnAsync(ValidIsbn);

        // Assert
        result.Found.Should().BeTrue();
        result.Authors.Should().ContainSingle().Which.Should().Be("Author Two");
    }

    [Fact]
    public async Task SearchByIsbnAsync_Should_ReturnSubtitle_WhenSubtitleExists()
    {
        // Arrange
        var jsonResponse = """
        {
            "title": "Main Title",
            "subtitle": "A Subtitle",
            "covers": [123]
        }
        """;

        using var handler = new MockHttpMessageHandler(new Dictionary<string, HttpResponseMessage>
        {
            [$"https://openlibrary.org/isbn/{ValidIsbn}.json"] = CreateJsonResponse(jsonResponse)
        });

        using var httpClient = new HttpClient(handler);
        var service = new OpenLibraryService(httpClient);

        // Act
        var result = await service.SearchByIsbnAsync(ValidIsbn);

        // Assert
        result.Found.Should().BeTrue();
        result.Title.Should().Be("Main Title");
        result.Subtitle.Should().Be("A Subtitle");
    }

    [Fact]
    public async Task SearchByIsbnAsync_Should_HandleCancellation()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        using var handler = new MockHttpMessageHandler(new Dictionary<string, HttpResponseMessage>
        {
            [$"https://openlibrary.org/isbn/{ValidIsbn}.json"] = CreateJsonResponse("{}")
        });

        using var httpClient = new HttpClient(handler);
        var service = new OpenLibraryService(httpClient);

        // Act & Assert
        await Assert.ThrowsAsync<TaskCanceledException>(
            () => service.SearchByIsbnAsync(ValidIsbn, cts.Token));
    }

    private static HttpResponseMessage CreateJsonResponse(string json)
    {
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
        };
    }

    private sealed class MockHttpMessageHandler : HttpMessageHandler
    {
        private readonly Dictionary<string, HttpResponseMessage>? _responses;
        private readonly Exception? _exception;

        public MockHttpMessageHandler(Dictionary<string, HttpResponseMessage> responses)
        {
            _responses = responses;
        }

        public MockHttpMessageHandler(Exception exception)
        {
            _exception = exception;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (_exception != null)
            {
                throw _exception;
            }

            var url = request.RequestUri?.ToString() ?? string.Empty;

            if (_responses != null && _responses.TryGetValue(url, out var response))
            {
                return Task.FromResult(response);
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        }
    }
}
