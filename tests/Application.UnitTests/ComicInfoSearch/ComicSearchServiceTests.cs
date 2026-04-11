using Application.ComicInfoSearch;
using Application.Interfaces;
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
    private readonly IGoogleBooksService _googleBooksService;
    private readonly IBedethequeService _bedethequeService;
    private readonly ICloudinaryService _cloudinaryService;
    private readonly IOptions<CloudinarySettings> _cloudinarySettings;
    private readonly ComicSearchService _sut;

    private static BedethequeBookResult BedethequeNotFound => new(
        Title: string.Empty,
        Serie: string.Empty,
        VolumeNumber: 1,
        Authors: [],
        Publishers: [],
        PublishDate: null,
        NumberOfPages: null,
        CoverUrl: null,
        Found: false);

    private static GoogleBooksBookResult GoogleBooksNotFound => new(
        Title: string.Empty,
        Subtitle: null,
        Authors: [],
        Publishers: [],
        PublishDate: null,
        NumberOfPages: null,
        CoverUrl: null,
        Description: null,
        Categories: [],
        Language: null,
        Found: false);

    public ComicSearchServiceTests()
    {
        _openLibraryService = Substitute.For<IOpenLibraryService>();
        _googleBooksService = Substitute.For<IGoogleBooksService>();
        _bedethequeService = Substitute.For<IBedethequeService>();
        _cloudinaryService = Substitute.For<ICloudinaryService>();
        _cloudinarySettings = Options.Create(new CloudinarySettings
        {
            CloudName = "test-cloud",
            ApiKey = "test-key",
            ApiSecret = "test-secret",
            Folder = "test-covers"
        });

        // Default: Bedetheque and Google Books return not found so tests focused on OpenLibrary still work
        _bedethequeService.SearchByIsbnAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(BedethequeNotFound);
        _googleBooksService.SearchByIsbnAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(GoogleBooksNotFound);

        _sut = new ComicSearchService(_openLibraryService, _googleBooksService, _bedethequeService, _cloudinaryService, _cloudinarySettings);
    }

    #region SearchByIsbnAsync Tests

    [Fact]
    public async Task SearchByIsbnAsync_ShouldReturnNotFound_WhenBothProvidersReturnNotFound()
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

        var googleBooksResult = new GoogleBooksBookResult(
            Title: string.Empty,
            Subtitle: null,
            Authors: EmptyStringArray,
            Publishers: EmptyStringArray,
            PublishDate: null,
            NumberOfPages: null,
            CoverUrl: null,
            Description: null,
            Categories: [],
            Language: null,
            Found: false);

        _openLibraryService.SearchByIsbnAsync(isbn, Arg.Any<CancellationToken>())
            .Returns(openLibraryResult);
        _googleBooksService.SearchByIsbnAsync(isbn, Arg.Any<CancellationToken>())
            .Returns(googleBooksResult);

        // Act
        var result = await _sut.SearchByIsbnAsync(isbn);

        // Assert
        result.Found.Should().BeFalse();
        result.Isbn.Should().Be(isbn);
        result.Title.Should().BeEmpty();
        result.Serie.Should().BeEmpty();
        result.VolumeNumber.Should().Be(1);
        await _googleBooksService.Received(1).SearchByIsbnAsync(isbn, Arg.Any<CancellationToken>());
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
            PublishDate: new DateOnly(1987, 1, 1),
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
        result.Title.Should().Be("Soda");
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
            PublishDate: null,
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
            PublishDate: null,
            NumberOfPages: 100,
            CoverUrl: coverUrl,
            Found: true);

        var cloudinaryResult = new CloudinaryUploadResult(
            Url: new Uri("https://res.cloudinary.com/test/uploaded.jpg"),
            PublicId: "test-id",
            Success: true,
            Error: null);

        _openLibraryService.SearchByIsbnAsync("9781234567890", Arg.Any<CancellationToken>())
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
            PublishDate: null,
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
            PublishDate: null,
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
    [InlineData("Fullmetal Alchemist Tome 23", "Fullmetal Alchemist", 23)]
    [InlineData("One Piece Tome 105", "One Piece", 105)]
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
            PublishDate: null,
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
            PublishDate: null,
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

    #region Volume Parsing Edge Cases

    [Theory]
    [InlineData("SODA, TOME 1", "SODA", 1)]
    [InlineData("soda, tome 5", "soda", 5)]
    [InlineData("FULLMETAL ALCHEMIST TOME 23", "FULLMETAL ALCHEMIST", 23)]
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
            PublishDate: null,
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
            PublishDate: null,
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
            PublishDate: null,
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
            PublishDate: null,
            NumberOfPages: 100,
            CoverUrl: coverUrl,
            Found: true);

        var cloudinaryResult = new CloudinaryUploadResult(
            Url: new Uri("https://res.cloudinary.com/test/uploaded.jpg"),
            PublicId: "test-id",
            Success: true,
            Error: null);

        _openLibraryService.SearchByIsbnAsync(expectedCleanIsbn, Arg.Any<CancellationToken>())
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
            PublishDate: null,
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
        const int numberOfPages = 160;
        var coverUrl = new Uri("https://covers.openlibrary.org/b/id/12345-L.jpg");

        var openLibraryResult = new OpenLibraryBookResult(
            Title: title,
            Subtitle: subtitle,
            Authors: authors,
            Publishers: publishers,
            PublishDate: new DateOnly(2012, 10, 10),
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
            PublishDate: null,
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

    [Fact]
    public async Task SearchByIsbnAsync_ShouldReturnNotFound_WhenTokenIsCancelledByCallerBeforeCall()
    {
        // Arrange
        const string isbn = "9781234567890";
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync(); // Cancel the token before the call

        _openLibraryService.SearchByIsbnAsync(isbn, Arg.Any<CancellationToken>())
            .ThrowsAsync(new OperationCanceledException(cts.Token));

        // Act
        var result = await _sut.SearchByIsbnAsync(isbn, cts.Token);

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
            PublishDate: null,
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
            PublishDate: null,
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
            PublishDate: null,
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

    #region Google Books Fallback Tests

    [Fact]
    public async Task SearchByIsbnAsync_ShouldFallbackToGoogleBooks_WhenOpenLibraryReturnsNotFound()
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

        var googleBooksResult = new GoogleBooksBookResult(
            Title: "Soda, tome 1",
            Subtitle: "Prières et balistique",
            Authors: ["Philippe Tome", "Luc Warnant"],
            Publishers: ["Dupuis"],
            PublishDate: new DateOnly(1987, 1, 1),
            NumberOfPages: 48,
            CoverUrl: new Uri("https://books.google.com/content?id=test"),
            Description: "A comic book.",
            Categories: ["Comics & Graphic Novels"],
            Language: "fr",
            Found: true);

        var cloudinaryResult = new CloudinaryUploadResult(
            Url: new Uri("https://res.cloudinary.com/test/image/upload/v1/test-covers/9781234567890.jpg"),
            PublicId: "test-covers/9781234567890",
            Success: true,
            Error: null);

        _openLibraryService.SearchByIsbnAsync(isbn, Arg.Any<CancellationToken>())
            .Returns(openLibraryResult);
        _googleBooksService.SearchByIsbnAsync(isbn, Arg.Any<CancellationToken>())
            .Returns(googleBooksResult);
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
        result.Title.Should().Be("Prières et balistique");
        result.Serie.Should().Be("Soda");
        result.VolumeNumber.Should().Be(1);
        result.Authors.Should().Be("Philippe Tome, Luc Warnant");
        result.Publishers.Should().Be("Dupuis");
        result.NumberOfPages.Should().Be(48);
        result.PublishDate.Should().Be(new DateOnly(1987, 1, 1));
    }

    [Fact]
    public async Task SearchByIsbnAsync_ShouldNotCallGoogleBooksOrOpenLibrary_WhenBedethequeReturnsData()
    {
        // Arrange
        const string isbn = "9781234567890";
        var bedethequeResult = new BedethequeBookResult(
            Title: "L'Ankou",
            Serie: "Biguden",
            VolumeNumber: 1,
            Authors: ["Stan Silas"],
            Publishers: ["EP Media"],
            PublishDate: new DateOnly(2014, 8, 1),
            NumberOfPages: 62,
            CoverUrl: null,
            Found: true);

        _bedethequeService.SearchByIsbnAsync(isbn, Arg.Any<CancellationToken>())
            .Returns(bedethequeResult);

        // Act
        await _sut.SearchByIsbnAsync(isbn);

        // Assert
        await _googleBooksService.DidNotReceive().SearchByIsbnAsync(
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
        await _openLibraryService.DidNotReceive().SearchByIsbnAsync(
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SearchByIsbnAsync_ShouldUploadGoogleBooksCoverToCloudinary_WhenFallbackSucceeds()
    {
        // Arrange
        const string isbn = "9781234567890";
        var openLibraryResult = new OpenLibraryBookResult(
            Title: string.Empty, Subtitle: null, Authors: EmptyStringArray,
            Publishers: EmptyStringArray, PublishDate: null, NumberOfPages: null,
            CoverUrl: null, Found: false);

        var googleCoverUrl = new Uri("https://books.google.com/content?id=test&img=1");
        var googleBooksResult = new GoogleBooksBookResult(
            Title: "Test Comic",
            Subtitle: null,
            Authors: SingleAuthorArray,
            Publishers: SinglePublisherArray,
            PublishDate: null,
            NumberOfPages: 100,
            CoverUrl: googleCoverUrl,
            Description: null,
            Categories: [],
            Language: null,
            Found: true);

        var cloudinaryResult = new CloudinaryUploadResult(
            Url: new Uri("https://res.cloudinary.com/test/uploaded.jpg"),
            PublicId: "test-id",
            Success: true,
            Error: null);

        _openLibraryService.SearchByIsbnAsync(isbn, Arg.Any<CancellationToken>())
            .Returns(openLibraryResult);
        _googleBooksService.SearchByIsbnAsync(isbn, Arg.Any<CancellationToken>())
            .Returns(googleBooksResult);
        _cloudinaryService.UploadImageFromUrlAsync(
                googleCoverUrl,
                "test-covers",
                isbn,
                Arg.Any<CancellationToken>())
            .Returns(cloudinaryResult);

        // Act
        var result = await _sut.SearchByIsbnAsync(isbn);

        // Assert
        result.ImageUrl.Should().Be("https://res.cloudinary.com/test/uploaded.jpg");
        await _cloudinaryService.Received(1).UploadImageFromUrlAsync(
            googleCoverUrl,
            "test-covers",
            isbn,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SearchByIsbnAsync_ShouldPassCancellationToken_ToGoogleBooksService()
    {
        // Arrange
        const string isbn = "9781234567890";
        using var cts = new CancellationTokenSource();
        var cancellationToken = cts.Token;

        var openLibraryResult = new OpenLibraryBookResult(
            Title: string.Empty, Subtitle: null, Authors: EmptyStringArray,
            Publishers: EmptyStringArray, PublishDate: null, NumberOfPages: null,
            CoverUrl: null, Found: false);

        var googleBooksResult = new GoogleBooksBookResult(
            Title: "Test", Subtitle: null, Authors: EmptyStringArray,
            Publishers: EmptyStringArray, PublishDate: null, NumberOfPages: null,
            CoverUrl: null, Description: null, Categories: [], Language: null,
            Found: true);

        _openLibraryService.SearchByIsbnAsync(isbn, cancellationToken)
            .Returns(openLibraryResult);
        _googleBooksService.SearchByIsbnAsync(isbn, cancellationToken)
            .Returns(googleBooksResult);

        // Act
        await _sut.SearchByIsbnAsync(isbn, cancellationToken);

        // Assert
        await _googleBooksService.Received(1).SearchByIsbnAsync(isbn, cancellationToken);
    }

    [Fact]
    public async Task SearchByIsbnAsync_ShouldMapGoogleBooksMetadata_WithAllFields()
    {
        // Arrange
        const string isbn = "9781607066019";
        var openLibraryResult = new OpenLibraryBookResult(
            Title: string.Empty, Subtitle: null, Authors: EmptyStringArray,
            Publishers: EmptyStringArray, PublishDate: null, NumberOfPages: null,
            CoverUrl: null, Found: false);

        var googleBooksResult = new GoogleBooksBookResult(
            Title: "Saga, vol. 1",
            Subtitle: "Chapter One",
            Authors: ["Brian K. Vaughan", "Fiona Staples"],
            Publishers: ["Image Comics"],
            PublishDate: new DateOnly(2012, 10, 10),
            NumberOfPages: 160,
            CoverUrl: new Uri("https://books.google.com/content?id=test"),
            Description: "An epic space opera.",
            Categories: ["Comics & Graphic Novels"],
            Language: "en",
            Found: true);

        var cloudinaryResult = new CloudinaryUploadResult(
            Url: new Uri("https://res.cloudinary.com/test/saga.jpg"),
            PublicId: "test-id",
            Success: true,
            Error: null);

        _openLibraryService.SearchByIsbnAsync(isbn, Arg.Any<CancellationToken>())
            .Returns(openLibraryResult);
        _googleBooksService.SearchByIsbnAsync(isbn, Arg.Any<CancellationToken>())
            .Returns(googleBooksResult);
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
        result.Title.Should().Be("Chapter One");
        result.Serie.Should().Be("Saga");
        result.VolumeNumber.Should().Be(1);
        result.Authors.Should().Be("Brian K. Vaughan, Fiona Staples");
        result.Publishers.Should().Be("Image Comics");
        result.PublishDate.Should().Be(new DateOnly(2012, 10, 10));
        result.NumberOfPages.Should().Be(160);
        result.ImageUrl.Should().NotBeEmpty();
    }

    #endregion
}

// Keep this class in the same file as it was originally (record tests)
public sealed class ComicSearchServiceWithLocalCoverTests
{
    private readonly IOpenLibraryService _openLibraryService;
    private readonly IGoogleBooksService _googleBooksService;
    private readonly IBedethequeService _bedethequeService;
    private readonly ICloudinaryService _cloudinaryService;
    private readonly IOptions<CloudinarySettings> _cloudinarySettings;
    private readonly ComicSearchService _sut;

    private static BedethequeBookResult BedethequeNotFound => new(
        Title: string.Empty,
        Serie: string.Empty,
        VolumeNumber: 1,
        Authors: [],
        Publishers: [],
        PublishDate: null,
        NumberOfPages: null,
        CoverUrl: null,
        Found: false);

    private static GoogleBooksBookResult GoogleBooksNotFound => new(
        Title: string.Empty,
        Subtitle: null,
        Authors: [],
        Publishers: [],
        PublishDate: null,
        NumberOfPages: null,
        CoverUrl: null,
        Description: null,
        Categories: [],
        Language: null,
        Found: false);

    private static OpenLibraryBookResult OpenLibraryNotFound => new(
        Title: string.Empty,
        Subtitle: null,
        Authors: [],
        Publishers: [],
        PublishDate: null,
        NumberOfPages: null,
        CoverUrl: null,
        Found: false);

    private static BedethequeBookResult MakeBedethequeFound(
        string serie = "Soda",
        string title = "L'Ankou",
        int volume = 1) =>
        new(
            Title: title,
            Serie: serie,
            VolumeNumber: volume,
            Authors: ["Philippe Tome", "Luc Warnant"],
            Publishers: ["Dupuis"],
            PublishDate: new DateOnly(1987, 10, 1),
            NumberOfPages: 48,
            CoverUrl: null,
            Found: true);

    public ComicSearchServiceWithLocalCoverTests()
    {
        _openLibraryService = Substitute.For<IOpenLibraryService>();
        _googleBooksService = Substitute.For<IGoogleBooksService>();
        _bedethequeService = Substitute.For<IBedethequeService>();
        _cloudinaryService = Substitute.For<ICloudinaryService>();
        _cloudinarySettings = Options.Create(new CloudinarySettings
        {
            CloudName = "test-cloud",
            ApiKey = "test-key",
            ApiSecret = "test-secret",
            Folder = "test-covers"
        });

        _bedethequeService.SearchByIsbnAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(BedethequeNotFound);
        _googleBooksService.SearchByIsbnAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(GoogleBooksNotFound);

        // Default no-op for stream uploads so tests without explicit setup don't throw
        _cloudinaryService.UploadImageFromStreamAsync(
                Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new CloudinaryUploadResult(null, null, false, "not configured"));

        _sut = new ComicSearchService(
            _openLibraryService, _googleBooksService, _bedethequeService, _cloudinaryService, _cloudinarySettings);
    }

    [Fact]
    public async Task SearchByIsbnWithLocalCoverAsync_Should_UploadCoverFromStream()
    {
        // Arrange
        const string isbn = "9782075162869";
        using var stream = new MemoryStream([1, 2, 3]);
        const string coverFileName = "cover.webp";

        _bedethequeService.SearchByIsbnAsync(isbn, Arg.Any<CancellationToken>())
            .Returns(MakeBedethequeFound());

        var cloudinaryResult = new CloudinaryUploadResult(
            Url: new Uri("https://res.cloudinary.com/test/soda.jpg"),
            PublicId: "test-covers/9782075162869",
            Success: true,
            Error: null);

        _cloudinaryService.UploadImageFromStreamAsync(
                Arg.Any<Stream>(), coverFileName, Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(cloudinaryResult);

        // Act
        var result = await _sut.SearchByIsbnWithLocalCoverAsync(isbn, stream, coverFileName);

        // Assert
        result.Found.Should().BeTrue();
        result.ImageUrl.Should().Be("https://res.cloudinary.com/test/soda.jpg");
        await _cloudinaryService.Received(1).UploadImageFromStreamAsync(
            stream, coverFileName, Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _cloudinaryService.DidNotReceive().UploadImageFromUrlAsync(
            Arg.Any<Uri>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SearchByIsbnWithLocalCoverAsync_Should_ReturnMetadata_WhenBedethequeFindsResult()
    {
        // Arrange
        const string isbn = "9782075162869";
        using var stream = new MemoryStream([1, 2, 3]);

        var bedethequeResult = new BedethequeBookResult(
            Title: "Prières et balistique",
            Serie: "Soda",
            VolumeNumber: 1,
            Authors: ["Philippe Tome", "Luc Warnant"],
            Publishers: ["Dupuis"],
            PublishDate: new DateOnly(1987, 10, 1),
            NumberOfPages: 48,
            CoverUrl: null,
            Found: true);

        _bedethequeService.SearchByIsbnAsync(isbn, Arg.Any<CancellationToken>())
            .Returns(bedethequeResult);

        // Act
        var result = await _sut.SearchByIsbnWithLocalCoverAsync(isbn, stream, "cover.webp");

        // Assert
        result.Found.Should().BeTrue();
        result.Isbn.Should().Be(isbn);
        result.Title.Should().Be("Prières et balistique");
        result.Serie.Should().Be("Soda");
        result.VolumeNumber.Should().Be(1);
        result.Authors.Should().Be("Philippe Tome, Luc Warnant");
        result.Publishers.Should().Be("Dupuis");
        result.PublishDate.Should().Be(new DateOnly(1987, 10, 1));
        result.NumberOfPages.Should().Be(48);
    }

    [Fact]
    public async Task SearchByIsbnWithLocalCoverAsync_Should_FallbackToGoogleBooks()
    {
        // Arrange
        const string isbn = "9782075162869";
        using var stream = new MemoryStream([1, 2, 3]);

        var googleBooksResult = new GoogleBooksBookResult(
            Title: "Soda, tome 1",
            Subtitle: "Prières et balistique",
            Authors: ["Philippe Tome", "Luc Warnant"],
            Publishers: ["Dupuis"],
            PublishDate: new DateOnly(1987, 1, 1),
            NumberOfPages: 48,
            CoverUrl: null,
            Description: null,
            Categories: [],
            Language: "fr",
            Found: true);

        _googleBooksService.SearchByIsbnAsync(isbn, Arg.Any<CancellationToken>())
            .Returns(googleBooksResult);

        // Act
        var result = await _sut.SearchByIsbnWithLocalCoverAsync(isbn, stream, "cover.webp");

        // Assert
        result.Found.Should().BeTrue();
        result.Serie.Should().Be("Soda");
        result.Title.Should().Be("Prières et balistique");
        result.VolumeNumber.Should().Be(1);
        await _openLibraryService.DidNotReceive().SearchByIsbnAsync(
            Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SearchByIsbnWithLocalCoverAsync_Should_ReturnNotFound_WhenNoProviderFindsResult()
    {
        // Arrange
        const string isbn = "9782075162869";
        using var stream = new MemoryStream([1, 2, 3]);

        _openLibraryService.SearchByIsbnAsync(isbn, Arg.Any<CancellationToken>())
            .Returns(OpenLibraryNotFound);

        // Act
        var result = await _sut.SearchByIsbnWithLocalCoverAsync(isbn, stream, "cover.webp");

        // Assert
        result.Found.Should().BeFalse();
        result.Isbn.Should().Be(isbn);
    }

    [Fact]
    public async Task SearchByIsbnWithLocalCoverAsync_Should_UseIsbnAsPublicId()
    {
        // Arrange
        const string formattedIsbn = "978-2-07-516286-9";
        const string cleanIsbn = "9782075162869";
        using var stream = new MemoryStream([1, 2, 3]);
        const string coverFileName = "cover.webp";

        _bedethequeService.SearchByIsbnAsync(cleanIsbn, Arg.Any<CancellationToken>())
            .Returns(MakeBedethequeFound());

        // Act
        await _sut.SearchByIsbnWithLocalCoverAsync(formattedIsbn, stream, coverFileName);

        // Assert: publicId must be the normalized ISBN
        await _cloudinaryService.Received(1).UploadImageFromStreamAsync(
            stream, coverFileName, "test-covers", cleanIsbn, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SearchByIsbnWithLocalCoverAsync_Should_UseGuidPrefix_WhenNoIsbn()
    {
        // Arrange
        using var stream = new MemoryStream([1, 2, 3]);
        const string coverFileName = "cover.webp";

        _cloudinaryService.UploadImageFromStreamAsync(
                Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<string>(),
                Arg.Is<string>(id => id.StartsWith("digital-", StringComparison.Ordinal)),
                Arg.Any<CancellationToken>())
            .Returns(new CloudinaryUploadResult(
                Url: new Uri("https://res.cloudinary.com/test/digital-abc.jpg"),
                PublicId: "test-covers/digital-abc",
                Success: true,
                Error: null));

        // Act
        var result = await _sut.SearchByIsbnWithLocalCoverAsync(string.Empty, stream, coverFileName);

        // Assert: providers not called, cover uploaded with guid-based publicId
        result.Found.Should().BeFalse();
        result.ImageUrl.Should().Be("https://res.cloudinary.com/test/digital-abc.jpg");
        await _bedethequeService.DidNotReceive().SearchByIsbnAsync(
            Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _cloudinaryService.Received(1).UploadImageFromStreamAsync(
            stream, coverFileName, "test-covers",
            Arg.Is<string>(id => id.StartsWith("digital-", StringComparison.Ordinal)),
            Arg.Any<CancellationToken>());
    }
}
