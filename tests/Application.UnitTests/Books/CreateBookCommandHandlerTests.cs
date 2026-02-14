using Application.Books.Create;
using Application.Helpers;
using Application.Interfaces;
using Ardalis.GuardClauses;
using Domain.Books;
using NSubstitute;

namespace Application.UnitTests.Books;

public class CreateBookCommandHandlerTests
{
    private static readonly CreateBookCommand s_validCommand = new(
        "Test Serie",
        "Test Title",
        "978-3-16-148410-0",
        1,
        "https://example.com/image.jpg",
        4
    );

    private readonly CreateBookCommandHandler _handler;
    private readonly IBookRepository _bookRepositoryMock;
    private readonly IUnitOfWork _unitOfWorkMock;

    public CreateBookCommandHandlerTests()
    {
        _bookRepositoryMock = Substitute.For<IBookRepository>();
        _unitOfWorkMock = Substitute.For<IUnitOfWork>();

        _handler = new CreateBookCommandHandler(_bookRepositoryMock, _unitOfWorkMock);
    }

    [Fact]
    public async Task Handle_Should_ReturnSuccess_WhenValidCommandProvided()
    {
        // Arrange
        var normalizedIsbn = IsbnHelper.NormalizeIsbn(s_validCommand.ISBN);
        _bookRepositoryMock.GetByIsbnAsync(Arg.Is<string>(s => s == normalizedIsbn), Arg.Any<CancellationToken>()).Returns((Book?)null);

        // Act
        var result = await _handler.Handle(s_validCommand, default);

        // Assert
        Guard.Against.Null(result.Value);
        result.IsSuccess.Should().BeTrue();
        result.Value.Serie.Should().Be(s_validCommand.Serie);
        result.Value.Title.Should().Be(s_validCommand.Title);
        result.Value.ISBN.Should().Be(IsbnHelper.NormalizeIsbn(s_validCommand.ISBN));
        result.Value.VolumeNumber.Should().Be(s_validCommand.VolumeNumber);
        result.Value.ImageLink.Should().Be(s_validCommand.ImageLink);
        result.Value.Rating.Should().Be(s_validCommand.Rating);
        result.Value.ReadingDates.Should().HaveCount(1);
        _bookRepositoryMock.Received(1).Add(Arg.Any<Book>());
        await _unitOfWorkMock.Received(1).SaveChangesAsync(CancellationToken.None);
    }

    [Fact]
    public async Task Handle_Should_ExecuteSaveChangesAsyncOnce()
    {
        // Arrange
        var normalizedIsbn = IsbnHelper.NormalizeIsbn(s_validCommand.ISBN);
        _bookRepositoryMock.GetByIsbnAsync(Arg.Is<string>(s => s == normalizedIsbn), Arg.Any<CancellationToken>()).Returns((Book?)null);

        // Act
        await _handler.Handle(s_validCommand, default);

        // Assert
        await _unitOfWorkMock.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldReturnBadRequest_WhenTitleIsEmpty()
    {
        // Arrange
        var emptyTitleCommand = new CreateBookCommand("Serie", string.Empty, "978-3-16-148410-0", 1, "");

        // Act
        var result = await _handler.Handle(emptyTitleCommand, default);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(BooksError.BadRequest);
        _bookRepositoryMock.Received(0).Add(Arg.Any<Book>());
        await _unitOfWorkMock.Received(0).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldReturnBadRequest_WhenTitleIsWhitespace()
    {
        // Arrange
        var whitespaceCommand = new CreateBookCommand("Serie", "   ", "978-3-16-148410-0", 1, "");

        // Act
        var result = await _handler.Handle(whitespaceCommand, default);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(BooksError.BadRequest);
        _bookRepositoryMock.Received(0).Add(Arg.Any<Book>());
        await _unitOfWorkMock.Received(0).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldReturnBadRequest_WhenISBNIsEmpty()
    {
        // Arrange
        var emptyIsbnCommand = new CreateBookCommand("Serie", "Title", string.Empty, 1, "");

        // Act
        var result = await _handler.Handle(emptyIsbnCommand, default);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(BooksError.BadRequest);
        _bookRepositoryMock.Received(0).Add(Arg.Any<Book>());
        await _unitOfWorkMock.Received(0).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldReturnBadRequest_WhenISBNIsWhitespace()
    {
        // Arrange
        var whitespaceIsbnCommand = new CreateBookCommand("Serie", "Title", "   ", 1, "");

        // Act
        var result = await _handler.Handle(whitespaceIsbnCommand, default);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(BooksError.BadRequest);
        _bookRepositoryMock.Received(0).Add(Arg.Any<Book>());
        await _unitOfWorkMock.Received(0).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldReturnInvalidISBN_WhenISBNFormatIsInvalid()
    {
        // Arrange
        var invalidIsbnCommand = new CreateBookCommand("Serie", "Title", "invalid-isbn", 1, "");

        // Act
        var result = await _handler.Handle(invalidIsbnCommand, default);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(BooksError.InvalidISBN);
        _bookRepositoryMock.Received(0).Add(Arg.Any<Book>());
        await _unitOfWorkMock.Received(0).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldReturnInvalidISBN_WhenISBNHasInvalidLength()
    {
        // Arrange
        var invalidLengthCommand = new CreateBookCommand("Serie", "Title", "12345", 1, "");

        // Act
        var result = await _handler.Handle(invalidLengthCommand, default);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(BooksError.InvalidISBN);
        _bookRepositoryMock.Received(0).Add(Arg.Any<Book>());
        await _unitOfWorkMock.Received(0).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldReturnDuplicate_WhenBookWithSameISBNAlreadyExists()
    {
        // Arrange        
        var existingBook = Book.Create(s_validCommand.Serie, s_validCommand.Title, s_validCommand.ISBN);
        var normalizedIsbn = IsbnHelper.NormalizeIsbn(s_validCommand.ISBN);
        _bookRepositoryMock.GetByIsbnAsync(Arg.Is<string>(s => s == normalizedIsbn), Arg.Any<CancellationToken>()).Returns(existingBook);

        // Act
        var result = await _handler.Handle(s_validCommand, default);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(BooksError.Duplicate);
        _bookRepositoryMock.Received(0).Add(Arg.Any<Book>());
        await _unitOfWorkMock.Received(0).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_ReturnSuccess_WithISBN10Format()
    {
        // Arrange
        var isbn10Command = new CreateBookCommand("Serie", "Title", "0-306-40615-2", 1, "");
        var normalizedIsbn = IsbnHelper.NormalizeIsbn(isbn10Command.ISBN);
        _bookRepositoryMock.GetByIsbnAsync(Arg.Is<string>(s => s == normalizedIsbn), Arg.Any<CancellationToken>()).Returns((Book?)null);

        // Act
        var result = await _handler.Handle(isbn10Command, default);

        // Assert
        result.IsSuccess.Should().BeTrue();
        Guard.Against.Null(result.Value);
        result.Value.ISBN.Should().Be(IsbnHelper.NormalizeIsbn(isbn10Command.ISBN));
        _bookRepositoryMock.Received(1).Add(Arg.Any<Book>());
        await _unitOfWorkMock.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_ReturnSuccess_WithISBN13Format()
    {
        // Arrange
        var isbn13Command = new CreateBookCommand("Serie", "Title", "978-0-306-40615-7", 1, "");
        var normalizedIsbn = IsbnHelper.NormalizeIsbn(isbn13Command.ISBN);
        _bookRepositoryMock.GetByIsbnAsync(Arg.Is<string>(s => s == normalizedIsbn), Arg.Any<CancellationToken>()).Returns((Book?)null);

        // Act
        var result = await _handler.Handle(isbn13Command, default);

        // Assert
        result.IsSuccess.Should().BeTrue();
        Guard.Against.Null(result.Value);
        result.Value.ISBN.Should().Be(IsbnHelper.NormalizeIsbn(isbn13Command.ISBN));
        _bookRepositoryMock.Received(1).Add(Arg.Any<Book>());
        await _unitOfWorkMock.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_CreateBookWithDefaultValues_WhenOptionalParametersNotProvided()
    {
        // Arrange
        var minimalCommand = new CreateBookCommand("Serie", "Title", "978-3-16-148410-0");
        var normalizedIsbn = IsbnHelper.NormalizeIsbn(minimalCommand.ISBN);
        _bookRepositoryMock.GetByIsbnAsync(Arg.Is<string>(s => s == normalizedIsbn), Arg.Any<CancellationToken>()).Returns((Book?)null);

        // Act
        var result = await _handler.Handle(minimalCommand, default);

        // Assert
        result.IsSuccess.Should().BeTrue();
        Guard.Against.Null(result.Value);
        result.Value.VolumeNumber.Should().Be(1);
        result.Value.ImageLink.Should().Be(string.Empty);
        result.Value.ReadingDates.Should().BeEmpty();
        _bookRepositoryMock.Received(1).Add(Arg.Any<Book>());
        await _unitOfWorkMock.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_AddReadingDate_WhenRatingIsProvided()
    {
        // Arrange
        var commandWithRating = new CreateBookCommand("Serie", "Title", "978-3-16-148410-0", 1, "", 5);
        var normalizedIsbn = IsbnHelper.NormalizeIsbn(commandWithRating.ISBN);
        _bookRepositoryMock.GetByIsbnAsync(Arg.Is<string>(s => s == normalizedIsbn), Arg.Any<CancellationToken>()).Returns((Book?)null);

        // Act
        var result = await _handler.Handle(commandWithRating, default);

        // Assert
        result.IsSuccess.Should().BeTrue();
        Guard.Against.Null(result.Value);
        result.Value.Rating.Should().Be(5);
        result.Value.ReadingDates.Should().HaveCount(1);
    }

    [Fact]
    public async Task Handle_Should_NotAddReadingDate_WhenRatingIsZero()
    {
        // Arrange
        var commandWithoutRating = new CreateBookCommand("Serie", "Title", "978-3-16-148410-0", 1, "", 0);
        var normalizedIsbn = IsbnHelper.NormalizeIsbn(commandWithoutRating.ISBN);
        _bookRepositoryMock.GetByIsbnAsync(Arg.Is<string>(s => s == normalizedIsbn), Arg.Any<CancellationToken>()).Returns((Book?)null);

        // Act
        var result = await _handler.Handle(commandWithoutRating, default);

        // Assert
        result.IsSuccess.Should().BeTrue();
        Guard.Against.Null(result.Value);
        result.Value.Rating.Should().Be(0);
        result.Value.ReadingDates.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_Should_PassCorrectCancellationToken()
    {
        // Arrange
        var cancellationToken = new CancellationToken();
        var normalizedIsbn = IsbnHelper.NormalizeIsbn(s_validCommand.ISBN);
        _bookRepositoryMock.GetByIsbnAsync(Arg.Is<string>(s => s == normalizedIsbn), cancellationToken).Returns((Book?)null);

        // Act
        await _handler.Handle(s_validCommand, cancellationToken);

        // Assert
        await _bookRepositoryMock.Received(1).GetByIsbnAsync(normalizedIsbn, cancellationToken);
        await _unitOfWorkMock.Received(1).SaveChangesAsync(cancellationToken);
    }

    #region Metadata Fields Tests

    [Fact]
    public async Task Handle_Should_CreateBookWithAllMetadataFields()
    {
        // Arrange
        const string authors = "Brian K. Vaughan, Fiona Staples";
        const string publishers = "Image Comics";
        var publishDate = new DateOnly(2012, 10, 10);
        const int numberOfPages = 160;

        var commandWithMetadata = new CreateBookCommand(
            "Saga",
            "Volume 1",
            "978-1-60706-601-9",
            1,
            "https://example.com/saga.jpg",
            5,
            authors,
            publishers,
            publishDate,
            numberOfPages
        );

        var normalizedIsbn = IsbnHelper.NormalizeIsbn(commandWithMetadata.ISBN);
        _bookRepositoryMock.GetByIsbnAsync(Arg.Is<string>(s => s == normalizedIsbn), Arg.Any<CancellationToken>())
            .Returns((Book?)null);

        // Act
        var result = await _handler.Handle(commandWithMetadata, default);

        // Assert
        result.IsSuccess.Should().BeTrue();
        Guard.Against.Null(result.Value);
        result.Value.Authors.Should().Be(authors);
        result.Value.Publishers.Should().Be(publishers);
        result.Value.PublishDate.Should().Be(publishDate);
        result.Value.NumberOfPages.Should().Be(numberOfPages);
        _bookRepositoryMock.Received(1).Add(Arg.Any<Book>());
        await _unitOfWorkMock.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_CreateBookWithAuthorsAndPublishers()
    {
        // Arrange
        const string authors = "Alan Moore, Dave Gibbons";
        const string publishers = "DC Comics";

        var command = new CreateBookCommand(
            "Watchmen",
            "Watchmen",
            "978-1-4012-4525-2",
            Authors: authors,
            Publishers: publishers
        );

        var normalizedIsbn = IsbnHelper.NormalizeIsbn(command.ISBN);
        _bookRepositoryMock.GetByIsbnAsync(Arg.Is<string>(s => s == normalizedIsbn), Arg.Any<CancellationToken>())
            .Returns((Book?)null);

        // Act
        var result = await _handler.Handle(command, default);

        // Assert
        result.IsSuccess.Should().BeTrue();
        Guard.Against.Null(result.Value);
        result.Value.Authors.Should().Be(authors);
        result.Value.Publishers.Should().Be(publishers);
        result.Value.PublishDate.Should().BeNull();
        result.Value.NumberOfPages.Should().BeNull();
    }

    [Fact]
    public async Task Handle_Should_CreateBookWithPublishDate()
    {
        // Arrange
        var publishDate = new DateOnly(2004, 10, 1);

        var command = new CreateBookCommand(
            "The Walking Dead",
            "Days Gone Bye",
            "978-1-58240-619-0",
            PublishDate: publishDate
        );

        var normalizedIsbn = IsbnHelper.NormalizeIsbn(command.ISBN);
        _bookRepositoryMock.GetByIsbnAsync(Arg.Is<string>(s => s == normalizedIsbn), Arg.Any<CancellationToken>())
            .Returns((Book?)null);

        // Act
        var result = await _handler.Handle(command, default);

        // Assert
        result.IsSuccess.Should().BeTrue();
        Guard.Against.Null(result.Value);
        result.Value.PublishDate.Should().Be(publishDate);
        result.Value.Authors.Should().BeEmpty();
        result.Value.Publishers.Should().BeEmpty();
        result.Value.NumberOfPages.Should().BeNull();
    }

    [Fact]
    public async Task Handle_Should_CreateBookWithNumberOfPages()
    {
        // Arrange
        const int numberOfPages = 128;

        var command = new CreateBookCommand(
            "Y: The Last Man",
            "Unmanned",
            "978-1-4012-1951-2",
            NumberOfPages: numberOfPages
        );

        var normalizedIsbn = IsbnHelper.NormalizeIsbn(command.ISBN);
        _bookRepositoryMock.GetByIsbnAsync(Arg.Is<string>(s => s == normalizedIsbn), Arg.Any<CancellationToken>())
            .Returns((Book?)null);

        // Act
        var result = await _handler.Handle(command, default);

        // Assert
        result.IsSuccess.Should().BeTrue();
        Guard.Against.Null(result.Value);
        result.Value.NumberOfPages.Should().Be(numberOfPages);
        result.Value.Authors.Should().BeEmpty();
        result.Value.Publishers.Should().BeEmpty();
        result.Value.PublishDate.Should().BeNull();
    }

    [Fact]
    public async Task Handle_Should_CreateBookWithNullMetadata_WhenNotProvided()
    {
        // Arrange
        var command = new CreateBookCommand(
            "Fables",
            "Volume 1",
            "978-1-56389-942-3"
        );

        var normalizedIsbn = IsbnHelper.NormalizeIsbn(command.ISBN);
        _bookRepositoryMock.GetByIsbnAsync(Arg.Is<string>(s => s == normalizedIsbn), Arg.Any<CancellationToken>())
            .Returns((Book?)null);

        // Act
        var result = await _handler.Handle(command, default);

        // Assert
        result.IsSuccess.Should().BeTrue();
        Guard.Against.Null(result.Value);
        result.Value.Authors.Should().BeEmpty();
        result.Value.Publishers.Should().BeEmpty();
        result.Value.PublishDate.Should().BeNull();
        result.Value.NumberOfPages.Should().BeNull();
    }

    [Fact]
    public async Task Handle_Should_CreateBookWithEmptyAuthorsAndPublishers()
    {
        // Arrange
        var command = new CreateBookCommand(
            "Unknown Series",
            "Unknown Title",
            "978-3-16-148410-0",
            Authors: "",
            Publishers: ""
        );

        var normalizedIsbn = IsbnHelper.NormalizeIsbn(command.ISBN);
        _bookRepositoryMock.GetByIsbnAsync(Arg.Is<string>(s => s == normalizedIsbn), Arg.Any<CancellationToken>())
            .Returns((Book?)null);

        // Act
        var result = await _handler.Handle(command, default);

        // Assert
        result.IsSuccess.Should().BeTrue();
        Guard.Against.Null(result.Value);
        result.Value.Authors.Should().BeEmpty();
        result.Value.Publishers.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_Should_CreateBookWithMultipleAuthors()
    {
        // Arrange
        const string authors = "Stan Lee, Jack Kirby, Steve Ditko";
        var command = new CreateBookCommand(
            "Marvel",
            "The Avengers",
            "978-1-4012-4525-2",
            Authors: authors
        );

        var normalizedIsbn = IsbnHelper.NormalizeIsbn(command.ISBN);
        _bookRepositoryMock.GetByIsbnAsync(Arg.Is<string>(s => s == normalizedIsbn), Arg.Any<CancellationToken>())
            .Returns((Book?)null);

        // Act
        var result = await _handler.Handle(command, default);

        // Assert
        result.IsSuccess.Should().BeTrue();
        Guard.Against.Null(result.Value);
        result.Value.Authors.Should().Be(authors);
    }

    [Fact]
    public async Task Handle_Should_CreateBookWithMultiplePublishers()
    {
        // Arrange
        const string publishers = "Marvel Comics, DC Comics";
        var command = new CreateBookCommand(
            "Crossover",
            "JLA/Avengers",
            "978-1-4012-0331-3",
            Publishers: publishers
        );

        var normalizedIsbn = IsbnHelper.NormalizeIsbn(command.ISBN);
        _bookRepositoryMock.GetByIsbnAsync(Arg.Is<string>(s => s == normalizedIsbn), Arg.Any<CancellationToken>())
            .Returns((Book?)null);

        // Act
        var result = await _handler.Handle(command, default);

        // Assert
        result.IsSuccess.Should().BeTrue();
        Guard.Against.Null(result.Value);
        result.Value.Publishers.Should().Be(publishers);
    }

    [Fact]
    public async Task Handle_Should_CreateBookWithHistoricalPublishDate()
    {
        // Arrange
        var publishDate = new DateOnly(1962, 8, 1);
        var command = new CreateBookCommand(
            "Amazing Fantasy",
            "Spider-Man's First Appearance",
            "978-1-60706-601-9",
            15,
            "",
            0,
            "Stan Lee, Steve Ditko",
            "Marvel Comics",
            publishDate,
            11
        );

        var normalizedIsbn = IsbnHelper.NormalizeIsbn(command.ISBN);
        _bookRepositoryMock.GetByIsbnAsync(Arg.Is<string>(s => s == normalizedIsbn), Arg.Any<CancellationToken>())
            .Returns((Book?)null);

        // Act
        var result = await _handler.Handle(command, default);

        // Assert
        result.IsSuccess.Should().BeTrue();
        Guard.Against.Null(result.Value);
        result.Value.PublishDate.Should().Be(publishDate);
        result.Value.NumberOfPages.Should().Be(11);
    }

    [Fact]
    public async Task Handle_Should_CreateBookWithLargeNumberOfPages()
    {
        // Arrange
        const int numberOfPages = 416;
        var command = new CreateBookCommand(
            "Watchmen",
            "Watchmen",
            "978-0-930289-23-2",
            1,
            "",
            0,
            "Alan Moore",
            "DC Comics",
            new DateOnly(1987, 9, 1),
            numberOfPages
        );

        var normalizedIsbn = IsbnHelper.NormalizeIsbn(command.ISBN);
        _bookRepositoryMock.GetByIsbnAsync(Arg.Is<string>(s => s == normalizedIsbn), Arg.Any<CancellationToken>())
            .Returns((Book?)null);

        // Act
        var result = await _handler.Handle(command, default);

        // Assert
        result.IsSuccess.Should().BeTrue();
        Guard.Against.Null(result.Value);
        result.Value.NumberOfPages.Should().Be(numberOfPages);
    }

    [Fact]
    public async Task Handle_Should_PassAllMetadataToBookCreate()
    {
        // Arrange
        const string serie = "Saga";
        const string title = "Volume 1";
        const string isbn = "978-1-60706-601-9";
        const int volumeNumber = 1;
        const string imageLink = "https://example.com/saga.jpg";
        const int rating = 5;
        const string authors = "Brian K. Vaughan, Fiona Staples";
        const string publishers = "Image Comics";
        var publishDate = new DateOnly(2012, 10, 10);
        const int numberOfPages = 160;

        var command = new CreateBookCommand(
            serie, title, isbn, volumeNumber, imageLink, rating,
            authors, publishers, publishDate, numberOfPages
        );

        var normalizedIsbn = IsbnHelper.NormalizeIsbn(command.ISBN);
        _bookRepositoryMock.GetByIsbnAsync(Arg.Is<string>(s => s == normalizedIsbn), Arg.Any<CancellationToken>())
            .Returns((Book?)null);

        // Act
        var result = await _handler.Handle(command, default);

        // Assert
        result.IsSuccess.Should().BeTrue();
        Guard.Against.Null(result.Value);
        result.Value.Serie.Should().Be(serie);
        result.Value.Title.Should().Be(title);
        result.Value.ISBN.Should().Be(normalizedIsbn);
        result.Value.VolumeNumber.Should().Be(volumeNumber);
        result.Value.ImageLink.Should().Be(imageLink);
        result.Value.Rating.Should().Be(rating);
        result.Value.Authors.Should().Be(authors);
        result.Value.Publishers.Should().Be(publishers);
        result.Value.PublishDate.Should().Be(publishDate);
        result.Value.NumberOfPages.Should().Be(numberOfPages);
        result.Value.ReadingDates.Should().HaveCount(1); // Rating > 0, so reading date is added
    }

    #endregion
}

