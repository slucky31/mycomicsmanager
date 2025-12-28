using Application.Books.GetById;
using Application.Interfaces;
using Ardalis.GuardClauses;
using Domain.Books;
using NSubstitute;
using NSubstitute.ReturnsExtensions;

namespace Application.UnitTests.Books;

public class GetBookQueryHandlerTests
{
    private static readonly Guid s_bookId = Guid.CreateVersion7();
    private static readonly Book s_existingBook = Book.Create("Test Serie", "Test Title", "978-3-16-148410-0", 1, "https://example.com/image.jpg");

    private readonly GetBookQueryHandler _handler;
    private readonly IBookRepository _bookRepositoryMock;

    public GetBookQueryHandlerTests()
    {
        _bookRepositoryMock = Substitute.For<IBookRepository>();
        _handler = new GetBookQueryHandler(_bookRepositoryMock);
    }

    [Fact]
    public async Task Handle_Should_ReturnSuccess_WhenBookExistsAsync()
    {
        // Arrange
        var query = new GetBookByIdQuery(s_bookId);
        _bookRepositoryMock.GetByIdAsync(s_bookId).Returns(s_existingBook);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Guard.Against.Null(result.Value);
        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(s_existingBook.Id);
        result.Value.Serie.Should().Be(s_existingBook.Serie);
        result.Value.Title.Should().Be(s_existingBook.Title);
        result.Value.ISBN.Should().Be(s_existingBook.ISBN);
        result.Value.VolumeNumber.Should().Be(s_existingBook.VolumeNumber);
        result.Value.ImageLink.Should().Be(s_existingBook.ImageLink);
        await _bookRepositoryMock.Received(1).GetByIdAsync(s_bookId);
    }

    [Fact]
    public async Task Handle_Should_CallGetByIdAsyncOnceAsync()
    {
        // Arrange
        var query = new GetBookByIdQuery(s_bookId);
        _bookRepositoryMock.GetByIdAsync(s_bookId).Returns(s_existingBook);

        // Act
        await _handler.Handle(query, CancellationToken.None);

        // Assert
        await _bookRepositoryMock.Received(1).GetByIdAsync(Arg.Any<Guid>());
    }

    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenBookIsNullAsync()
    {
        // Arrange
        var query = new GetBookByIdQuery(s_bookId);
        _bookRepositoryMock.GetByIdAsync(s_bookId).ReturnsNull();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(BooksError.NotFound);
        await _bookRepositoryMock.Received(1).GetByIdAsync(s_bookId);
    }

    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenBookDoesNotExistAsync()
    {
        // Arrange
        var nonExistentId = Guid.CreateVersion7();
        var query = new GetBookByIdQuery(nonExistentId);
        _bookRepositoryMock.GetByIdAsync(nonExistentId).ReturnsNull();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(BooksError.NotFound);
        await _bookRepositoryMock.Received(1).GetByIdAsync(nonExistentId);
    }

    [Fact]
    public async Task Handle_Should_ReturnCorrectBook_WhenMultipleBooksExistAsync()
    {
        // Arrange
        var bookId1 = Guid.CreateVersion7();
        var bookId2 = Guid.CreateVersion7();
        var book1 = Book.Create("Serie 1", "Title 1", "978-3-16-148410-0");
        var book2 = Book.Create("Serie 2", "Title 2", "978-0-306-40615-7");

        _bookRepositoryMock.GetByIdAsync(bookId1).Returns(book1);
        _bookRepositoryMock.GetByIdAsync(bookId2).Returns(book2);

        var query = new GetBookByIdQuery(bookId2);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Guard.Against.Null(result.Value);
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(book2);
        result.Value.Serie.Should().Be("Serie 2");
        result.Value.Title.Should().Be("Title 2");
        await _bookRepositoryMock.Received(1).GetByIdAsync(bookId2);
    }

    [Fact]
    public async Task Handle_Should_ReturnBookWithAllPropertiesAsync()
    {
        // Arrange
        var bookId = Guid.CreateVersion7();
        var book = Book.Create("Complete Serie", "Complete Title", "978-3-16-148410-0", 5, "https://example.com/cover.jpg");
        var query = new GetBookByIdQuery(bookId);
        _bookRepositoryMock.GetByIdAsync(bookId).Returns(book);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Guard.Against.Null(result.Value);
        result.IsSuccess.Should().BeTrue();
        result.Value.Serie.Should().Be("Complete Serie");
        result.Value.Title.Should().Be("Complete Title");
        result.Value.ISBN.Should().Be("978-3-16-148410-0");
        result.Value.VolumeNumber.Should().Be(5);
        result.Value.ImageLink.Should().Be("https://example.com/cover.jpg");
    }

    [Fact]
    public async Task Handle_Should_CallGetByIdAsyncWithCorrectIdAsync()
    {
        // Arrange
        var specificId = Guid.CreateVersion7();
        var query = new GetBookByIdQuery(specificId);
        _bookRepositoryMock.GetByIdAsync(specificId).Returns(s_existingBook);

        // Act
        await _handler.Handle(query, CancellationToken.None);

        // Assert
        await _bookRepositoryMock.Received(1).GetByIdAsync(specificId);
        await _bookRepositoryMock.DidNotReceive().GetByIdAsync(Arg.Is<Guid>(g => g != specificId));
    }

    [Fact]
    public async Task Handle_Should_ReturnSameBookInstanceAsync()
    {
        // Arrange
        var query = new GetBookByIdQuery(s_bookId);
        var specificBook = Book.Create("Specific Serie", "Specific Title", "978-3-16-148410-0");
        _bookRepositoryMock.GetByIdAsync(s_bookId).Returns(specificBook);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Guard.Against.Null(result.Value);
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeSameAs(specificBook);
    }
}
