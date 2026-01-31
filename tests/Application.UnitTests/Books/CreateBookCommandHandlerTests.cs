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
        _bookRepositoryMock.Received(1).Add(Arg.Any<Book>());
        await _unitOfWorkMock.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
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
}
