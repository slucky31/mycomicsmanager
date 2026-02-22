using Application.Books.DeleteReadingDate;
using Application.Interfaces;
using Domain.Books;
using NSubstitute;

namespace Application.UnitTests.Books;

public class DeleteReadingDateCommandHandlerTests
{
    private readonly DeleteReadingDateCommandHandler _handler;
    private readonly IBookRepository _bookRepositoryMock;
    private readonly IUnitOfWork _unitOfWorkMock;

    public DeleteReadingDateCommandHandlerTests()
    {
        _bookRepositoryMock = Substitute.For<IBookRepository>();
        _unitOfWorkMock = Substitute.For<IUnitOfWork>();
        _handler = new DeleteReadingDateCommandHandler(_bookRepositoryMock, _unitOfWorkMock);
    }

    [Fact]
    public async Task Handle_Should_ReturnNotFound_WhenBookDoesNotExist()
    {
        // Arrange
        var command = new DeleteReadingDateCommand(Guid.NewGuid(), Guid.NewGuid());
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
        var book = Book.Create("Serie", "Title", "978-3-16-148410-0");
        book.AddReadingDate(DateTime.UtcNow, 4);
        var readingDateId = book.ReadingDates[0].Id;
        var command = new DeleteReadingDateCommand(book.Id, readingDateId);
        _bookRepositoryMock.GetByIdAsync(command.BookId).Returns(book);

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
        var book = Book.Create("Serie", "Title", "978-3-16-148410-0");
        book.AddReadingDate(DateTime.UtcNow, 4);
        var nonExistentReadingDateId = Guid.NewGuid();
        var command = new DeleteReadingDateCommand(book.Id, nonExistentReadingDateId);
        _bookRepositoryMock.GetByIdAsync(command.BookId).Returns(book);

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
        var book = Book.Create("Serie", "Title", "978-3-16-148410-0");
        book.AddReadingDate(DateTime.UtcNow, 3);
        var readingDateId = book.ReadingDates[0].Id;
        var command = new DeleteReadingDateCommand(book.Id, readingDateId);
        var cancellationToken = new CancellationToken();
        _bookRepositoryMock.GetByIdAsync(command.BookId).Returns(book);

        // Act
        await _handler.Handle(command, cancellationToken);

        // Assert
        await _unitOfWorkMock.Received(1).SaveChangesAsync(cancellationToken);
    }
}
