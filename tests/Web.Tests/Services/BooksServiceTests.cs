using Application.Abstractions.Messaging;
using Application.Books.Create;
using Application.Books.Delete;
using Application.Books.GetById;
using Application.Books.List;
using Application.Books.Update;
using Ardalis.GuardClauses;
using AwesomeAssertions;
using Domain.Books;
using Domain.Primitives;
using NSubstitute;
using Web.Services;
using Xunit;

namespace Web.Tests.Services;

public sealed class BooksServiceTests
{
    private readonly IQueryHandler<GetBookByIdQuery, Book> _getBookByIdHandler;
    private readonly IQueryHandler<GetBooksQuery, List<Book>> _getBooksHandler;
    private readonly ICommandHandler<CreateBookCommand, Book> _createBookHandler;
    private readonly ICommandHandler<UpdateBookCommand, Book> _updateBookHandler;
    private readonly ICommandHandler<DeleteBookCommand> _deleteBookHandler;
    private readonly BooksService _service;

    public BooksServiceTests()
    {
        _getBookByIdHandler = Substitute.For<IQueryHandler<GetBookByIdQuery, Book>>();
        _getBooksHandler = Substitute.For<IQueryHandler<GetBooksQuery, List<Book>>>();
        _createBookHandler = Substitute.For<ICommandHandler<CreateBookCommand, Book>>();
        _updateBookHandler = Substitute.For<ICommandHandler<UpdateBookCommand, Book>>();
        _deleteBookHandler = Substitute.For<ICommandHandler<DeleteBookCommand>>();

        _service = new BooksService(
            _getBookByIdHandler,
            _getBooksHandler,
            _createBookHandler,
            _updateBookHandler,
            _deleteBookHandler);
    }

    #region GetById Tests

    [Fact]
    public async Task GetById_ShouldReturnValidationError_WhenIdIsNull()
    {
        // Arrange
        string? id = null;

        // Act
        var result = await _service.GetById(id);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(BooksError.ValidationError);
        await _getBookByIdHandler.DidNotReceive().Handle(Arg.Any<GetBookByIdQuery>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetById_ShouldReturnValidationError_WhenIdIsEmpty()
    {
        // Arrange
        var id = string.Empty;

        // Act
        var result = await _service.GetById(id);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(BooksError.ValidationError);
        await _getBookByIdHandler.DidNotReceive().Handle(Arg.Any<GetBookByIdQuery>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetById_ShouldReturnValidationError_WhenIdIsInvalidGuid()
    {
        // Arrange
        const string id = "invalid-guid";

        // Act
        var result = await _service.GetById(id);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(BooksError.ValidationError);
        await _getBookByIdHandler.DidNotReceive().Handle(Arg.Any<GetBookByIdQuery>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetById_ShouldCallHandler_WhenIdIsValidGuid()
    {
        // Arrange
        var bookId = Guid.CreateVersion7();
        var book = Book.Create("Test Serie", "Test Title", "978-3-16-148410-0");
        _getBookByIdHandler.Handle(Arg.Any<GetBookByIdQuery>(), Arg.Any<CancellationToken>())
            .Returns(book);

        // Act
        var result = await _service.GetById(bookId.ToString());

        // Assert
        result.IsSuccess.Should().BeTrue();
        await _getBookByIdHandler.Received(1).Handle(
            Arg.Is<GetBookByIdQuery>(q => q.Id == bookId),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetById_ShouldReturnBook_WhenBookExists()
    {
        // Arrange
        var bookId = Guid.CreateVersion7();
        var expectedBook = Book.Create("Serie", "Title", "978-3-16-148410-0", 5, "https://example.com/cover.jpg");
        _getBookByIdHandler.Handle(Arg.Any<GetBookByIdQuery>(), Arg.Any<CancellationToken>())
            .Returns(expectedBook);

        // Act
        var result = await _service.GetById(bookId.ToString());

        // Assert
        Guard.Against.Null(result.Value);
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(expectedBook);
        result.Value.Serie.Should().Be("Serie");
        result.Value.Title.Should().Be("Title");
        result.Value.VolumeNumber.Should().Be(5);
    }

    #endregion

    #region Create Tests (3 parameters)

    [Fact]
    public async Task Create_With3Parameters_ShouldCallFullCreateWithDefaults()
    {
        // Arrange
        const string series = "Test Series";
        const string title = "Test Title";
        const string isbn = "978-3-16-148410-0";
        var book = Book.Create(series, title, isbn, 1, "");
        _createBookHandler.Handle(Arg.Any<CreateBookCommand>(), Arg.Any<CancellationToken>())
            .Returns(book);

        // Act
        var result = await _service.Create(series, title, isbn);

        // Assert
        result.IsSuccess.Should().BeTrue();
        await _createBookHandler.Received(1).Handle(
            Arg.Is<CreateBookCommand>(c =>
                c.Serie == series &&
                c.Title == title &&
                c.ISBN == isbn &&
                c.VolumeNumber == 1 &&
                c.ImageLink == ""),
            Arg.Any<CancellationToken>());
    }

    #endregion

    #region Create Tests (4 parameters)

    [Fact]
    public async Task Create_With4Parameters_ShouldCallFullCreateWithDefaults()
    {
        // Arrange
        const string series = "Test Series";
        const string title = "Test Title";
        const string isbn = "978-3-16-148410-0";
        const int volumeNumber = 3;
        var book = Book.Create(series, title, isbn, volumeNumber, "");
        _createBookHandler.Handle(Arg.Any<CreateBookCommand>(), Arg.Any<CancellationToken>())
            .Returns(book);

        // Act
        var result = await _service.Create(series, title, isbn, volumeNumber);

        // Assert
        result.IsSuccess.Should().BeTrue();
        await _createBookHandler.Received(1).Handle(
            Arg.Is<CreateBookCommand>(c =>
                c.Serie == series &&
                c.Title == title &&
                c.ISBN == isbn &&
                c.VolumeNumber == volumeNumber &&
                c.ImageLink == ""),
            Arg.Any<CancellationToken>());
    }

    #endregion

    #region Create Tests (6 parameters)

    [Fact]
    public async Task Create_With6Parameters_ShouldCallHandlerWithAllParameters()
    {
        // Arrange
        const string series = "Test Series";
        const string title = "Test Title";
        const string isbn = "978-3-16-148410-0";
        const int volumeNumber = 5;
        const string imageLink = "https://example.com/cover.jpg";
        const int rating = 4;
        var book = Book.Create(series, title, isbn, volumeNumber, imageLink, rating);
        _createBookHandler.Handle(Arg.Any<CreateBookCommand>(), Arg.Any<CancellationToken>())
            .Returns(book);

        // Act
        var result = await _service.Create(series, title, isbn, volumeNumber, imageLink, rating);

        // Assert
        result.IsSuccess.Should().BeTrue();
        await _createBookHandler.Received(1).Handle(
            Arg.Is<CreateBookCommand>(c =>
                c.Serie == series &&
                c.Title == title &&
                c.ISBN == isbn &&
                c.VolumeNumber == volumeNumber &&
                c.ImageLink == imageLink &&
                c.Rating == rating),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Create_With6Parameters_ShouldReturnCreatedBook()
    {
        // Arrange
        const string series = "Marvel";
        const string title = "Spider-Man";
        const string isbn = "978-3-16-148410-0";
        const int volumeNumber = 10;
        const string imageLink = "https://example.com/spiderman.jpg";
        const int rating = 5;
        var expectedBook = Book.Create(series, title, isbn, volumeNumber, imageLink, rating);
        _createBookHandler.Handle(Arg.Any<CreateBookCommand>(), Arg.Any<CancellationToken>())
            .Returns(expectedBook);

        // Act
        var result = await _service.Create(series, title, isbn, volumeNumber, imageLink, rating);

        // Assert
        Guard.Against.Null(result.Value);
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(expectedBook);
        result.Value.Serie.Should().Be(series);
        result.Value.Title.Should().Be(title);
        result.Value.ISBN.Should().Be(isbn);
        result.Value.VolumeNumber.Should().Be(volumeNumber);
        result.Value.ImageLink.Should().Be(imageLink);
        result.Value.Rating.Should().Be(rating);
    }

    [Fact]
    public async Task Create_With6Parameters_ShouldPassCancellationToken()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var book = Book.Create("Series", "Title", "978-3-16-148410-0");
        _createBookHandler.Handle(Arg.Any<CreateBookCommand>(), Arg.Any<CancellationToken>())
            .Returns(book);

        // Act
        await _service.Create("Series", "Title", "978-3-16-148410-0", 1, "", 0, cts.Token);

        // Assert
        await _createBookHandler.Received(1).Handle(
            Arg.Any<CreateBookCommand>(),
            Arg.Is<CancellationToken>(ct => ct == cts.Token));
    }

    [Fact]
    public async Task Create_With6Parameters_ShouldForwardEmptyMetadataToFullOverload()
    {
        // Arrange
        const string series = "Test Series";
        const string title = "Test Title";
        const string isbn = "978-3-16-148410-0";
        const int volumeNumber = 5;
        const string imageLink = "https://example.com/cover.jpg";
        const int rating = 4;
        var book = Book.Create(series, title, isbn, volumeNumber, imageLink, rating);
        _createBookHandler.Handle(Arg.Any<CreateBookCommand>(), Arg.Any<CancellationToken>())
            .Returns(book);

        // Act
        var result = await _service.Create(series, title, isbn, volumeNumber, imageLink, rating);

        // Assert
        result.IsSuccess.Should().BeTrue();
        await _createBookHandler.Received(1).Handle(
            Arg.Is<CreateBookCommand>(c =>
                c.Serie == series &&
                c.Title == title &&
                c.ISBN == isbn &&
                c.VolumeNumber == volumeNumber &&
                c.ImageLink == imageLink &&
                c.Rating == rating &&
                c.Authors == "" &&
                c.Publishers == "" &&
                c.PublishDate == null &&
                c.NumberOfPages == null),
            Arg.Any<CancellationToken>());
    }

    #endregion

    #region Create Tests (10 parameters - with metadata)

    [Fact]
    public async Task Create_With10Parameters_ShouldCallHandlerWithAllMetadataFields()
    {
        // Arrange
        const string series = "Saga";
        const string title = "Saga, Volume 1";
        const string isbn = "978-1-60706-601-9";
        const int volumeNumber = 1;
        const string imageLink = "https://example.com/saga.jpg";
        const int rating = 5;
        const string authors = "Brian K. Vaughan, Fiona Staples";
        const string publishers = "Image Comics";
        var publishDate = new DateOnly(2012, 10, 10);
        const int numberOfPages = 160;
        var book = Book.Create(series, title, isbn, volumeNumber, imageLink, rating, authors, publishers, publishDate, numberOfPages);
        _createBookHandler.Handle(Arg.Any<CreateBookCommand>(), Arg.Any<CancellationToken>())
            .Returns(book);

        // Act
        var result = await _service.Create(series, title, isbn, volumeNumber, imageLink, rating, authors, publishers, publishDate, numberOfPages);

        // Assert
        result.IsSuccess.Should().BeTrue();
        await _createBookHandler.Received(1).Handle(
            Arg.Is<CreateBookCommand>(c =>
                c.Serie == series &&
                c.Title == title &&
                c.ISBN == isbn &&
                c.VolumeNumber == volumeNumber &&
                c.ImageLink == imageLink &&
                c.Rating == rating &&
                c.Authors == authors &&
                c.Publishers == publishers &&
                c.PublishDate == publishDate &&
                c.NumberOfPages == numberOfPages),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Create_With10Parameters_ShouldReturnCreatedBookWithMetadata()
    {
        // Arrange
        const string series = "Watchmen";
        const string title = "Watchmen";
        const string isbn = "978-0-930289-23-2";
        const int volumeNumber = 1;
        const string imageLink = "https://example.com/watchmen.jpg";
        const int rating = 5;
        const string authors = "Alan Moore, Dave Gibbons";
        const string publishers = "DC Comics";
        var publishDate = new DateOnly(1987, 9, 1);
        const int numberOfPages = 320;
        var expectedBook = Book.Create(series, title, isbn, volumeNumber, imageLink, rating, authors, publishers, publishDate, numberOfPages);
        _createBookHandler.Handle(Arg.Any<CreateBookCommand>(), Arg.Any<CancellationToken>())
            .Returns(expectedBook);

        // Act
        var result = await _service.Create(series, title, isbn, volumeNumber, imageLink, rating, authors, publishers, publishDate, numberOfPages);

        // Assert
        Guard.Against.Null(result.Value);
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(expectedBook);
        result.Value.Serie.Should().Be(series);
        result.Value.Title.Should().Be(title);
        result.Value.ISBN.Should().Be(isbn);
        result.Value.VolumeNumber.Should().Be(volumeNumber);
        result.Value.ImageLink.Should().Be(imageLink);
        result.Value.Rating.Should().Be(rating);
        result.Value.Authors.Should().Be(authors);
        result.Value.Publishers.Should().Be(publishers);
        result.Value.PublishDate.Should().Be(publishDate);
        result.Value.NumberOfPages.Should().Be(numberOfPages);
    }

    [Fact]
    public async Task Create_With10Parameters_ShouldPassCancellationToken()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var publishDate = new DateOnly(2020, 1, 1);
        var book = Book.Create("Series", "Title", "978-3-16-148410-0", 1, "", 0, "Author", "Publisher", publishDate, 100);
        _createBookHandler.Handle(Arg.Any<CreateBookCommand>(), Arg.Any<CancellationToken>())
            .Returns(book);

        // Act
        await _service.Create("Series", "Title", "978-3-16-148410-0", 1, "", 0, "Author", "Publisher", publishDate, 100, cts.Token);

        // Assert
        await _createBookHandler.Received(1).Handle(
            Arg.Any<CreateBookCommand>(),
            Arg.Is<CancellationToken>(ct => ct == cts.Token));
    }

    [Fact]
    public async Task Create_With10Parameters_ShouldHandleNullPublishDate()
    {
        // Arrange
        const string series = "Unknown Date Comic";
        const string title = "Mystery";
        const string isbn = "978-3-16-148410-0";
        const string authors = "Unknown Author";
        const string publishers = "Unknown Publisher";
        var book = Book.Create(series, title, isbn, 1, "", 0, authors, publishers, null, null);
        _createBookHandler.Handle(Arg.Any<CreateBookCommand>(), Arg.Any<CancellationToken>())
            .Returns(book);

        // Act
        var result = await _service.Create(series, title, isbn, 1, "", 0, authors, publishers, null, null);

        // Assert
        result.IsSuccess.Should().BeTrue();
        await _createBookHandler.Received(1).Handle(
            Arg.Is<CreateBookCommand>(c =>
                c.Authors == authors &&
                c.Publishers == publishers &&
                c.PublishDate == null &&
                c.NumberOfPages == null),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Create_With10Parameters_ShouldHandleEmptyAuthorsAndPublishers()
    {
        // Arrange
        var book = Book.Create("Series", "Title", "978-3-16-148410-0", 1, "", 0, "", "", null, null);
        _createBookHandler.Handle(Arg.Any<CreateBookCommand>(), Arg.Any<CancellationToken>())
            .Returns(book);

        // Act
        var result = await _service.Create("Series", "Title", "978-3-16-148410-0", 1, "", 0, "", "", null, null);

        // Assert
        result.IsSuccess.Should().BeTrue();
        await _createBookHandler.Received(1).Handle(
            Arg.Is<CreateBookCommand>(c =>
                c.Authors == "" &&
                c.Publishers == ""),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Create_With10Parameters_ShouldHandleMultipleAuthorsAndPublishers()
    {
        // Arrange
        const string authors = "Stan Lee, Jack Kirby, Steve Ditko";
        const string publishers = "Marvel Comics, Timely Comics";
        var book = Book.Create("Marvel", "Fantastic Four", "978-3-16-148410-0", 1, "", 0, authors, publishers, null, null);
        _createBookHandler.Handle(Arg.Any<CreateBookCommand>(), Arg.Any<CancellationToken>())
            .Returns(book);

        // Act
        var result = await _service.Create("Marvel", "Fantastic Four", "978-3-16-148410-0", 1, "", 0, authors, publishers, null, null);

        // Assert
        result.IsSuccess.Should().BeTrue();
        await _createBookHandler.Received(1).Handle(
            Arg.Is<CreateBookCommand>(c =>
                c.Authors == authors &&
                c.Publishers == publishers),
            Arg.Any<CancellationToken>());
    }

    #endregion

    #region Update Tests (10 parameters - with metadata)

    [Fact]
    public async Task Update_With10Parameters_ShouldCallHandlerWithAllMetadataFields()
    {
        // Arrange
        var bookId = Guid.CreateVersion7();
        const string series = "The Walking Dead";
        const string title = "Days Gone Bye";
        const string isbn = "978-1-582406-72-1";
        const int volumeNumber = 1;
        const string imageLink = "https://example.com/twd.jpg";
        const int rating = 5;
        const string authors = "Robert Kirkman, Tony Moore";
        const string publishers = "Image Comics";
        var publishDate = new DateOnly(2004, 10, 1);
        const int numberOfPages = 144;
        var book = Book.Create(series, title, isbn, volumeNumber, imageLink, rating, authors, publishers, publishDate, numberOfPages);
        _updateBookHandler.Handle(Arg.Any<UpdateBookCommand>(), Arg.Any<CancellationToken>())
            .Returns(book);

        // Act
        var result = await _service.Update(bookId.ToString(), series, title, isbn, volumeNumber, imageLink, rating, authors, publishers, publishDate, numberOfPages);

        // Assert
        result.IsSuccess.Should().BeTrue();
        await _updateBookHandler.Received(1).Handle(
            Arg.Is<UpdateBookCommand>(c =>
                c.Id == bookId &&
                c.Serie == series &&
                c.Title == title &&
                c.ISBN == isbn &&
                c.VolumeNumber == volumeNumber &&
                c.ImageLink == imageLink &&
                c.Rating == rating &&
                c.Authors == authors &&
                c.Publishers == publishers &&
                c.PublishDate == publishDate &&
                c.NumberOfPages == numberOfPages),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Update_With10Parameters_ShouldReturnUpdatedBookWithMetadata()
    {
        // Arrange
        var bookId = Guid.CreateVersion7();
        const string series = "Y: The Last Man";
        const string title = "Unmanned";
        const string isbn = "978-1-56389-980-9";
        const int volumeNumber = 1;
        const string imageLink = "https://example.com/ytlm.jpg";
        const int rating = 5;
        const string authors = "Brian K. Vaughan, Pia Guerra";
        const string publishers = "Vertigo";
        var publishDate = new DateOnly(2003, 4, 1);
        const int numberOfPages = 128;
        var expectedBook = Book.Create(series, title, isbn, volumeNumber, imageLink, rating, authors, publishers, publishDate, numberOfPages);
        _updateBookHandler.Handle(Arg.Any<UpdateBookCommand>(), Arg.Any<CancellationToken>())
            .Returns(expectedBook);

        // Act
        var result = await _service.Update(bookId.ToString(), series, title, isbn, volumeNumber, imageLink, rating, authors, publishers, publishDate, numberOfPages);

        // Assert
        Guard.Against.Null(result.Value);
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(expectedBook);
        result.Value.Serie.Should().Be(series);
        result.Value.Title.Should().Be(title);
        result.Value.Authors.Should().Be(authors);
        result.Value.Publishers.Should().Be(publishers);
        result.Value.PublishDate.Should().Be(publishDate);
        result.Value.NumberOfPages.Should().Be(numberOfPages);
    }

    [Fact]
    public async Task Update_With10Parameters_ShouldPassCancellationToken()
    {
        // Arrange
        var bookId = Guid.CreateVersion7();
        using var cts = new CancellationTokenSource();
        var publishDate = new DateOnly(2020, 5, 15);
        var book = Book.Create("Series", "Title", "978-3-16-148410-0", 1, "", 0, "Author", "Publisher", publishDate, 200);
        _updateBookHandler.Handle(Arg.Any<UpdateBookCommand>(), Arg.Any<CancellationToken>())
            .Returns(book);

        // Act
        await _service.Update(bookId.ToString(), "Series", "Title", "978-3-16-148410-0", 1, "", 0, "Author", "Publisher", publishDate, 200, cts.Token);

        // Assert
        await _updateBookHandler.Received(1).Handle(
            Arg.Any<UpdateBookCommand>(),
            Arg.Is<CancellationToken>(ct => ct == cts.Token));
    }

    [Fact]
    public async Task Update_With10Parameters_ShouldReturnValidationError_WhenIdIsNull()
    {
        // Arrange
        string? id = null;
        var publishDate = new DateOnly(2020, 1, 1);

        // Act
        var result = await _service.Update(id, "Series", "Title", "978-3-16-148410-0", 1, "", 0, "Author", "Publisher", publishDate, 100);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(BooksError.ValidationError);
        await _updateBookHandler.DidNotReceive().Handle(Arg.Any<UpdateBookCommand>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Update_With10Parameters_ShouldReturnValidationError_WhenIdIsInvalidGuid()
    {
        // Arrange
        const string id = "not-a-valid-guid";
        var publishDate = new DateOnly(2020, 1, 1);

        // Act
        var result = await _service.Update(id, "Series", "Title", "978-3-16-148410-0", 1, "", 0, "Author", "Publisher", publishDate, 100);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(BooksError.ValidationError);
        await _updateBookHandler.DidNotReceive().Handle(Arg.Any<UpdateBookCommand>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Update_With10Parameters_ShouldHandleNullMetadata()
    {
        // Arrange
        var bookId = Guid.CreateVersion7();
        var book = Book.Create("Series", "Title", "978-3-16-148410-0", 1, "", 0, "", "", null, null);
        _updateBookHandler.Handle(Arg.Any<UpdateBookCommand>(), Arg.Any<CancellationToken>())
            .Returns(book);

        // Act
        var result = await _service.Update(bookId.ToString(), "Series", "Title", "978-3-16-148410-0", 1, "", 0, "", "", null, null);

        // Assert
        result.IsSuccess.Should().BeTrue();
        await _updateBookHandler.Received(1).Handle(
            Arg.Is<UpdateBookCommand>(c =>
                c.Authors == "" &&
                c.Publishers == "" &&
                c.PublishDate == null &&
                c.NumberOfPages == null),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Update_With10Parameters_ShouldAllowUpdatingOnlyMetadata()
    {
        // Arrange
        var bookId = Guid.CreateVersion7();
        var originalPublishDate = new DateOnly(2020, 1, 1);
        var newPublishDate = new DateOnly(2021, 6, 15);
        var book = Book.Create("Series", "Title", "978-3-16-148410-0", 1, "", 0, "New Author", "New Publisher", newPublishDate, 250);
        _updateBookHandler.Handle(Arg.Any<UpdateBookCommand>(), Arg.Any<CancellationToken>())
            .Returns(book);

        // Act
        var result = await _service.Update(bookId.ToString(), "Series", "Title", "978-3-16-148410-0", 1, "", 0, "New Author", "New Publisher", newPublishDate, 250);

        // Assert
        result.IsSuccess.Should().BeTrue();
        await _updateBookHandler.Received(1).Handle(
            Arg.Is<UpdateBookCommand>(c =>
                c.Authors == "New Author" &&
                c.Publishers == "New Publisher" &&
                c.PublishDate == newPublishDate &&
                c.NumberOfPages == 250),
            Arg.Any<CancellationToken>());
    }

    #endregion

    #region Update Tests

    [Fact]
    public async Task Update_ShouldReturnValidationError_WhenIdIsNull()
    {
        // Arrange
        string? id = null;

        // Act
        var result = await _service.Update(id, "Series", "Title", "978-3-16-148410-0", 1, "", 0);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(BooksError.ValidationError);
        await _updateBookHandler.DidNotReceive().Handle(Arg.Any<UpdateBookCommand>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Update_ShouldReturnValidationError_WhenIdIsInvalidGuid()
    {
        // Arrange
        const string id = "not-a-guid";

        // Act
        var result = await _service.Update(id, "Series", "Title", "978-3-16-148410-0", 1, "", 0);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(BooksError.ValidationError);
        await _updateBookHandler.DidNotReceive().Handle(Arg.Any<UpdateBookCommand>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Update_ShouldCallHandler_WhenIdIsValidGuid()
    {
        // Arrange
        var bookId = Guid.CreateVersion7();
        const string series = "Updated Series";
        const string title = "Updated Title";
        const string isbn = "978-3-16-148410-0";
        const int volumeNumber = 7;
        const string imageLink = "https://example.com/updated.jpg";
        const int rating = 3;
        var book = Book.Create(series, title, isbn, volumeNumber, imageLink, rating);
        _getBookByIdHandler.Handle(Arg.Any<GetBookByIdQuery>(), Arg.Any<CancellationToken>())
            .Returns(book);
        _updateBookHandler.Handle(Arg.Any<UpdateBookCommand>(), Arg.Any<CancellationToken>())
            .Returns(book);

        // Act
        var result = await _service.Update(bookId.ToString(), series, title, isbn, volumeNumber, imageLink, rating);

        // Assert
        result.IsSuccess.Should().BeTrue();
        await _updateBookHandler.Received(1).Handle(
            Arg.Is<UpdateBookCommand>(c =>
                c.Id == bookId &&
                c.Serie == series &&
                c.Title == title &&
                c.ISBN == isbn &&
                c.VolumeNumber == volumeNumber &&
                c.ImageLink == imageLink &&
                c.Rating == rating),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Update_ShouldReturnUpdatedBook()
    {
        // Arrange
        var bookId = Guid.CreateVersion7();
        const string series = "DC Comics";
        const string title = "Batman";
        const string isbn = "978-3-16-148410-0";
        const int volumeNumber = 15;
        const string imageLink = "https://example.com/batman.jpg";
        const int rating = 5;
        var expectedBook = Book.Create(series, title, isbn, volumeNumber, imageLink, rating);
        _getBookByIdHandler.Handle(Arg.Any<GetBookByIdQuery>(), Arg.Any<CancellationToken>())
            .Returns(expectedBook);
        _updateBookHandler.Handle(Arg.Any<UpdateBookCommand>(), Arg.Any<CancellationToken>())
            .Returns(expectedBook);

        // Act
        var result = await _service.Update(bookId.ToString(), series, title, isbn, volumeNumber, imageLink, rating);

        // Assert
        Guard.Against.Null(result.Value);
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(expectedBook);
        result.Value.Serie.Should().Be(series);
        result.Value.Title.Should().Be(title);
        result.Value.VolumeNumber.Should().Be(volumeNumber);
        result.Value.Rating.Should().Be(rating);
    }

    [Fact]
    public async Task Update_ShouldPassCancellationToken()
    {
        // Arrange
        var bookId = Guid.CreateVersion7();
        using var cts = new CancellationTokenSource();
        var book = Book.Create("Series", "Title", "978-3-16-148410-0");
        _getBookByIdHandler.Handle(Arg.Any<GetBookByIdQuery>(), Arg.Any<CancellationToken>())
            .Returns(book);
        _updateBookHandler.Handle(Arg.Any<UpdateBookCommand>(), Arg.Any<CancellationToken>())
            .Returns(book);

        // Act
        await _service.Update(bookId.ToString(), "Series", "Title", "978-3-16-148410-0", 1, "", 0, cts.Token);

        // Assert
        await _updateBookHandler.Received(1).Handle(
            Arg.Any<UpdateBookCommand>(),
            Arg.Is<CancellationToken>(ct => ct == cts.Token));
    }

    [Fact]
    public async Task Update_With7Parameters_ShouldPreserveMetadata_WhenBookHasExistingMetadata()
    {
        // Arrange
        var bookId = Guid.CreateVersion7();
        var publishDate = new DateOnly(2023, 6, 15);
        var existingBook = Book.Create("Series", "Title", "978-3-16-148410-0", 1, "", 0,
            "Brian K. Vaughan", "Image Comics", publishDate, 160);
        _getBookByIdHandler.Handle(Arg.Any<GetBookByIdQuery>(), Arg.Any<CancellationToken>())
            .Returns(existingBook);
        _updateBookHandler.Handle(Arg.Any<UpdateBookCommand>(), Arg.Any<CancellationToken>())
            .Returns(existingBook);

        // Act
        var result = await _service.Update(bookId.ToString(), "Updated Series", "Updated Title", "978-3-16-148410-0", 2, "http://example.com/img.jpg", 5);

        // Assert
        result.IsSuccess.Should().BeTrue();
        await _updateBookHandler.Received(1).Handle(
            Arg.Is<UpdateBookCommand>(c =>
                c.Id == bookId &&
                c.Serie == "Updated Series" &&
                c.Title == "Updated Title" &&
                c.Authors == "Brian K. Vaughan" &&
                c.Publishers == "Image Comics" &&
                c.PublishDate == publishDate &&
                c.NumberOfPages == 160),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Update_With7Parameters_ShouldReturnFailure_WhenBookNotFound()
    {
        // Arrange
        var bookId = Guid.CreateVersion7();
        _getBookByIdHandler.Handle(Arg.Any<GetBookByIdQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result<Book>.Failure(BooksError.NotFound));

        // Act
        var result = await _service.Update(bookId.ToString(), "Series", "Title", "978-3-16-148410-0", 1, "", 0);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(BooksError.NotFound);
        await _updateBookHandler.DidNotReceive().Handle(Arg.Any<UpdateBookCommand>(), Arg.Any<CancellationToken>());
    }

    #endregion

    #region GetAll Tests

    [Fact]
    public async Task GetAll_ShouldCallHandler()
    {
        // Arrange
        var books = new List<Book>
        {
            Book.Create("Series 1", "Title 1", "978-3-16-148410-0"),
            Book.Create("Series 2", "Title 2", "978-3-16-148410-1")
        };
        _getBooksHandler.Handle(Arg.Any<GetBooksQuery>(), Arg.Any<CancellationToken>())
            .Returns(books);

        // Act
        var result = await _service.GetAll();

        // Assert
        result.IsSuccess.Should().BeTrue();
        await _getBooksHandler.Received(1).Handle(
            Arg.Any<GetBooksQuery>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetAll_ShouldReturnListOfBooks()
    {
        // Arrange
        var expectedBooks = new List<Book>
        {
            Book.Create("Series 1", "Title 1", "978-3-16-148410-0", 1, "https://example.com/1.jpg"),
            Book.Create("Series 2", "Title 2", "978-3-16-148410-1", 2, "https://example.com/2.jpg"),
            Book.Create("Series 3", "Title 3", "978-3-16-148410-2", 3, "https://example.com/3.jpg")
        };
        _getBooksHandler.Handle(Arg.Any<GetBooksQuery>(), Arg.Any<CancellationToken>())
            .Returns(expectedBooks);

        // Act
        var result = await _service.GetAll();

        // Assert
        Guard.Against.Null(result.Value);
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(3);
        result.Value.Should().BeEquivalentTo(expectedBooks);
    }

    [Fact]
    public async Task GetAll_ShouldReturnEmptyList_WhenNoBooksExist()
    {
        // Arrange
        var emptyList = new List<Book>();
        _getBooksHandler.Handle(Arg.Any<GetBooksQuery>(), Arg.Any<CancellationToken>())
            .Returns(emptyList);

        // Act
        var result = await _service.GetAll();

        // Assert
        Guard.Against.Null(result.Value);
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    #endregion

    #region Delete Tests

    [Fact]
    public async Task Delete_ShouldReturnValidationError_WhenIdIsNull()
    {
        // Arrange
        string? id = null;

        // Act
        var result = await _service.Delete(id);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(BooksError.ValidationError);
        await _deleteBookHandler.DidNotReceive().Handle(Arg.Any<DeleteBookCommand>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Delete_ShouldReturnValidationError_WhenIdIsEmpty()
    {
        // Arrange
        var id = string.Empty;

        // Act
        var result = await _service.Delete(id);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(BooksError.ValidationError);
        await _deleteBookHandler.DidNotReceive().Handle(Arg.Any<DeleteBookCommand>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Delete_ShouldReturnValidationError_WhenIdIsInvalidGuid()
    {
        // Arrange
        const string id = "invalid-guid-format";

        // Act
        var result = await _service.Delete(id);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(BooksError.ValidationError);
        await _deleteBookHandler.DidNotReceive().Handle(Arg.Any<DeleteBookCommand>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Delete_ShouldCallHandler_WhenIdIsValidGuid()
    {
        // Arrange
        var bookId = Guid.CreateVersion7();
        _deleteBookHandler.Handle(Arg.Any<DeleteBookCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        // Act
        var result = await _service.Delete(bookId.ToString());

        // Assert
        result.IsSuccess.Should().BeTrue();
        await _deleteBookHandler.Received(1).Handle(
            Arg.Is<DeleteBookCommand>(c => c.Id == bookId),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Delete_ShouldReturnSuccess_WhenDeletionSucceeds()
    {
        // Arrange
        var bookId = Guid.CreateVersion7();
        _deleteBookHandler.Handle(Arg.Any<DeleteBookCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        // Act
        var result = await _service.Delete(bookId.ToString());

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Delete_ShouldPassCancellationToken()
    {
        // Arrange
        var bookId = Guid.CreateVersion7();
        using var cts = new CancellationTokenSource();
        _deleteBookHandler.Handle(Arg.Any<DeleteBookCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        // Act
        await _service.Delete(bookId.ToString(), cts.Token);

        // Assert
        await _deleteBookHandler.Received(1).Handle(
            Arg.Any<DeleteBookCommand>(),
            Arg.Is<CancellationToken>(ct => ct == cts.Token));
    }

    [Fact]
    public async Task Delete_ShouldReturnFailure_WhenHandlerReturnsFailure()
    {
        // Arrange
        var bookId = Guid.CreateVersion7();
        var expectedError = BooksError.NotFound;
        _deleteBookHandler.Handle(Arg.Any<DeleteBookCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure(expectedError));

        // Act
        var result = await _service.Delete(bookId.ToString());

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(expectedError);
    }

    #endregion
}
