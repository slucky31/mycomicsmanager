using System.Net;
using Application.ComicInfoSearch;

namespace Application.UnitTests.ComicInfoSearch;

public class GoogleBooksServiceTests
{
    private const string ValidIsbn = "9782205089165";
    private const string ValidIsbnWithDashes = "978-2-205-08916-5";

    [Fact]
    public async Task SearchByIsbnAsync_Should_ReturnBookResult_WhenBookFound()
    {
        // Arrange
        var jsonResponse = """
        {
            "totalItems": 1,
            "items": [{
                "volumeInfo": {
                    "title": "Le Jardin secret - Tome 1",
                    "subtitle": "Le jardin de Gaston",
                    "authors": ["Jim", "Pierre Brochard"],
                    "publisher": "DARGAUD",
                    "publishedDate": "2021-04-23",
                    "description": "A beautiful comic about gardens.",
                    "pageCount": 96,
                    "categories": ["Comics & Graphic Novels"],
                    "imageLinks": {
                        "smallThumbnail": "http://books.google.com/small.jpg",
                        "thumbnail": "http://books.google.com/thumb.jpg"
                    },
                    "language": "fr"
                }
            }]
        }
        """;

        using var handler = new MockHttpMessageHandler(new Dictionary<string, HttpResponseMessage>
        {
            [$"https://www.googleapis.com/books/v1/volumes?q=isbn:{ValidIsbn}"] = CreateJsonResponse(jsonResponse)
        });

        using var httpClient = new HttpClient(handler);
        var service = new GoogleBooksService(httpClient);

        // Act
        var result = await service.SearchByIsbnAsync(ValidIsbn);

        // Assert
        result.Found.Should().BeTrue();
        result.Title.Should().Be("Le Jardin secret - Tome 1");
        result.Subtitle.Should().Be("Le jardin de Gaston");
        result.Authors.Should().HaveCount(2);
        result.Authors.Should().Contain("Jim");
        result.Authors.Should().Contain("Pierre Brochard");
        result.Publishers.Should().ContainSingle().Which.Should().Be("DARGAUD");
        result.PublishDate.Should().Be("2021-04-23");
        result.NumberOfPages.Should().Be(96);
        result.Description.Should().Be("A beautiful comic about gardens.");
        result.Categories.Should().ContainSingle().Which.Should().Be("Comics & Graphic Novels");
        result.Language.Should().Be("fr");
        result.CoverUrl.Should().NotBeNull();
        result.CoverUrl!.ToString().Should().StartWith("https://");
    }

    [Fact]
    public async Task SearchByIsbnAsync_Should_CleanIsbn_WhenIsbnContainsDashesAndSpaces()
    {
        // Arrange
        var jsonResponse = """
        {
            "totalItems": 1,
            "items": [{
                "volumeInfo": {
                    "title": "Test Book",
                    "imageLinks": { "thumbnail": "http://books.google.com/thumb.jpg" }
                }
            }]
        }
        """;

        using var handler = new MockHttpMessageHandler(new Dictionary<string, HttpResponseMessage>
        {
            [$"https://www.googleapis.com/books/v1/volumes?q=isbn:{ValidIsbn}"] = CreateJsonResponse(jsonResponse)
        });

        using var httpClient = new HttpClient(handler);
        var service = new GoogleBooksService(httpClient);

        // Act
        var result = await service.SearchByIsbnAsync(ValidIsbnWithDashes);

        // Assert
        result.Found.Should().BeTrue();
        result.Title.Should().Be("Test Book");
    }

    [Fact]
    public async Task SearchByIsbnAsync_Should_ReturnNotFound_WhenApiReturnsTotalItemsZero()
    {
        // Arrange
        var jsonResponse = """{"totalItems": 0}""";

        using var handler = new MockHttpMessageHandler(new Dictionary<string, HttpResponseMessage>
        {
            [$"https://www.googleapis.com/books/v1/volumes?q=isbn:{ValidIsbn}"] = CreateJsonResponse(jsonResponse)
        });

        using var httpClient = new HttpClient(handler);
        var service = new GoogleBooksService(httpClient);

        // Act
        var result = await service.SearchByIsbnAsync(ValidIsbn);

        // Assert
        result.Found.Should().BeFalse();
        result.Title.Should().BeEmpty();
        result.CoverUrl.Should().BeNull();
    }

    [Fact]
    public async Task SearchByIsbnAsync_Should_ReturnNotFound_WhenApiReturnsError()
    {
        // Arrange
        using var handler = new MockHttpMessageHandler(new Dictionary<string, HttpResponseMessage>
        {
            [$"https://www.googleapis.com/books/v1/volumes?q=isbn:{ValidIsbn}"] = new HttpResponseMessage(HttpStatusCode.InternalServerError)
        });

        using var httpClient = new HttpClient(handler);
        var service = new GoogleBooksService(httpClient);

        // Act
        var result = await service.SearchByIsbnAsync(ValidIsbn);

        // Assert
        result.Found.Should().BeFalse();
    }

    [Fact]
    public async Task SearchByIsbnAsync_Should_ReturnNotFound_WhenHttpRequestFails()
    {
        // Arrange
        using var handler = new MockHttpMessageHandler(
            new HttpRequestException("Network error"));

        using var httpClient = new HttpClient(handler);
        var service = new GoogleBooksService(httpClient);

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
            [$"https://www.googleapis.com/books/v1/volumes?q=isbn:{ValidIsbn}"] = CreateJsonResponse("invalid json {{{")
        });

        using var httpClient = new HttpClient(handler);
        var service = new GoogleBooksService(httpClient);

        // Act
        var result = await service.SearchByIsbnAsync(ValidIsbn);

        // Assert
        result.Found.Should().BeFalse();
    }

    [Fact]
    public async Task SearchByIsbnAsync_Should_ReturnResultWithoutCover_WhenNoImageLinksInResponse()
    {
        // Arrange
        var jsonResponse = """
        {
            "totalItems": 1,
            "items": [{
                "volumeInfo": {
                    "title": "Book Without Cover",
                    "publisher": "Publisher"
                }
            }]
        }
        """;

        using var handler = new MockHttpMessageHandler(new Dictionary<string, HttpResponseMessage>
        {
            [$"https://www.googleapis.com/books/v1/volumes?q=isbn:{ValidIsbn}"] = CreateJsonResponse(jsonResponse)
        });

        using var httpClient = new HttpClient(handler);
        var service = new GoogleBooksService(httpClient);

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
        var jsonResponse = """
        {
            "totalItems": 1,
            "items": [{
                "volumeInfo": {
                    "title": "Book Without Authors"
                }
            }]
        }
        """;

        using var handler = new MockHttpMessageHandler(new Dictionary<string, HttpResponseMessage>
        {
            [$"https://www.googleapis.com/books/v1/volumes?q=isbn:{ValidIsbn}"] = CreateJsonResponse(jsonResponse)
        });

        using var httpClient = new HttpClient(handler);
        var service = new GoogleBooksService(httpClient);

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
            "totalItems": 1,
            "items": [{
                "volumeInfo": {
                    "title": "Multi-Author Book",
                    "authors": ["Author One", "Author Two", "Author Three"]
                }
            }]
        }
        """;

        using var handler = new MockHttpMessageHandler(new Dictionary<string, HttpResponseMessage>
        {
            [$"https://www.googleapis.com/books/v1/volumes?q=isbn:{ValidIsbn}"] = CreateJsonResponse(jsonResponse)
        });

        using var httpClient = new HttpClient(handler);
        var service = new GoogleBooksService(httpClient);

        // Act
        var result = await service.SearchByIsbnAsync(ValidIsbn);

        // Assert
        result.Found.Should().BeTrue();
        result.Authors.Should().HaveCount(3);
        result.Authors.Should().Contain("Author One");
        result.Authors.Should().Contain("Author Two");
        result.Authors.Should().Contain("Author Three");
    }

    [Fact]
    public async Task SearchByIsbnAsync_Should_HandleCancellation()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        using var handler = new MockHttpMessageHandler(new Dictionary<string, HttpResponseMessage>
        {
            [$"https://www.googleapis.com/books/v1/volumes?q=isbn:{ValidIsbn}"] = CreateJsonResponse("{}")
        });

        using var httpClient = new HttpClient(handler);
        var service = new GoogleBooksService(httpClient);

        // Act & Assert
        await Assert.ThrowsAsync<TaskCanceledException>(
            () => service.SearchByIsbnAsync(ValidIsbn, cts.Token));
    }

    [Fact]
    public async Task SearchByIsbnAsync_Should_PreferLargestImage_WhenMultipleImageSizesAvailable()
    {
        // Arrange
        var jsonResponse = """
        {
            "totalItems": 1,
            "items": [{
                "volumeInfo": {
                    "title": "Book With Images",
                    "imageLinks": {
                        "smallThumbnail": "http://books.google.com/small.jpg",
                        "thumbnail": "http://books.google.com/thumb.jpg",
                        "medium": "http://books.google.com/medium.jpg",
                        "large": "http://books.google.com/large.jpg"
                    }
                }
            }]
        }
        """;

        using var handler = new MockHttpMessageHandler(new Dictionary<string, HttpResponseMessage>
        {
            [$"https://www.googleapis.com/books/v1/volumes?q=isbn:{ValidIsbn}"] = CreateJsonResponse(jsonResponse)
        });

        using var httpClient = new HttpClient(handler);
        var service = new GoogleBooksService(httpClient);

        // Act
        var result = await service.SearchByIsbnAsync(ValidIsbn);

        // Assert
        result.Found.Should().BeTrue();
        result.CoverUrl.Should().NotBeNull();
        result.CoverUrl!.ToString().Should().Contain("large");
        result.CoverUrl.ToString().Should().StartWith("https://");
    }

    [Fact]
    public async Task SearchByIsbnAsync_Should_UpgradeHttpToHttps_ForCoverUrl()
    {
        // Arrange
        var jsonResponse = """
        {
            "totalItems": 1,
            "items": [{
                "volumeInfo": {
                    "title": "Test",
                    "imageLinks": {
                        "thumbnail": "http://books.google.com/thumb.jpg&edge=curl"
                    }
                }
            }]
        }
        """;

        using var handler = new MockHttpMessageHandler(new Dictionary<string, HttpResponseMessage>
        {
            [$"https://www.googleapis.com/books/v1/volumes?q=isbn:{ValidIsbn}"] = CreateJsonResponse(jsonResponse)
        });

        using var httpClient = new HttpClient(handler);
        var service = new GoogleBooksService(httpClient);

        // Act
        var result = await service.SearchByIsbnAsync(ValidIsbn);

        // Assert
        result.CoverUrl.Should().NotBeNull();
        result.CoverUrl!.ToString().Should().StartWith("https://");
        result.CoverUrl.ToString().Should().NotContain("edge=curl");
    }

    [Fact]
    public async Task SearchByIsbnAsync_Should_WrapSinglePublisher_InList()
    {
        // Arrange
        var jsonResponse = """
        {
            "totalItems": 1,
            "items": [{
                "volumeInfo": {
                    "title": "Test",
                    "publisher": "DARGAUD"
                }
            }]
        }
        """;

        using var handler = new MockHttpMessageHandler(new Dictionary<string, HttpResponseMessage>
        {
            [$"https://www.googleapis.com/books/v1/volumes?q=isbn:{ValidIsbn}"] = CreateJsonResponse(jsonResponse)
        });

        using var httpClient = new HttpClient(handler);
        var service = new GoogleBooksService(httpClient);

        // Act
        var result = await service.SearchByIsbnAsync(ValidIsbn);

        // Assert
        result.Found.Should().BeTrue();
        result.Publishers.Should().ContainSingle().Which.Should().Be("DARGAUD");
    }

    [Fact]
    public async Task SearchByIsbnAsync_Should_ReturnEmptyPublishers_WhenNoPublisherInResponse()
    {
        // Arrange
        var jsonResponse = """
        {
            "totalItems": 1,
            "items": [{
                "volumeInfo": {
                    "title": "Test"
                }
            }]
        }
        """;

        using var handler = new MockHttpMessageHandler(new Dictionary<string, HttpResponseMessage>
        {
            [$"https://www.googleapis.com/books/v1/volumes?q=isbn:{ValidIsbn}"] = CreateJsonResponse(jsonResponse)
        });

        using var httpClient = new HttpClient(handler);
        var service = new GoogleBooksService(httpClient);

        // Act
        var result = await service.SearchByIsbnAsync(ValidIsbn);

        // Assert
        result.Found.Should().BeTrue();
        result.Publishers.Should().BeEmpty();
    }

    [Fact]
    public async Task SearchByIsbnAsync_Should_ReturnNotFound_WhenVolumeInfoIsNull()
    {
        // Arrange
        var jsonResponse = """
        {
            "totalItems": 1,
            "items": [{}]
        }
        """;

        using var handler = new MockHttpMessageHandler(new Dictionary<string, HttpResponseMessage>
        {
            [$"https://www.googleapis.com/books/v1/volumes?q=isbn:{ValidIsbn}"] = CreateJsonResponse(jsonResponse)
        });

        using var httpClient = new HttpClient(handler);
        var service = new GoogleBooksService(httpClient);

        // Act
        var result = await service.SearchByIsbnAsync(ValidIsbn);

        // Assert
        result.Found.Should().BeFalse();
    }

    [Fact]
    public async Task SearchByIsbnAsync_Should_ReturnCategories_WhenCategoriesPresent()
    {
        // Arrange
        var jsonResponse = """
        {
            "totalItems": 1,
            "items": [{
                "volumeInfo": {
                    "title": "Test",
                    "categories": ["Comics & Graphic Novels", "Fiction"]
                }
            }]
        }
        """;

        using var handler = new MockHttpMessageHandler(new Dictionary<string, HttpResponseMessage>
        {
            [$"https://www.googleapis.com/books/v1/volumes?q=isbn:{ValidIsbn}"] = CreateJsonResponse(jsonResponse)
        });

        using var httpClient = new HttpClient(handler);
        var service = new GoogleBooksService(httpClient);

        // Act
        var result = await service.SearchByIsbnAsync(ValidIsbn);

        // Assert
        result.Found.Should().BeTrue();
        result.Categories.Should().HaveCount(2);
        result.Categories.Should().Contain("Comics & Graphic Novels");
        result.Categories.Should().Contain("Fiction");
    }

    [Fact]
    public async Task SearchByIsbnAsync_Should_UseDetailedVolumeInfo_WhenSelfLinkIsAvailable()
    {
        // Arrange
        var searchResponse = """
        {
            "totalItems": 1,
            "items": [{
                "selfLink": "https://www.googleapis.com/books/v1/volumes/kOJlRgAACAAJ",
                "volumeInfo": {
                    "title": "Fullmetal Alchemist",
                    "authors": ["Hiromu Arakawa"],
                    "pageCount": 180,
                    "language": "fr"
                }
            }]
        }
        """;

        var detailedResponse = """
        {
            "selfLink": "https://www.googleapis.com/books/v1/volumes/kOJlRgAACAAJ",
            "volumeInfo": {
                "title": "Fullmetal Alchemist Tome 23",
                "authors": ["Hiromu Arakawa"],
                "publisher": "Kurokawa",
                "publishedDate": "2010-04-08",
                "pageCount": 176,
                "language": "fr"
            }
        }
        """;

        using var handler = new MockHttpMessageHandler(new Dictionary<string, HttpResponseMessage>
        {
            [$"https://www.googleapis.com/books/v1/volumes?q=isbn:{ValidIsbn}"] = CreateJsonResponse(searchResponse),
            ["https://www.googleapis.com/books/v1/volumes/kOJlRgAACAAJ"] = CreateJsonResponse(detailedResponse)
        });

        using var httpClient = new HttpClient(handler);
        var service = new GoogleBooksService(httpClient);

        // Act
        var result = await service.SearchByIsbnAsync(ValidIsbn);

        // Assert
        result.Found.Should().BeTrue();
        result.Title.Should().Be("Fullmetal Alchemist Tome 23");
        result.Authors.Should().ContainSingle().Which.Should().Be("Hiromu Arakawa");
        result.Publishers.Should().ContainSingle().Which.Should().Be("Kurokawa");
        result.PublishDate.Should().Be("2010-04-08");
        result.NumberOfPages.Should().Be(176);
        result.Language.Should().Be("fr");
    }

    [Fact]
    public async Task SearchByIsbnAsync_Should_FallbackToSearchData_WhenSelfLinkFetchFails()
    {
        // Arrange
        var searchResponse = """
        {
            "totalItems": 1,
            "items": [{
                "selfLink": "https://www.googleapis.com/books/v1/volumes/kOJlRgAACAAJ",
                "volumeInfo": {
                    "title": "Fullmetal Alchemist",
                    "authors": ["Hiromu Arakawa"],
                    "pageCount": 180,
                    "language": "fr"
                }
            }]
        }
        """;

        using var handler = new MockHttpMessageHandler(new Dictionary<string, HttpResponseMessage>
        {
            [$"https://www.googleapis.com/books/v1/volumes?q=isbn:{ValidIsbn}"] = CreateJsonResponse(searchResponse),
            ["https://www.googleapis.com/books/v1/volumes/kOJlRgAACAAJ"] = new HttpResponseMessage(HttpStatusCode.InternalServerError)
        });

        using var httpClient = new HttpClient(handler);
        var service = new GoogleBooksService(httpClient);

        // Act
        var result = await service.SearchByIsbnAsync(ValidIsbn);

        // Assert
        result.Found.Should().BeTrue();
        result.Title.Should().Be("Fullmetal Alchemist");
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
