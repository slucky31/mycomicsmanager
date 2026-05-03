using Application.Books.GetById;
using Application.Interfaces;
using Ardalis.GuardClauses;
using Domain.Books;
using Domain.Libraries;
using NSubstitute;
using NSubstitute.ReturnsExtensions;

namespace Application.UnitTests.Books;

public sealed class GetBookQueryHandlerTests
{
    private static readonly Guid s_userId = Guid.CreateVersion7();

    private readonly IBookRepository _bookRepository;
    private readonly IRepository<Library, Guid> _libraryRepositoryMock;
    private readonly GetBookQueryHandler _handler;

    public GetBookQueryHandlerTests()
    {
        _bookRepository = Substitute.For<IBookRepository>();
        _libraryRepositoryMock = Substitute.For<IRepository<Library, Guid>>();
        _handler = new GetBookQueryHandler(_bookRepository, _libraryRepositoryMock);
    }

    private static Library CreateLibrary(Guid userId)
        => Library.Create("Test", "#FF0000", "book", LibraryBookType.Physical, userId).Value!;

    [Fact]
    public async Task Handle_ShouldReturnBadRequest_WhenRequestIsNull()
    {
        // Arrange
        GetBookByIdQuery? query = null;

        // Act
        var result = await _handler.Handle(query!, TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(BooksError.BadRequest);
        await _bookRepository.DidNotReceive().GetByIdAsync(Arg.Any<Guid>());
    }

    [Fact]
    public async Task Handle_ShouldReturnBadRequest_WhenIdIsEmpty()
    {
        // Arrange
        var query = new GetBookByIdQuery(Guid.Empty, s_userId);

        // Act
        var result = await _handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(BooksError.BadRequest);
        await _bookRepository.DidNotReceive().GetByIdAsync(Arg.Any<Guid>());
    }

    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenBookDoesNotExist()
    {
        // Arrange
        var bookId = Guid.CreateVersion7();
        var query = new GetBookByIdQuery(bookId, s_userId);
        _bookRepository.GetByIdAsync(bookId).ReturnsNull();

        // Act
        var result = await _handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(BooksError.NotFound);
        await _bookRepository.Received(1).GetByIdAsync(bookId);
    }

    [Fact]
    public async Task Handle_ShouldReturnBook_WhenBookExists()
    {
        // Arrange
        var bookId = Guid.CreateVersion7();
        var expectedBook = PhysicalBook.Create("Test Serie", "Test Title", "978-3-16-148410-0", 1, "https://example.com/image.jpg", libraryId: Guid.CreateVersion7()).Value!;
        var query = new GetBookByIdQuery(bookId, s_userId);
        var library = CreateLibrary(s_userId);
        _bookRepository.GetByIdAsync(bookId).Returns(expectedBook);
        _libraryRepositoryMock.GetByIdAsync(expectedBook.LibraryId).Returns(library);

        // Act
        var result = await _handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        Guard.Against.Null(result.Value);
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeSameAs(expectedBook);
        result.Value.Serie.Should().Be("Test Serie");
        result.Value.Title.Should().Be("Test Title");
        result.Value.ISBN.Should().Be("978-3-16-148410-0");
        result.Value.VolumeNumber.Should().Be(1);
        result.Value.ImageLink.Should().Be("https://example.com/image.jpg");
        await _bookRepository.Received(1).GetByIdAsync(bookId);
    }

    [Fact]
    public async Task Handle_ShouldCallGetByIdAsyncWithCorrectId_WhenHandlingQuery()
    {
        // Arrange
        var specificId = Guid.CreateVersion7();
        var book = PhysicalBook.Create("Serie", "Title", "978-3-16-148410-0", libraryId: Guid.CreateVersion7()).Value!;
        var query = new GetBookByIdQuery(specificId, s_userId);
        var library = CreateLibrary(s_userId);
        _bookRepository.GetByIdAsync(specificId).Returns(book);
        _libraryRepositoryMock.GetByIdAsync(book.LibraryId).Returns(library);

        // Act
        await _handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        await _bookRepository.Received(1).GetByIdAsync(specificId);
        await _bookRepository.DidNotReceive().GetByIdAsync(Arg.Is<Guid>(g => g != specificId));
    }

    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenBookBelongsToOtherUser()
    {
        // Arrange
        var bookId = Guid.CreateVersion7();
        var ownerId = Guid.CreateVersion7();
        var requestingUserId = Guid.CreateVersion7();
        var libraryId = Guid.CreateVersion7();
        var book = PhysicalBook.Create("Serie", "Title", "978-3-16-148410-0", libraryId: libraryId).Value!;
        var library = CreateLibrary(ownerId);
        var query = new GetBookByIdQuery(bookId, UserId: requestingUserId);

        _bookRepository.GetByIdAsync(bookId).Returns(book);
        _libraryRepositoryMock.GetByIdAsync(libraryId).Returns(library);

        // Act
        var result = await _handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(BooksError.NotFound);
    }

    [Fact]
    public async Task Handle_ShouldReturnBook_WhenOwnershipVerified()
    {
        // Arrange
        var userId = Guid.CreateVersion7();
        var bookId = Guid.CreateVersion7();
        var libraryId = Guid.CreateVersion7();
        var book = PhysicalBook.Create("Serie", "Title", "978-3-16-148410-0", libraryId: libraryId).Value!;
        var library = CreateLibrary(userId);
        var query = new GetBookByIdQuery(bookId, UserId: userId);

        _bookRepository.GetByIdAsync(bookId).Returns(book);
        _libraryRepositoryMock.GetByIdAsync(libraryId).Returns(library);

        // Act
        var result = await _handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeSameAs(book);
    }
}
