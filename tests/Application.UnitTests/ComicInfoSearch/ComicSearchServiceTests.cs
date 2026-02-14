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
    [InlineData("September 06, 1987", 1987, 9, 6)]
    [InlineData("Sep 16, 1987", 1987, 9, 16)]
    [InlineData("Sep 06, 1987", 1987, 9, 6)]
    [InlineData("1987-09-16", 1987, 9, 16)]
    [InlineData("1987/09/16", 1987, 9, 16)]
    [InlineData("16/09/1987", 1987, 9, 16)]
    [InlineData("09/16/1987", 1987, 9, 16)]
    [InlineData("September 1987", 1987, 9, 1)]
    [InlineData("Sep 1987", 1987, 9, 1)]
    [InlineData("1987", 1987, 1, 1)]
    [InlineData("2024", 2024, 1, 1)]
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

    [Theory]
    [InlineData("invalid-date")]
    [InlineData("not a date")]
    [InlineData("999")]
    [InlineData("10000")]
    [InlineData("13/45/2020")]
    public async Task SearchByIsbnAsync_ShouldReturnNullPublishDate_WhenDateStringIsInvalid(string publishDate)
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

    #endregion

    #region Volume Parsing Edge Cases

    [Theory]
    [InlineData("SODA, TOME 1", "SODA", 1)]
    [InlineData("soda, tome 5", "soda", 5)]
    [InlineData("Spider-Man, VOL. 10", "Spider-Man", 10)]
    [InlineData("Batman #25", "Batman", 25)]
    public async Task SearchByIsbnAsync_ShouldParseVolumeAndSerie_CaseInsensitively(
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

    [Theory]
    [InlineData(null, "", 1)]
    [InlineData("", "", 1)]
    [InlineData("   ", "", 1)]
    public async Task SearchByIsbnAsync_ShouldHandleEmptyOrNullTitle_WhenParsingVolumeAndSerie(
        string? title,
        string expectedSerie,
        int expectedVolume)
    {
        // Arrange
        const string isbn = "9781234567890";
        var openLibraryResult = new OpenLibraryBookResult(
            Title: title ?? string.Empty,
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
    public async Task SearchByIsbnAsync_ShouldDefaultToVolumeOne_WhenVolumeNumberCannotBeParsed()
    {
        // Arrange
        const string isbn = "9781234567890";
        const string title = "Soda, tome abc";
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
        result.VolumeNumber.Should().Be(1);
    }

    #endregion

    #region ISBN Cleaning Tests

    [Theory]
    [InlineData("978-1-234-56789-0", "9781234567890")]
    [InlineData("978 1 234 56789 0", "9781234567890")]
    [InlineData("978-1234-567890", "9781234567890")]
    public async Task SearchByIsbnAsync_ShouldCleanIsbn_WhenUploadingToCloudinary(
        string isbnWithFormatting,
        string expectedCleanIsbn)
    {
        // Arrange
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

        _openLibraryService.SearchByIsbnAsync(isbnWithFormatting, Arg.Any<CancellationToken>())
            .Returns(openLibraryResult);
        _cloudinaryService.UploadImageFromUrlAsync(
                Arg.Any<Uri>(),
                Arg.Any<string>(),
                expectedCleanIsbn,
                Arg.Any<CancellationToken>())
            .Returns(cloudinaryResult);

        // Act
        await _sut.SearchByIsbnAsync(isbnWithFormatting);

        // Assert
        await _cloudinaryService.Received(1).UploadImageFromUrlAsync(
            coverUrl,
            "test-covers",
            expectedCleanIsbn,
            Arg.Any<CancellationToken>());
    }

    #endregion

    #region Additional Date Format Tests

    [Theory]
    [InlineData("January 1, 2000", 2000, 1, 1)]
    [InlineData("December 31, 1999", 1999, 12, 31)]
    [InlineData("Feb 29, 2020", 2020, 2, 29)]
    [InlineData("2020/02/29", 2020, 2, 29)]
    [InlineData("29/02/2020", 2020, 2, 29)]
    [InlineData("March 2021", 2021, 3, 1)]
    [InlineData("Dec 2019", 2019, 12, 1)]
    public async Task SearchByIsbnAsync_ShouldParsePublishDate_WithAdditionalFormats(
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

    #endregion

    #region Multiple Volume Patterns Tests

    [Theory]
    [InlineData("The Walking Dead, Tome 1 - Days Gone Bye", "The Walking Dead", 1)]
    [InlineData("Saga - vol. 1: Chapter One", "Saga", 1)]
    [InlineData("Y: The Last Man, Vol. 2 - Cycles", "Y: The Last Man", 2)]
    public async Task SearchByIsbnAsync_ShouldExtractSerie_FromComplexTitles(
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

    #endregion

    #region Metadata Mapping Tests

    [Fact]
    public async Task SearchByIsbnAsync_ShouldMapAllMetadata_FromOpenLibraryToResult()
    {
        // Arrange
        const string isbn = "9781607066019";
        const string title = "Saga, vol. 1";
        const string subtitle = "Chapter One";
        var authors = new[] { "Brian K. Vaughan", "Fiona Staples" };
        var publishers = new[] { "Image Comics" };
        const string publishDate = "October 10, 2012";
        const int numberOfPages = 160;
        var coverUrl = new Uri("https://covers.openlibrary.org/b/id/12345-L.jpg");

        var openLibraryResult = new OpenLibraryBookResult(
            Title: title,
            Subtitle: subtitle,
            Authors: authors,
            Publishers: publishers,
            PublishDate: publishDate,
            NumberOfPages: numberOfPages,
            CoverUrl: coverUrl,
            Found: true);

        var cloudinaryResult = new CloudinaryUploadResult(
            Url: new Uri("https://res.cloudinary.com/test/saga.jpg"),
            PublicId: "test-id",
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
        result.Title.Should().Be(subtitle);
        result.Serie.Should().Be("Saga");
        result.VolumeNumber.Should().Be(1);
        result.Authors.Should().Be("Brian K. Vaughan, Fiona Staples");
        result.Publishers.Should().Be("Image Comics");
        result.PublishDate.Should().Be(new DateOnly(2012, 10, 10));
        result.NumberOfPages.Should().Be(160);
        result.ImageUrl.Should().NotBeEmpty();
    }

    [Fact]
    public async Task SearchByIsbnAsync_ShouldHandleNullNumberOfPages()
    {
        // Arrange
        const string isbn = "9781234567890";
        var openLibraryResult = new OpenLibraryBookResult(
            Title: "Test Comic",
            Subtitle: null,
            Authors: SingleAuthorArray,
            Publishers: SinglePublisherArray,
            PublishDate: "2024",
            NumberOfPages: null,
            CoverUrl: null,
            Found: true);

        _openLibraryService.SearchByIsbnAsync(isbn, Arg.Any<CancellationToken>())
            .Returns(openLibraryResult);

        // Act
        var result = await _sut.SearchByIsbnAsync(isbn);

        // Assert
        result.NumberOfPages.Should().BeNull();
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public async Task SearchByIsbnAsync_ShouldReturnNotFound_WhenOperationCancelledButNotByToken()
    {
        // Arrange
        const string isbn = "9781234567890";
        var exception = new TaskCanceledException("Operation was canceled");

        _openLibraryService.SearchByIsbnAsync(isbn, Arg.Any<CancellationToken>())
            .ThrowsAsync(exception);

        // Act
        var result = await _sut.SearchByIsbnAsync(isbn);

        // Assert
        result.Found.Should().BeFalse();
        result.Isbn.Should().Be(isbn);
        result.Title.Should().BeEmpty();
        result.Serie.Should().BeEmpty();
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
