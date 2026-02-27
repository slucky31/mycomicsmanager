using Application.Books.AddReadingDate;
using Application.Interfaces;
using Domain.Books;
using Domain.Libraries;
using NSubstitute;

namespace Application.UnitTests.Books;

public class AddReadingDateCommandHandlerTests
{
    private static readonly Guid s_userId = Guid.CreateVersion7();
    private static readonly Guid s_libraryId = Guid.CreateVersion7();

    private readonly AddReadingDateCommandHandler _handler;
    private readonly IBookRepository _bookRepositoryMock;
    private readonly IUnitOfWork _unitOfWorkMock;
    private readonly IRepository<Library, Guid> _libraryRepositoryMock;

    public AddReadingDateCommandHandlerTests()
    {
        _bookRepositoryMock = Substitute.For<IBookRepository>();
        _unitOfWorkMock = Substitute.For<IUnitOfWork>();
        _libraryRepositoryMock = Substitute.For<IRepository<Library, Guid>>();
        _handler = new AddReadingDateCommandHandler(_bookRepositoryMock, _unitOfWorkMock, _libraryRepositoryMock);
    }

    private static Book CreateBook()
        => PhysicalBook.Create("Serie", "Title", "978-3-16-148410-0", libraryId: s_libraryId).Value!;

    private static Library CreateLibrary(Guid userId)
        => Library.Create("Test", "#FF0000", "book", LibraryBookType.Physical, userId).Value!;

    [Fact]
    public async Task Handle_Should_ReturnNotFound_WhenBookDoesNotExist()
    {
        // Arrange
        var command = new AddReadingDateCommand(Guid.NewGuid(), 4);
        _bookRepositoryMock.GetByIdAsync(command.BookId).Returns((Book?)null);

        // Act
        var result = await _handler.Handle(command, default);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(BooksError.NotFound);
        _bookRepositoryMock.DidNotReceive().AddReadingDate(Arg.Any<ReadingDate>());
        _bookRepositoryMock.DidNotReceive().Update(Arg.Any<Book>());
        await _unitOfWorkMock.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_ReturnSuccess_WhenBookExists()
    {
        // Arrange
        var book = CreateBook();
        var command = new AddReadingDateCommand(book.Id, 5);
        _bookRepositoryMock.GetByIdAsync(command.BookId).Returns(book);

        // Act
        var result = await _handler.Handle(command, default);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Rating.Should().Be(5);
        book.ReadingDates.Should().HaveCount(1);
        _bookRepositoryMock.Received(1).AddReadingDate(Arg.Any<ReadingDate>());
        _bookRepositoryMock.Received(1).Update(book);
        await _unitOfWorkMock.Received(1).SaveChangesAsync(CancellationToken.None);
    }

    [Fact]
    public async Task Handle_Should_AddReadingDateWithCorrectRating()
    {
        // Arrange
        var book = CreateBook();
        const int rating = 3;
        var command = new AddReadingDateCommand(book.Id, rating);
        _bookRepositoryMock.GetByIdAsync(command.BookId).Returns(book);

        // Act
        var result = await _handler.Handle(command, default);

        // Assert
        result.IsSuccess.Should().BeTrue();
        book.ReadingDates.Should().HaveCount(1);
        book.ReadingDates[0].Rating.Should().Be(rating);
        book.ReadingDates[0].BookId.Should().Be(book.Id);
    }

    [Fact]
    public async Task Handle_Should_PassCancellationToken()
    {
        // Arrange
        var book = CreateBook();
        var command = new AddReadingDateCommand(book.Id, 4);
        var cancellationToken = new CancellationToken();
        _bookRepositoryMock.GetByIdAsync(command.BookId).Returns(book);

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
        var library = CreateLibrary(s_userId); // different owner
        var command = new AddReadingDateCommand(book.Id, 4, UserId: requestingUserId);
        _bookRepositoryMock.GetByIdAsync(book.Id).Returns(book);
        _libraryRepositoryMock.GetByIdAsync(s_libraryId).Returns(library);

        // Act
        var result = await _handler.Handle(command, default);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(BooksError.NotFound);
        _bookRepositoryMock.DidNotReceive().AddReadingDate(Arg.Any<ReadingDate>());
    }

    [Fact]
    public async Task Handle_Should_ReturnSuccess_WhenOwnershipVerified()
    {
        // Arrange
        var book = CreateBook();
        var library = CreateLibrary(s_userId);
        var command = new AddReadingDateCommand(book.Id, 4, UserId: s_userId);
        _bookRepositoryMock.GetByIdAsync(book.Id).Returns(book);
        _libraryRepositoryMock.GetByIdAsync(s_libraryId).Returns(library);

        // Act
        var result = await _handler.Handle(command, default);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Rating.Should().Be(4);
    }
}
