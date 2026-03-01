using Application.Books.DeleteReadingDate;
using Application.Interfaces;
using Domain.Books;
using Domain.Libraries;
using NSubstitute;

namespace Application.UnitTests.Books;

public class DeleteReadingDateCommandHandlerTests
{
    private static readonly Guid s_userId = Guid.CreateVersion7();
    private static readonly Guid s_libraryId = Guid.CreateVersion7();

    private readonly DeleteReadingDateCommandHandler _handler;
    private readonly IBookRepository _bookRepositoryMock;
    private readonly IUnitOfWork _unitOfWorkMock;
    private readonly IRepository<Library, Guid> _libraryRepositoryMock;

    public DeleteReadingDateCommandHandlerTests()
    {
        _bookRepositoryMock = Substitute.For<IBookRepository>();
        _unitOfWorkMock = Substitute.For<IUnitOfWork>();
        _libraryRepositoryMock = Substitute.For<IRepository<Library, Guid>>();
        _handler = new DeleteReadingDateCommandHandler(_bookRepositoryMock, _unitOfWorkMock, _libraryRepositoryMock);
    }

    private static PhysicalBook CreateBook()
        => PhysicalBook.Create("Serie", "Title", "978-3-16-148410-0", libraryId: s_libraryId).Value!;

    private static Library CreateLibrary(Guid userId)
        => Library.Create("Test", "#FF0000", "book", LibraryBookType.Physical, userId).Value!;

    [Fact]
    public async Task Handle_Should_ReturnNotFound_WhenBookDoesNotExist()
    {
        // Arrange
        var command = new DeleteReadingDateCommand(Guid.NewGuid(), Guid.NewGuid(), s_userId);
        _bookRepositoryMock.GetByIdAsync(command.BookId).Returns((Book?)null);

        // Act
        var result = await _handler.Handle(command, default);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(BooksError.NotFound);
        _bookRepositoryMock.DidNotReceive().Update(Arg.Any<Book>());
        await _unitOfWorkMock.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_ReturnSuccess_AndRemoveReadingDate_WhenBookExists()
    {
        // Arrange
        var book = CreateBook();
        book.AddReadingDate(DateTime.UtcNow, 4);
        var readingDateId = book.ReadingDates[0].Id;
        var command = new DeleteReadingDateCommand(book.Id, readingDateId, s_userId);
        _bookRepositoryMock.GetByIdAsync(command.BookId).Returns(book);
        _libraryRepositoryMock.GetByIdAsync(s_libraryId).Returns(CreateLibrary(s_userId));

        // Act
        var result = await _handler.Handle(command, default);

        // Assert
        result.IsSuccess.Should().BeTrue();
        book.ReadingDates.Should().BeEmpty();
        _bookRepositoryMock.Received(1).Update(book);
        await _unitOfWorkMock.Received(1).SaveChangesAsync(CancellationToken.None);
    }

    [Fact]
    public async Task Handle_Should_ReturnSuccess_WhenReadingDateDoesNotExist()
    {
        // Arrange
        var book = CreateBook();
        book.AddReadingDate(DateTime.UtcNow, 4);
        var nonExistentReadingDateId = Guid.NewGuid();
        var command = new DeleteReadingDateCommand(book.Id, nonExistentReadingDateId, s_userId);
        _bookRepositoryMock.GetByIdAsync(command.BookId).Returns(book);
        _libraryRepositoryMock.GetByIdAsync(s_libraryId).Returns(CreateLibrary(s_userId));

        // Act
        var result = await _handler.Handle(command, default);

        // Assert
        result.IsSuccess.Should().BeTrue();
        book.ReadingDates.Should().HaveCount(1);
    }

    [Fact]
    public async Task Handle_Should_PassCancellationToken()
    {
        // Arrange
        var book = CreateBook();
        book.AddReadingDate(DateTime.UtcNow, 3);
        var readingDateId = book.ReadingDates[0].Id;
        var command = new DeleteReadingDateCommand(book.Id, readingDateId, s_userId);
        var cancellationToken = new CancellationToken();
        _bookRepositoryMock.GetByIdAsync(command.BookId).Returns(book);
        _libraryRepositoryMock.GetByIdAsync(s_libraryId).Returns(CreateLibrary(s_userId));

        // Act
        await _handler.Handle(command, cancellationToken);

        // Assert
        await _unitOfWorkMock.Received(1).SaveChangesAsync(cancellationToken);
    }

    [Fact]
    public async Task Handle_Should_ReturnNotFound_WhenBookBelongsToOtherUser()
    {
        // Arrange
        var requestingUserId = Guid.CreateVersion7();
        var book = CreateBook();
        book.AddReadingDate(DateTime.UtcNow, 4);
        var readingDateId = book.ReadingDates[0].Id;
        var library = CreateLibrary(s_userId); // different owner
        var command = new DeleteReadingDateCommand(book.Id, readingDateId, UserId: requestingUserId);
        _bookRepositoryMock.GetByIdAsync(book.Id).Returns(book);
        _libraryRepositoryMock.GetByIdAsync(s_libraryId).Returns(library);

        // Act
        var result = await _handler.Handle(command, default);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(BooksError.NotFound);
        _bookRepositoryMock.DidNotReceive().Update(Arg.Any<Book>());
    }

    [Fact]
    public async Task Handle_Should_ReturnSuccess_WhenOwnershipVerified()
    {
        // Arrange
        var book = CreateBook();
        book.AddReadingDate(DateTime.UtcNow, 4);
        var readingDateId = book.ReadingDates[0].Id;
        var library = CreateLibrary(s_userId);
        var command = new DeleteReadingDateCommand(book.Id, readingDateId, UserId: s_userId);
        _bookRepositoryMock.GetByIdAsync(book.Id).Returns(book);
        _libraryRepositoryMock.GetByIdAsync(s_libraryId).Returns(library);

        // Act
        var result = await _handler.Handle(command, default);

        // Assert
        result.IsSuccess.Should().BeTrue();
        book.ReadingDates.Should().BeEmpty();
    }
}
