using Application.ComicInfoSearch;
using Application.Interfaces;
using NSubstitute;

namespace Application.UnitTests.ComicInfoSearch;

public sealed class ComicSearchServiceTests
{
    private static readonly string[] EmptyStringArray = [];
    private static readonly string[] SingleAuthorArray = ["Author"];
    private static readonly string[] SinglePublisherArray = ["Publisher"];

    private readonly IBnfCatalogueService _bnfService = BnfNotFoundFactory.Create();
    private readonly IBubbleService _bubbleService = BubbleNotFoundFactory.Create();
    private readonly IOpenLibraryService _openLibraryService;
    private readonly IGoogleBooksService _googleBooksService;
    private readonly IBedethequeService _bedethequeService;
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

        // Default: Bedetheque and Google Books return not found so tests focused on OpenLibrary still work
        _bedethequeService.SearchByIsbnAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(BedethequeNotFound);
        _googleBooksService.SearchByIsbnAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(GoogleBooksNotFound);
        _openLibraryService.SearchByIsbnAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new OpenLibraryBookResult(string.Empty, null, [], [], null, null, null, Found: false));

        _sut = new ComicSearchService(new BookCatalogues(_bnfService, _googleBooksService, _openLibraryService, _bubbleService, _bedethequeService), NullLogger<ComicSearchService>.Instance);
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
        var result = await _sut.SearchByIsbnAsync(isbn, TestContext.Current.CancellationToken);

        // Assert
        result.Found.Should().BeFalse();
        result.Isbn.Should().Be(isbn);
        result.Title.Should().BeEmpty();
        result.Serie.Should().BeEmpty();
        result.VolumeNumber.Should().Be(1);
        result.Source.Should().BeNull();
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

        _openLibraryService.SearchByIsbnAsync(isbn, Arg.Any<CancellationToken>())
            .Returns(openLibraryResult);

        // Act
        var result = await _sut.SearchByIsbnAsync(isbn, TestContext.Current.CancellationToken);

        // Assert
        result.Found.Should().BeTrue();
        result.Isbn.Should().Be(isbn);
        result.Title.Should().Be("Soda");
        result.Serie.Should().Be("Soda");
        result.VolumeNumber.Should().Be(1);
        result.Authors.Should().Be("Philippe Tome, Luc Wartholz");
        result.Publishers.Should().Be("Dupuis");
        result.NumberOfPages.Should().Be(48);
        result.ImageUrl.Should().Be("https://covers.openlibrary.org/b/id/12345-L.jpg");
        result.PublishDate.Should().Be(new DateOnly(1987, 1, 1));
        result.Source.Should().Be("Open Library");
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
        var result = await _sut.SearchByIsbnAsync(isbn, TestContext.Current.CancellationToken);

        // Assert
        result.Title.Should().Be(subtitle);
    }

    private static OpenLibraryBookResult OpenLibraryBook(string title, string? subtitle = null, string? serie = null, int? volumeNumber = null) =>
        new(title, subtitle, [], [], null, null, null, Found: true, Serie: serie, VolumeNumber: volumeNumber);

    [Fact]
    public void ParseTitleInfo_Should_KeepTheTitle_WhenTheSourceGivesTheSeriesApart()
    {
        var book = OpenLibraryBook("Le Noyé à deux têtes", subtitle: "Un sous-titre", serie: "Les Aventures extraordinaires d'Adèle Blanc-Sec", volumeNumber: 6);

        _sut.ParseTitleInfo(book).Should().Be(("Le Noyé à deux têtes", "Les Aventures extraordinaires d'Adèle Blanc-Sec", (int?)6));
    }

    [Theory]
    [InlineData("Les Schtroumpfs. 12, Le schtroumpfissime", "Le schtroumpfissime", "Les Schtroumpfs", 12)]
    [InlineData("Soda, tome 3", "Soda", "Soda", 3)]
    [InlineData("Le Noyé à deux têtes", "Le Noyé à deux têtes", "Le Noyé à deux têtes", null)]
    public void ParseTitleInfo_Should_ReadTheSeriesFromTheTitle_WhenTheSourceGivesNoSeries(string rawTitle, string title, string serie, int? volumeNumber)
    {
        _sut.ParseTitleInfo(OpenLibraryBook(rawTitle)).Should().Be((title, serie, volumeNumber));
    }

    [Fact]
    public async Task SearchByIsbnAsync_Should_StoreTheBookAsTheFirstVolume_WhenTheSourceGivesASeriesWithoutVolume()
    {
        const string isbn = "220330507X";
        _openLibraryService.SearchByIsbnAsync(isbn, Arg.Any<CancellationToken>())
            .Returns(OpenLibraryBook("Le Noyé à deux têtes", serie: "Les Aventures extraordinaires d'Adèle Blanc-Sec"));

        var result = await _sut.SearchByIsbnAsync(isbn, TestContext.Current.CancellationToken);

        result.Serie.Should().Be("Les Aventures extraordinaires d'Adèle Blanc-Sec");
        result.Title.Should().Be("Le Noyé à deux têtes");
        result.VolumeNumber.Should().Be(1);
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
        var result = await _sut.SearchByIsbnAsync(isbn, TestContext.Current.CancellationToken);

        // Assert
        result.ImageUrl.Should().BeEmpty();
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
        var result = await _sut.SearchByIsbnAsync(isbn, TestContext.Current.CancellationToken);

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
        var result = await _sut.SearchByIsbnAsync(isbn, TestContext.Current.CancellationToken);

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
        var result = await _sut.SearchByIsbnAsync(isbn, TestContext.Current.CancellationToken);

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
        var result = await _sut.SearchByIsbnAsync(isbn, TestContext.Current.CancellationToken);

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
        var result = await _sut.SearchByIsbnAsync(isbn, TestContext.Current.CancellationToken);

        // Assert
        result.VolumeNumber.Should().Be(1);
    }

    #endregion

    #region ISBN Cleaning Tests

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
        var result = await _sut.SearchByIsbnAsync(isbn, TestContext.Current.CancellationToken);

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

        _openLibraryService.SearchByIsbnAsync(isbn, Arg.Any<CancellationToken>())
            .Returns(openLibraryResult);

        // Act
        var result = await _sut.SearchByIsbnAsync(isbn, TestContext.Current.CancellationToken);

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
        var result = await _sut.SearchByIsbnAsync(isbn, TestContext.Current.CancellationToken);

        // Assert
        result.NumberOfPages.Should().BeNull();
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
        var result = await _sut.SearchByIsbnAsync(isbn, TestContext.Current.CancellationToken);

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
        var result = await _sut.SearchByIsbnAsync(isbn, TestContext.Current.CancellationToken);

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
        var result = await _sut.SearchByIsbnAsync(isbn, TestContext.Current.CancellationToken);

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

        _openLibraryService.SearchByIsbnAsync(isbn, Arg.Any<CancellationToken>())
            .Returns(openLibraryResult);
        _googleBooksService.SearchByIsbnAsync(isbn, Arg.Any<CancellationToken>())
            .Returns(googleBooksResult);

        // Act
        var result = await _sut.SearchByIsbnAsync(isbn, TestContext.Current.CancellationToken);

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
        result.Source.Should().Be("Google Books");
    }

    [Fact]
    public async Task SearchByIsbnAsync_Should_AskBedethequeLast_WhenNoOtherSourceKnowsTheBook()
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
        var result = await _sut.SearchByIsbnAsync(isbn, TestContext.Current.CancellationToken);

        // Assert
        result.Serie.Should().Be("Biguden");
        result.Source.Should().Be("Bedetheque");
        Received.InOrder(() =>
        {
            _bnfService.SearchByIsbnAsync(isbn, Arg.Any<CancellationToken>());
            _googleBooksService.SearchByIsbnAsync(isbn, Arg.Any<CancellationToken>());
            _openLibraryService.SearchByIsbnAsync(isbn, Arg.Any<CancellationToken>());
            _bubbleService.SearchByIsbnAsync(isbn, Arg.Any<CancellationToken>());
            _bedethequeService.SearchByIsbnAsync(isbn, Arg.Any<CancellationToken>());
        });
    }

    [Fact]
    public async Task SearchByIsbnAsync_Should_TakeBubbleBeforeBedetheque_WhenTheOtherSourcesDoNotKnowTheBook()
    {
        // Arrange
        const string isbn = "9791026820369";
        var coverUrl = new Uri("https://assets.bubblebd.com/img/1jvlcxhupm/qhnc4tjxcz.jpg");
        _bubbleService.SearchByIsbnAsync(isbn, Arg.Any<CancellationToken>())
            .Returns(new BubbleBookResult("Entre deux vies", "Catwoman - Selina Kyle", 3, [], ["Urban Comics"], new DateOnly(2021, 5, 7), 200, coverUrl, Found: true));

        // Act
        var result = await _sut.SearchByIsbnAsync(isbn, TestContext.Current.CancellationToken);

        // Assert
        result.Should().BeEquivalentTo(new
        {
            Title = "Entre deux vies",
            Serie = "Catwoman - Selina Kyle",
            VolumeNumber = 3,
            Publishers = "Urban Comics",
            PublishDate = new DateOnly(2021, 5, 7),
            NumberOfPages = 200,
            ImageUrl = coverUrl.ToString(),
            Source = "Bubble",
        });
        await _bedethequeService.DidNotReceive().SearchByIsbnAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SearchByIsbnAsync_Should_TakeTheBubbleCover_WhenTheBnfKnowsTheBookButGoogleAndOpenLibraryHaveNoCover()
    {
        // Arrange
        const string isbn = "9791026820369";
        var coverUrl = new Uri("https://assets.bubblebd.com/img/1jvlcxhupm/qhnc4tjxcz.jpg");
        _bnfService.SearchByIsbnAsync(isbn, Arg.Any<CancellationToken>())
            .Returns(new BnfBookResult("Entre deux vies", null, [], [], null, null, null, Found: true));
        _openLibraryService.SearchByIsbnAsync(isbn, Arg.Any<CancellationToken>())
            .Returns(new OpenLibraryBookResult("Entre deux vies", null, [], [], null, null, null, Found: true));
        _bubbleService.SearchByIsbnAsync(isbn, Arg.Any<CancellationToken>())
            .Returns(new BubbleBookResult("Entre deux vies", "Catwoman - Selina Kyle", 3, [], [], null, null, coverUrl, Found: true));

        // Act
        var result = await _sut.SearchByIsbnAsync(isbn, TestContext.Current.CancellationToken);

        // Assert
        result.Source.Should().Be("BnF");
        result.ImageUrl.Should().Be(coverUrl.ToString());
    }

    [Fact]
    public async Task SearchByIsbnAsync_Should_TakeTheBnfRecordAndTheGoogleCover_WhenTheBnfKnowsTheBook()
    {
        // Arrange
        const string isbn = "9782917237359";
        var coverUrl = new Uri("https://books.google.com/content?id=tangente&img=1");
        _bnfService.SearchByIsbnAsync(isbn, Arg.Any<CancellationToken>())
            .Returns(new BnfBookResult("Tangente", null, ["Céline Wagner"], ["des Ronds dans l'O"], new DateOnly(2012, 1, 1), 82, null, Found: true));
        _googleBooksService.SearchByIsbnAsync(isbn, Arg.Any<CancellationToken>())
            .Returns(GoogleBooksNotFound with { Found = true, Title = "Tangente (Google)", CoverUrl = coverUrl });

        // Act
        var result = await _sut.SearchByIsbnAsync(isbn, TestContext.Current.CancellationToken);

        // Assert
        result.Found.Should().BeTrue();
        result.Serie.Should().Be("Tangente");
        result.Authors.Should().Be("Céline Wagner");
        result.NumberOfPages.Should().Be(82);
        result.ImageUrl.Should().Be(coverUrl.ToString());
        result.Source.Should().Be("BnF");
        await _bedethequeService.DidNotReceive().SearchByIsbnAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SearchByIsbnAsync_Should_TakeTheOpenLibraryCover_WhenTheBnfKnowsTheBookButGoogleHasNoCover()
    {
        // Arrange
        const string isbn = "9782917237359";
        var coverUrl = new Uri("https://covers.openlibrary.org/b/id/42-L.jpg");
        _bnfService.SearchByIsbnAsync(isbn, Arg.Any<CancellationToken>())
            .Returns(new BnfBookResult("Tangente", null, [], [], null, null, null, Found: true));
        _openLibraryService.SearchByIsbnAsync(isbn, Arg.Any<CancellationToken>())
            .Returns(new OpenLibraryBookResult("Tangente", null, [], [], null, null, coverUrl, Found: true));

        // Act
        var result = await _sut.SearchByIsbnAsync(isbn, TestContext.Current.CancellationToken);

        // Assert
        result.ImageUrl.Should().Be(coverUrl.ToString());
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

        _openLibraryService.SearchByIsbnAsync(isbn, Arg.Any<CancellationToken>())
            .Returns(openLibraryResult);
        _googleBooksService.SearchByIsbnAsync(isbn, Arg.Any<CancellationToken>())
            .Returns(googleBooksResult);

        // Act
        var result = await _sut.SearchByIsbnAsync(isbn, TestContext.Current.CancellationToken);

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

    #region UploadCoverAsync Tests

    #endregion
}

internal static class BubbleNotFoundFactory
{
    public static IBubbleService Create()
    {
        var service = Substitute.For<IBubbleService>();
        service.SearchByIsbnAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new BubbleBookResult(string.Empty, string.Empty, 0, [], [], null, null, null, Found: false));
        return service;
    }
}

internal static class BnfNotFoundFactory
{
    public static IBnfCatalogueService Create()
    {
        var service = Substitute.For<IBnfCatalogueService>();
        service.SearchByIsbnAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new BnfBookResult(string.Empty, null, [], [], null, null, null, Found: false));
        return service;
    }
}
