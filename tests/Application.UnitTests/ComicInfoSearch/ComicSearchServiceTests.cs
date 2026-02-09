using Application.ComicInfoSearch;
using Microsoft.Extensions.Options;
using NSubstitute;
using NSubstitute.ExceptionExtensions;


namespace Application.UnitTests.ComicInfoSearch;

public sealed class ComicSearchServiceTests
{
    private static readonly string[] EmptyStringArray = [];
    private static readonly string[] SingleAuthorArray = ["Author"];
    private static readonly string[] SinglePublisherArray = ["Publisher"];

    private readonly IOpenLibraryService _openLibraryService;
    private readonly ICloudinaryService _cloudinaryService;
    private readonly IOptions<CloudinarySettings> _cloudinarySettings;
    private readonly ComicSearchService _sut;

    public ComicSearchServiceTests()
    {
        _openLibraryService = Substitute.For<IOpenLibraryService>();
        _cloudinaryService = Substitute.For<ICloudinaryService>();
        _cloudinarySettings = Options.Create(new CloudinarySettings
        {
            CloudName = "test-cloud",
            ApiKey = "test-key",
            ApiSecret = "test-secret",
            Folder = "test-covers"
        });
        _sut = new ComicSearchService(_openLibraryService, _cloudinaryService, _cloudinarySettings);
    }

    #region SearchByIsbnAsync Tests

    [Fact]
    public async Task SearchByIsbnAsync_ShouldReturnNotFound_WhenOpenLibraryReturnsNotFound()
    {
        // Arrange
        const string isbn = "9781234567890";
        var openLibraryResult = new OpenLibraryBookResult(
            Title: string.Empty,
            Subtitle: null,
            Authors: EmptyStringArray,
            Publishers: EmptyStringArray,
            PublishDate: null,
            NumberOfPages: null,
            CoverUrl: null,
            Found: false);

        _openLibraryService.SearchByIsbnAsync(isbn, Arg.Any<CancellationToken>())
            .Returns(openLibraryResult);

        // Act
        var result = await _sut.SearchByIsbnAsync(isbn);

        // Assert
        result.Found.Should().BeFalse();
        result.Isbn.Should().Be(isbn);
        result.Title.Should().BeEmpty();
        result.Serie.Should().BeEmpty();
        result.VolumeNumber.Should().Be(1);
    }

    [Fact]
    public async Task SearchByIsbnAsync_ShouldReturnSuccess_WhenOpenLibraryReturnsData()
    {
        // Arrange
        const string isbn = "9781234567890";
        const string title = "Soda, tome 1";
        var openLibraryResult = new OpenLibraryBookResult(
            Title: title,
            Subtitle: null,
            Authors: ["Philippe Tome", "Luc Wartholz"],
            Publishers: ["Dupuis"],
            PublishDate: "1987",
            NumberOfPages: 48,
            CoverUrl: new Uri("https://covers.openlibrary.org/b/id/12345-L.jpg"),
            Found: true);

        var cloudinaryResult = new CloudinaryUploadResult(
            Url: new Uri("https://res.cloudinary.com/test/image/upload/v1/test-covers/9781234567890.jpg"),
            PublicId: "test-covers/9781234567890",
            Success: true,
            Error: null);

        _openLibraryService.SearchByIsbnAsync(isbn, Arg.Any<CancellationToken>())
            .Returns(openLibraryResult);
        _cloudinaryService.UploadImageFromUrlAsync(
                Arg.Any<Uri>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>())
            .Returns(cloudinaryResult);

        // Act
        var result = await _sut.SearchByIsbnAsync(isbn);

        // Assert
        result.Found.Should().BeTrue();
        result.Isbn.Should().Be(isbn);
        result.Title.Should().Be(title);
        result.Serie.Should().Be("Soda");
        result.VolumeNumber.Should().Be(1);
        result.Authors.Should().Be("Philippe Tome, Luc Wartholz");
        result.Publishers.Should().Be("Dupuis");
        result.NumberOfPages.Should().Be(48);
        result.ImageUrl.Should().Be("https://res.cloudinary.com/test/image/upload/v1/test-covers/9781234567890.jpg");
        result.PublishDate.Should().Be(new DateOnly(1987, 1, 1));
    }

    [Fact]
    public async Task SearchByIsbnAsync_ShouldUseSubtitleAsTitle_WhenSubtitleIsProvided()
    {
        // Arrange
        const string isbn = "9781234567890";
        const string title = "The Amazing Spider-Man";
        const string subtitle = "Volume 1: Coming Home";
        var openLibraryResult = new OpenLibraryBookResult(
            Title: title,
            Subtitle: subtitle,
            Authors: ["Stan Lee"],
            Publishers: ["Marvel"],
            PublishDate: "2001-01-01",
            NumberOfPages: 120,
            CoverUrl: null,
            Found: true);

        _openLibraryService.SearchByIsbnAsync(isbn, Arg.Any<CancellationToken>())
            .Returns(openLibraryResult);

        // Act
        var result = await _sut.SearchByIsbnAsync(isbn);

        // Assert
        result.Title.Should().Be(subtitle);
    }

    [Fact]
    public async Task SearchByIsbnAsync_ShouldUploadCoverToCloudinary_WhenCoverUrlIsProvided()
    {
        // Arrange
        const string isbn = "978-1234-5678-90";
        var coverUrl = new Uri("https://covers.openlibrary.org/b/id/12345-L.jpg");
        var openLibraryResult = new OpenLibraryBookResult(
            Title: "Test Comic",
            Subtitle: null,
            Authors: SingleAuthorArray,
            Publishers: SinglePublisherArray,
            PublishDate: "2024",
            NumberOfPages: 100,
            CoverUrl: coverUrl,
            Found: true);

        var cloudinaryResult = new CloudinaryUploadResult(
            Url: new Uri("https://res.cloudinary.com/test/uploaded.jpg"),
            PublicId: "test-id",
            Success: true,
            Error: null);

        _openLibraryService.SearchByIsbnAsync(isbn, Arg.Any<CancellationToken>())
            .Returns(openLibraryResult);
        _cloudinaryService.UploadImageFromUrlAsync(
                coverUrl,
                "test-covers",
                "9781234567890",
                Arg.Any<CancellationToken>())
            .Returns(cloudinaryResult);

        // Act
        var result = await _sut.SearchByIsbnAsync(isbn);

        // Assert
        result.ImageUrl.Should().Be("https://res.cloudinary.com/test/uploaded.jpg");
        await _cloudinaryService.Received(1).UploadImageFromUrlAsync(
            coverUrl,
            "test-covers",
            "9781234567890",
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SearchByIsbnAsync_ShouldUseOriginalUrl_WhenCloudinaryUploadFails()
    {
        // Arrange
        const string isbn = "9781234567890";
        var coverUrl = new Uri("https://covers.openlibrary.org/b/id/12345-L.jpg");
        var openLibraryResult = new OpenLibraryBookResult(
            Title: "Test Comic",
            Subtitle: null,
            Authors: SingleAuthorArray,
            Publishers: SinglePublisherArray,
            PublishDate: "2024",
            NumberOfPages: 100,
            CoverUrl: coverUrl,
            Found: true);

        var cloudinaryResult = new CloudinaryUploadResult(
            Url: null,
            PublicId: null,
            Success: false,
            Error: "Upload failed");

        _openLibraryService.SearchByIsbnAsync(isbn, Arg.Any<CancellationToken>())
            .Returns(openLibraryResult);
        _cloudinaryService.UploadImageFromUrlAsync(
                Arg.Any<Uri>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>())
            .Returns(cloudinaryResult);

        // Act
        var result = await _sut.SearchByIsbnAsync(isbn);

        // Assert
        result.ImageUrl.Should().Be(coverUrl.ToString());
    }

    [Fact]
    public async Task SearchByIsbnAsync_ShouldReturnEmptyImageUrl_WhenNoCoverUrlProvided()
    {
        // Arrange
        const string isbn = "9781234567890";
        var openLibraryResult = new OpenLibraryBookResult(
            Title: "Test Comic",
            Subtitle: null,
            Authors: SingleAuthorArray,
            Publishers: SinglePublisherArray,
            PublishDate: "2024",
            NumberOfPages: 100,
            CoverUrl: null,
            Found: true);

        _openLibraryService.SearchByIsbnAsync(isbn, Arg.Any<CancellationToken>())
            .Returns(openLibraryResult);

        // Act
        var result = await _sut.SearchByIsbnAsync(isbn);

        // Assert
        result.ImageUrl.Should().BeEmpty();
        await _cloudinaryService.DidNotReceive().UploadImageFromUrlAsync(
            Arg.Any<Uri>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SearchByIsbnAsync_ShouldReturnNotFound_WhenHttpRequestExceptionIsThrown()
    {
        // Arrange
        const string isbn = "9781234567890";
        _openLibraryService.SearchByIsbnAsync(isbn, Arg.Any<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Network error"));

        // Act
        var result = await _sut.SearchByIsbnAsync(isbn);

        // Assert
        result.Found.Should().BeFalse();
        result.Isbn.Should().Be(isbn);
    }

    [Fact]
    public async Task SearchByIsbnAsync_ShouldReturnNotFound_WhenInvalidOperationExceptionIsThrown()
    {
        // Arrange
        const string isbn = "9781234567890";
        _openLibraryService.SearchByIsbnAsync(isbn, Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("Invalid operation"));

        // Act
        var result = await _sut.SearchByIsbnAsync(isbn);

        // Assert
        result.Found.Should().BeFalse();
        result.Isbn.Should().Be(isbn);
    }

    [Fact]
    public async Task SearchByIsbnAsync_ShouldReturnNotFound_WhenTimeoutOccurs()
    {
        // Arrange
        const string isbn = "9781234567890";
        _openLibraryService.SearchByIsbnAsync(isbn, Arg.Any<CancellationToken>())
            .ThrowsAsync(new TaskCanceledException("Request timeout"));

        // Act
        var result = await _sut.SearchByIsbnAsync(isbn);

        // Assert
        result.Found.Should().BeFalse();
        result.Isbn.Should().Be(isbn);
    }

    #endregion

    #region ParseVolumeAndSerie Tests

    [Theory]
    [InlineData("Soda, tome 1", "Soda", 1)]
    [InlineData("Soda, tome 12", "Soda", 12)]
    [InlineData("Tintin - tome 3", "Tintin", 3)]
    [InlineData("Asterix - tome 24", "Asterix", 24)]
    [InlineData("Spider-Man, vol. 5", "Spider-Man", 5)]
    [InlineData("Batman, vol 10", "Batman", 10)]
    [InlineData("Superman - vol. 2", "Superman", 2)]
    [InlineData("X-Men vol. 1", "X-Men", 1)]
    [InlineData("Avengers vol 3", "Avengers", 3)]
    [InlineData("Naruto #15", "Naruto", 15)]
    public async Task SearchByIsbnAsync_ShouldParseVolumeAndSerie_WithVariousFormats(
        string title,
        string expectedSerie,
        int expectedVolume)
    {
        // Arrange
        const string isbn = "9781234567890";
        var openLibraryResult = new OpenLibraryBookResult(
            Title: title,
            Subtitle: null,
            Authors: SingleAuthorArray,
            Publishers: SinglePublisherArray,
            PublishDate: "2024",
            NumberOfPages: 100,
            CoverUrl: null,
            Found: true);

        _openLibraryService.SearchByIsbnAsync(isbn, Arg.Any<CancellationToken>())
            .Returns(openLibraryResult);

        // Act
        var result = await _sut.SearchByIsbnAsync(isbn);

        // Assert
        result.Serie.Should().Be(expectedSerie);
        result.VolumeNumber.Should().Be(expectedVolume);
    }

    [Fact]
    public async Task SearchByIsbnAsync_ShouldUseFullTitleAsSerie_WhenNoVolumePatternMatches()
    {
        // Arrange
        const string isbn = "9781234567890";
        const string title = "The Watchmen";
        var openLibraryResult = new OpenLibraryBookResult(
            Title: title,
            Subtitle: null,
            Authors: ["Alan Moore"],
            Publishers: ["DC Comics"],
            PublishDate: "1987",
            NumberOfPages: 416,
            CoverUrl: null,
            Found: true);

        _openLibraryService.SearchByIsbnAsync(isbn, Arg.Any<CancellationToken>())
            .Returns(openLibraryResult);

        // Act
        var result = await _sut.SearchByIsbnAsync(isbn);

        // Assert
        result.Serie.Should().Be(title);
        result.VolumeNumber.Should().Be(1);
    }

    #endregion

    #region ParsePublishDate Tests

    [Theory]
    [InlineData("September 16, 1987", 1987, 9, 16)]
    [InlineData("Sep 16, 1987", 1987, 9, 16)]
    [InlineData("1987-09-16", 1987, 9, 16)]
    [InlineData("1987/09/16", 1987, 9, 16)]
    [InlineData("16/09/1987", 1987, 9, 16)]
    [InlineData("September 1987", 1987, 9, 1)]
    [InlineData("Sep 1987", 1987, 9, 1)]
    [InlineData("1987", 1987, 1, 1)]
    public async Task SearchByIsbnAsync_ShouldParsePublishDate_WithVariousFormats(
        string publishDate,
        int expectedYear,
        int expectedMonth,
        int expectedDay)
    {
        // Arrange
        const string isbn = "9781234567890";
        var openLibraryResult = new OpenLibraryBookResult(
            Title: "Test Comic",
            Subtitle: null,
            Authors: SingleAuthorArray,
            Publishers: SinglePublisherArray,
            PublishDate: publishDate,
            NumberOfPages: 100,
            CoverUrl: null,
            Found: true);

        _openLibraryService.SearchByIsbnAsync(isbn, Arg.Any<CancellationToken>())
            .Returns(openLibraryResult);

        // Act
        var result = await _sut.SearchByIsbnAsync(isbn);

        // Assert
        result.PublishDate.Should().Be(new DateOnly(expectedYear, expectedMonth, expectedDay));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task SearchByIsbnAsync_ShouldReturnNullPublishDate_WhenDateStringIsNullOrEmpty(string? publishDate)
    {
        // Arrange
        const string isbn = "9781234567890";
        var openLibraryResult = new OpenLibraryBookResult(
            Title: "Test Comic",
            Subtitle: null,
            Authors: SingleAuthorArray,
            Publishers: SinglePublisherArray,
            PublishDate: publishDate,
            NumberOfPages: 100,
            CoverUrl: null,
            Found: true);

        _openLibraryService.SearchByIsbnAsync(isbn, Arg.Any<CancellationToken>())
            .Returns(openLibraryResult);

        // Act
        var result = await _sut.SearchByIsbnAsync(isbn);

        // Assert
        result.PublishDate.Should().BeNull();
    }

    [Fact]
    public async Task SearchByIsbnAsync_ShouldReturnNullPublishDate_WhenDateStringIsInvalid()
    {
        // Arrange
        const string isbn = "9781234567890";
        var openLibraryResult = new OpenLibraryBookResult(
            Title: "Test Comic",
            Subtitle: null,
            Authors: SingleAuthorArray,
            Publishers: SinglePublisherArray,
            PublishDate: "invalid-date",
            NumberOfPages: 100,
            CoverUrl: null,
            Found: true);

        _openLibraryService.SearchByIsbnAsync(isbn, Arg.Any<CancellationToken>())
            .Returns(openLibraryResult);

        // Act
        var result = await _sut.SearchByIsbnAsync(isbn);

        // Assert
        result.PublishDate.Should().BeNull();
    }

    #endregion

    #region Multiple Authors and Publishers Tests

    [Fact]
    public async Task SearchByIsbnAsync_ShouldCombineMultipleAuthors_AsCommaSeparatedString()
    {
        // Arrange
        const string isbn = "9781234567890";
        var openLibraryResult = new OpenLibraryBookResult(
            Title: "Test Comic",
            Subtitle: null,
            Authors: ["Stan Lee", "Jack Kirby", "Steve Ditko"],
            Publishers: SinglePublisherArray,
            PublishDate: "2024",
            NumberOfPages: 100,
            CoverUrl: null,
            Found: true);

        _openLibraryService.SearchByIsbnAsync(isbn, Arg.Any<CancellationToken>())
            .Returns(openLibraryResult);

        // Act
        var result = await _sut.SearchByIsbnAsync(isbn);

        // Assert
        result.Authors.Should().Be("Stan Lee, Jack Kirby, Steve Ditko");
    }

    [Fact]
    public async Task SearchByIsbnAsync_ShouldCombineMultiplePublishers_AsCommaSeparatedString()
    {
        // Arrange
        const string isbn = "9781234567890";
        var openLibraryResult = new OpenLibraryBookResult(
            Title: "Test Comic",
            Subtitle: null,
            Authors: SingleAuthorArray,
            Publishers: ["Marvel Comics", "DC Comics", "Image Comics"],
            PublishDate: "2024",
            NumberOfPages: 100,
            CoverUrl: null,
            Found: true);

        _openLibraryService.SearchByIsbnAsync(isbn, Arg.Any<CancellationToken>())
            .Returns(openLibraryResult);

        // Act
        var result = await _sut.SearchByIsbnAsync(isbn);

        // Assert
        result.Publishers.Should().Be("Marvel Comics, DC Comics, Image Comics");
    }

    [Fact]
    public async Task SearchByIsbnAsync_ShouldHandleEmptyAuthorsAndPublishers()
    {
        // Arrange
        const string isbn = "9781234567890";
        var openLibraryResult = new OpenLibraryBookResult(
            Title: "Test Comic",
            Subtitle: null,
            Authors: EmptyStringArray,
            Publishers: EmptyStringArray,
            PublishDate: "2024",
            NumberOfPages: 100,
            CoverUrl: null,
            Found: true);

        _openLibraryService.SearchByIsbnAsync(isbn, Arg.Any<CancellationToken>())
            .Returns(openLibraryResult);

        // Act
        var result = await _sut.SearchByIsbnAsync(isbn);

        // Assert
        result.Authors.Should().BeEmpty();
        result.Publishers.Should().BeEmpty();
    }

    #endregion

    #region CancellationToken Tests

    [Fact]
    public async Task SearchByIsbnAsync_ShouldPassCancellationToken_ToOpenLibraryService()
    {
        // Arrange
        const string isbn = "9781234567890";
        using var cts = new CancellationTokenSource();
        var cancellationToken = cts.Token;

        var openLibraryResult = new OpenLibraryBookResult(
            Title: "Test",
            Subtitle: null,
            Authors: EmptyStringArray,
            Publishers: EmptyStringArray,
            PublishDate: null,
            NumberOfPages: null,
            CoverUrl: null,
            Found: true);

        _openLibraryService.SearchByIsbnAsync(isbn, cancellationToken)
            .Returns(openLibraryResult);

        // Act
        await _sut.SearchByIsbnAsync(isbn, cancellationToken);

        // Assert
        await _openLibraryService.Received(1).SearchByIsbnAsync(isbn, cancellationToken);
    }

    [Fact]
    public async Task SearchByIsbnAsync_ShouldPassCancellationToken_ToCloudinaryService()
    {
        // Arrange
        const string isbn = "9781234567890";
        using var cts = new CancellationTokenSource();
        var cancellationToken = cts.Token;
        var coverUrl = new Uri("https://covers.openlibrary.org/b/id/12345-L.jpg");

        var openLibraryResult = new OpenLibraryBookResult(
            Title: "Test",
            Subtitle: null,
            Authors: EmptyStringArray,
            Publishers: EmptyStringArray,
            PublishDate: null,
            NumberOfPages: null,
            CoverUrl: coverUrl,
            Found: true);

        var cloudinaryResult = new CloudinaryUploadResult(
            Url: new Uri("https://res.cloudinary.com/test/uploaded.jpg"),
            PublicId: "test-id",
            Success: true,
            Error: null);

        _openLibraryService.SearchByIsbnAsync(isbn, cancellationToken)
            .Returns(openLibraryResult);
        _cloudinaryService.UploadImageFromUrlAsync(
                Arg.Any<Uri>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                cancellationToken)
            .Returns(cloudinaryResult);

        // Act
        await _sut.SearchByIsbnAsync(isbn, cancellationToken);

        // Assert
        await _cloudinaryService.Received(1).UploadImageFromUrlAsync(
            coverUrl,
            Arg.Any<string>(),
            Arg.Any<string>(),
            cancellationToken);
    }

    #endregion
}
