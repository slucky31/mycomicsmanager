using Application.Books.GetById;
using Application.Interfaces;
using Ardalis.GuardClauses;
using Domain.Books;
using NSubstitute;
using NSubstitute.ReturnsExtensions;

namespace Application.UnitTests.Books;

public sealed class GetBookQueryHandlerTests
{
    private readonly IBookRepository _bookRepository;
    private readonly GetBookQueryHandler _handler;

    public GetBookQueryHandlerTests()
    {
        _bookRepository = Substitute.For<IBookRepository>();
        _handler = new GetBookQueryHandler(_bookRepository);
    }

    [Fact]
    public async Task Handle_ShouldReturnBadRequest_WhenRequestIsNull()
    {
        // Arrange
        GetBookByIdQuery? query = null;

        // Act
        var result = await _handler.Handle(query!, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(BooksError.BadRequest);
        await _bookRepository.DidNotReceive().GetByIdAsync(Arg.Any<Guid>());
    }

    [Fact]
    public async Task Handle_ShouldReturnBadRequest_WhenIdIsEmpty()
    {
        // Arrange
        var query = new GetBookByIdQuery(Guid.Empty);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

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
        var query = new GetBookByIdQuery(bookId);
        _bookRepository.GetByIdAsync(bookId).ReturnsNull();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

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
        var expectedBook = Book.Create("Test Serie", "Test Title", "978-3-16-148410-0", 1, "https://example.com/image.jpg");
        var query = new GetBookByIdQuery(bookId);
        _bookRepository.GetByIdAsync(bookId).Returns(expectedBook);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

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
        var book = Book.Create("Serie", "Title", "978-3-16-148410-0");
        var query = new GetBookByIdQuery(specificId);
        _bookRepository.GetByIdAsync(specificId).Returns(book);

        // Act
        await _handler.Handle(query, CancellationToken.None);

        // Assert
        await _bookRepository.Received(1).GetByIdAsync(specificId);
        await _bookRepository.DidNotReceive().GetByIdAsync(Arg.Is<Guid>(g => g != specificId));
    }
}
