using System.Reflection;
using Application.Books.Update;
using Application.Interfaces;
using Ardalis.GuardClauses;
using Domain.Books;
using NSubstitute;

namespace Application.UnitTests.Books;

public class UpdateBookCommandHandlerTests
{
    private static readonly Guid s_bookId = Guid.CreateVersion7();
    private static readonly UpdateBookCommand s_validCommand = new(
        s_bookId,
        "Updated Serie",
        "Updated Title",
        "978-0-306-40615-7",
        2,
        "https://example.com/updated.jpg",
        5
    );

    private readonly UpdateBookCommandHandler _handler;
    private readonly IBookRepository _bookRepositoryMock;
    private readonly IUnitOfWork _unitOfWorkMock;

    public UpdateBookCommandHandlerTests()
    {
        _bookRepositoryMock = Substitute.For<IBookRepository>();
        _unitOfWorkMock = Substitute.For<IUnitOfWork>();

        _handler = new UpdateBookCommandHandler(_bookRepositoryMock, _unitOfWorkMock);
    }

    private static Book CreateBookWithId(Guid id, string serie, string title, string isbn, int volumeNumber = 1, string imageLink = "")
    {
        var book = Book.Create(serie, title, isbn, volumeNumber, imageLink);

        // Use reflection to set the Id property
        // TODO :  Consider adding a test-specific factory method or constructor in the test project (e.g., via internal visibility or a test helper) to create Book instances with specific IDs for testing purposes.
        var idProperty = typeof(Book).GetProperty("Id", BindingFlags.Public | BindingFlags.Instance);
        idProperty?.SetValue(book, id);

        return book;
    }

    [Fact]
    public async Task Handle_Should_ReturnSuccess_WhenValidCommandProvided()
    {
        // Arrange
        var existingBook = CreateBookWithId(s_bookId, "Original Serie", "Original Title", "978-3-16-148410-0");
        _bookRepositoryMock.GetByIdAsync(s_validCommand.Id).Returns(existingBook);
        _bookRepositoryMock.ListAsync().Returns(new List<Book> { existingBook });

        // Act
        var result = await _handler.Handle(s_validCommand, default);

        // Assert
        Guard.Against.Null(result.Value);
        result.IsSuccess.Should().BeTrue();
        result.Value.Serie.Should().Be(s_validCommand.Serie);
        result.Value.Title.Should().Be(s_validCommand.Title);
        result.Value.ISBN.Should().Be("9780306406157");
        result.Value.VolumeNumber.Should().Be(s_validCommand.VolumeNumber);
        result.Value.ImageLink.Should().Be(s_validCommand.ImageLink);
        _bookRepositoryMock.Received(1).Update(Arg.Any<Book>());
        await _unitOfWorkMock.Received(1).SaveChangesAsync(CancellationToken.None);
    }

    [Fact]
    public async Task Handle_Should_ExecuteSaveChangesAsyncOnce()
    {
        // Arrange
        var existingBook = CreateBookWithId(s_bookId, "Original Serie", "Original Title", "978-3-16-148410-0");
        _bookRepositoryMock.GetByIdAsync(s_validCommand.Id).Returns(existingBook);
        _bookRepositoryMock.ListAsync().Returns(new List<Book> { existingBook });

        // Act
        await _handler.Handle(s_validCommand, default);

        // Assert
        await _unitOfWorkMock.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldReturnBadRequest_WhenTitleIsEmpty()
    {
        // Arrange
        var emptyTitleCommand = new UpdateBookCommand(s_bookId, "Serie", string.Empty, "978-3-16-148410-0", 1, "", 0);

        // Act
        var result = await _handler.Handle(emptyTitleCommand, default);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(BooksError.BadRequest);
        _bookRepositoryMock.Received(0).Update(Arg.Any<Book>());
        await _unitOfWorkMock.Received(0).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldReturnBadRequest_WhenTitleIsNull()
    {
        // Arrange
        var nullTitleCommand = new UpdateBookCommand(s_bookId, "Serie", null!, "978-3-16-148410-0", 1, "", 0);

        // Act
        var result = await _handler.Handle(nullTitleCommand, default);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(BooksError.BadRequest);
        _bookRepositoryMock.Received(0).Update(Arg.Any<Book>());
        await _unitOfWorkMock.Received(0).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldReturnBadRequest_WhenISBNIsEmpty()
    {
        // Arrange
        var emptyIsbnCommand = new UpdateBookCommand(s_bookId, "Serie", "Title", string.Empty, 1, "", 0);

        // Act
        var result = await _handler.Handle(emptyIsbnCommand, default);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(BooksError.BadRequest);
        _bookRepositoryMock.Received(0).Update(Arg.Any<Book>());
        await _unitOfWorkMock.Received(0).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldReturnBadRequest_WhenISBNIsNull()
    {
        // Arrange
        var nullIsbnCommand = new UpdateBookCommand(s_bookId, "Serie", "Title", null!, 1, "", 0);

        // Act
        var result = await _handler.Handle(nullIsbnCommand, default);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(BooksError.BadRequest);
        _bookRepositoryMock.Received(0).Update(Arg.Any<Book>());
        await _unitOfWorkMock.Received(0).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldReturnInvalidISBN_WhenISBNFormatIsInvalid()
    {
        // Arrange
        var invalidIsbnCommand = new UpdateBookCommand(s_bookId, "Serie", "Title", "invalid-isbn", 1, "", 0);

        // Act
        var result = await _handler.Handle(invalidIsbnCommand, default);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(BooksError.InvalidISBN);
        _bookRepositoryMock.Received(0).Update(Arg.Any<Book>());
        await _unitOfWorkMock.Received(0).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldReturnInvalidISBN_WhenISBNHasInvalidLength()
    {
        // Arrange
        var invalidLengthCommand = new UpdateBookCommand(s_bookId, "Serie", "Title", "12345", 1, "", 0);

        // Act
        var result = await _handler.Handle(invalidLengthCommand, default);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(BooksError.InvalidISBN);
        _bookRepositoryMock.Received(0).Update(Arg.Any<Book>());
        await _unitOfWorkMock.Received(0).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenBookDoesNotExist()
    {
        // Arrange
        _bookRepositoryMock.GetByIdAsync(s_validCommand.Id).Returns((Book?)null);

        // Act
        var result = await _handler.Handle(s_validCommand, default);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(BooksError.NotFound);
        _bookRepositoryMock.Received(0).Update(Arg.Any<Book>());
        await _unitOfWorkMock.Received(0).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldReturnDuplicate_WhenAnotherBookWithSameISBNExists()
    {
        // Arrange
        var existingBook = CreateBookWithId(s_bookId, "Original Serie", "Original Title", "978-3-16-148410-0");
        var anotherBookId = Guid.CreateVersion7();
        var anotherBook = CreateBookWithId(anotherBookId, "Another Serie", "Another Title", "978-0-306-40615-7");

        _bookRepositoryMock.GetByIdAsync(s_validCommand.Id).Returns(existingBook);
        _bookRepositoryMock.GetByIsbnAsync("9780306406157", Arg.Any<CancellationToken>()).Returns(anotherBook);

        // Act
        var result = await _handler.Handle(s_validCommand, default);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(BooksError.Duplicate);
        _bookRepositoryMock.Received(0).Update(Arg.Any<Book>());
        await _unitOfWorkMock.Received(0).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_AllowUpdatingToSameISBN()
    {
        // Arrange - Updating book to keep its own ISBN
        var existingBook = CreateBookWithId(s_bookId, "Original Serie", "Original Title", "978-3-16-148410-0");

        var commandWithSameISBN = new UpdateBookCommand(
            s_bookId,
            "Updated Serie",
            "Updated Title",
            existingBook.ISBN, // Same ISBN as existing book
            2,
            "https://example.com/updated.jpg",
            3
        );

        _bookRepositoryMock.GetByIdAsync(commandWithSameISBN.Id).Returns(existingBook);
        _bookRepositoryMock.GetByIsbnAsync("9783161484100", Arg.Any<CancellationToken>()).Returns(existingBook);

        // Act
        var result = await _handler.Handle(commandWithSameISBN, default);

        // Assert
        Guard.Against.Null(result.Value);
        result.IsSuccess.Should().BeTrue();
        _bookRepositoryMock.Received(1).Update(Arg.Any<Book>());
        await _unitOfWorkMock.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_UpdateAllBookProperties()
    {
        // Arrange
        var existingBook = CreateBookWithId(s_bookId, "Original Serie", "Original Title", "978-3-16-148410-0");
        _bookRepositoryMock.GetByIdAsync(s_validCommand.Id).Returns(existingBook);
        _bookRepositoryMock.GetByIsbnAsync("9780306406157", Arg.Any<CancellationToken>()).Returns((Book?)null);

        // Act
        var result = await _handler.Handle(s_validCommand, default);

        // Assert
        Guard.Against.Null(result.Value);
        result.IsSuccess.Should().BeTrue();
        result.Value.Serie.Should().Be("Updated Serie");
        result.Value.Title.Should().Be("Updated Title");
        result.Value.ISBN.Should().Be("9780306406157");
        result.Value.VolumeNumber.Should().Be(2);
        result.Value.ImageLink.Should().Be("https://example.com/updated.jpg");
    }

    [Fact]
    public async Task Handle_Should_CallUpdateWithCorrectBook()
    {
        // Arrange
        var existingBook = CreateBookWithId(s_bookId, "Original Serie", "Original Title", "978-3-16-148410-0");
        _bookRepositoryMock.GetByIdAsync(s_validCommand.Id).Returns(existingBook);
        _bookRepositoryMock.GetByIsbnAsync("9780306406157", Arg.Any<CancellationToken>()).Returns((Book?)null);

        // Act
        await _handler.Handle(s_validCommand, default);

        // Assert
        _bookRepositoryMock.Received(1).Update(existingBook);
    }

    [Fact]
    public async Task Handle_Should_PassCorrectCancellationToken()
    {
        // Arrange
        var existingBook = CreateBookWithId(s_bookId, "Original Serie", "Original Title", "978-3-16-148410-0");
        var cancellationToken = new CancellationToken();
        _bookRepositoryMock.GetByIdAsync(s_validCommand.Id).Returns(existingBook);
        _bookRepositoryMock.GetByIsbnAsync("9780306406157", Arg.Any<CancellationToken>()).Returns((Book?)null);

        // Act
        await _handler.Handle(s_validCommand, cancellationToken);

        // Assert
        await _bookRepositoryMock.Received(1).GetByIdAsync(s_validCommand.Id);
        await _unitOfWorkMock.Received(1).SaveChangesAsync(cancellationToken);
    }

    [Fact]
    public async Task Handle_Should_ReturnSuccess_WithISBN10Format()
    {
        // Arrange
        var existingBook = CreateBookWithId(s_bookId, "Original Serie", "Original Title", "978-3-16-148410-0");
        var isbn10Command = new UpdateBookCommand(s_bookId, "Serie", "Title", "0-306-40615-2", 1, "", 0);
        _bookRepositoryMock.GetByIdAsync(isbn10Command.Id).Returns(existingBook);
        _bookRepositoryMock.GetByIsbnAsync("0306406152", Arg.Any<CancellationToken>()).Returns((Book?)null);

        // Act
        var result = await _handler.Handle(isbn10Command, default);

        // Assert
        result.IsSuccess.Should().BeTrue();
        Guard.Against.Null(result.Value);
        result.Value.ISBN.Should().Be("0306406152");
        _bookRepositoryMock.Received(1).Update(Arg.Any<Book>());
        await _unitOfWorkMock.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_ReturnSuccess_WithISBN13Format()
    {
        // Arrange
        var existingBook = CreateBookWithId(s_bookId, "Original Serie", "Original Title", "978-3-16-148410-0");
        var isbn13Command = new UpdateBookCommand(s_bookId, "Serie", "Title", "978-0-306-40615-7", 1, "", 0);
        _bookRepositoryMock.GetByIdAsync(isbn13Command.Id).Returns(existingBook);
        _bookRepositoryMock.GetByIsbnAsync("9780306406157", Arg.Any<CancellationToken>()).Returns((Book?)null);

        // Act
        var result = await _handler.Handle(isbn13Command, default);

        // Assert
        result.IsSuccess.Should().BeTrue();
        Guard.Against.Null(result.Value);
        result.Value.ISBN.Should().Be("9780306406157");
        _bookRepositoryMock.Received(1).Update(Arg.Any<Book>());
        await _unitOfWorkMock.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_UpdateVolumeNumber()
    {
        // Arrange
        var existingBook = CreateBookWithId(s_bookId, "Original Serie", "Original Title", "978-3-16-148410-0");
        var updateVolumeCommand = new UpdateBookCommand(s_bookId, "Serie", "Title", "978-3-16-148410-0", 5, "", 0);
        _bookRepositoryMock.GetByIdAsync(updateVolumeCommand.Id).Returns(existingBook);
        _bookRepositoryMock.GetByIsbnAsync("9783161484100", Arg.Any<CancellationToken>()).Returns(existingBook);

        // Act
        var result = await _handler.Handle(updateVolumeCommand, default);

        // Assert
        Guard.Against.Null(result.Value);
        result.IsSuccess.Should().BeTrue();
        result.Value.VolumeNumber.Should().Be(5);
    }

    [Fact]
    public async Task Handle_Should_UpdateImageLink()
    {
        // Arrange
        var existingBook = CreateBookWithId(s_bookId, "Original Serie", "Original Title", "978-3-16-148410-0");
        var updateImageCommand = new UpdateBookCommand(s_bookId, "Serie", "Title", "978-3-16-148410-0", 1, "https://new-image.com/cover.jpg", 0);
        _bookRepositoryMock.GetByIdAsync(updateImageCommand.Id).Returns(existingBook);
        _bookRepositoryMock.GetByIsbnAsync("9783161484100", Arg.Any<CancellationToken>()).Returns(existingBook);

        // Act
        var result = await _handler.Handle(updateImageCommand, default);

        // Assert
        Guard.Against.Null(result.Value);
        result.IsSuccess.Should().BeTrue();
        result.Value.ImageLink.Should().Be("https://new-image.com/cover.jpg");
    }

    [Fact]
    public async Task Handle_Should_AllowEmptyImageLink()
    {
        // Arrange
        var existingBook = CreateBookWithId(s_bookId, "Original Serie", "Original Title", "978-3-16-148410-0");
        var emptyImageCommand = new UpdateBookCommand(s_bookId, "Serie", "Title", "978-3-16-148410-0", 1, "", 0);
        _bookRepositoryMock.GetByIdAsync(emptyImageCommand.Id).Returns(existingBook);
        _bookRepositoryMock.GetByIsbnAsync("9783161484100", Arg.Any<CancellationToken>()).Returns(existingBook);

        // Act
        var result = await _handler.Handle(emptyImageCommand, default);

        // Assert
        Guard.Against.Null(result.Value);
        result.IsSuccess.Should().BeTrue();
        result.Value.ImageLink.Should().Be(string.Empty);
    }

    [Fact]
    public async Task Handle_Should_NotCallUpdate_WhenBookNotFound()
    {
        // Arrange
        _bookRepositoryMock.GetByIdAsync(s_validCommand.Id).Returns((Book?)null);

        // Act
        await _handler.Handle(s_validCommand, default);

        // Assert
        _bookRepositoryMock.DidNotReceive().Update(Arg.Any<Book>());
    }

    [Fact]
    public async Task Handle_Should_NotCallSaveChanges_WhenValidationFails()
    {
        // Arrange
        var invalidCommand = new UpdateBookCommand(s_bookId, "Serie", "", "978-3-16-148410-0", 1, "", 0);

        // Act
        await _handler.Handle(invalidCommand, default);

        // Assert
        await _unitOfWorkMock.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_CheckDuplicateISBNAcrossMultipleBooks()
    {
        // Arrange
        var bookId1 = Guid.CreateVersion7();
        var bookId2 = Guid.CreateVersion7();
        var bookId3 = Guid.CreateVersion7();

        var book1 = CreateBookWithId(bookId1, "Serie 1", "Title 1", "978-3-16-148410-0");
        var book2 = CreateBookWithId(bookId2, "Serie 2", "Title 2", "978-0-306-40615-7");
        var book3 = CreateBookWithId(bookId3, "Serie 3", "Title 3", "978-0-451-52493-5");

        _bookRepositoryMock.GetByIdAsync(bookId1).Returns(book1);
        _bookRepositoryMock.ListAsync().Returns(new List<Book> { book1, book2, book3 });

        // Mock GetByIsbnAsync to return book2 when queried with its ISBN
        _bookRepositoryMock.GetByIsbnAsync("9780306406157", Arg.Any<CancellationToken>()).Returns(book2);

        // Command tries to update book1 to have the same ISBN as book2
        var duplicateCommand = new UpdateBookCommand(bookId1, "Serie", "Title", book2.ISBN, 1, "", 0);

        // Act
        var result = await _handler.Handle(duplicateCommand, default);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(BooksError.Duplicate);
    }

}
