using Application.Books.AddReadingDate;
using Application.Interfaces;
using Domain.Books;
using NSubstitute;

namespace Application.UnitTests.Books;

public class AddReadingDateCommandHandlerTests
{
    private readonly AddReadingDateCommandHandler _handler;
    private readonly IBookRepository _bookRepositoryMock;
    private readonly IUnitOfWork _unitOfWorkMock;

    public AddReadingDateCommandHandlerTests()
    {
        _bookRepositoryMock = Substitute.For<IBookRepository>();
        _unitOfWorkMock = Substitute.For<IUnitOfWork>();
        _handler = new AddReadingDateCommandHandler(_bookRepositoryMock, _unitOfWorkMock);
    }

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
        var book = Book.Create("Serie", "Title", "978-3-16-148410-0");
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
        var book = Book.Create("Serie", "Title", "978-3-16-148410-0");
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
        var book = Book.Create("Serie", "Title", "978-3-16-148410-0");
        var command = new AddReadingDateCommand(book.Id, 4);
        var cancellationToken = new CancellationToken();
        _bookRepositoryMock.GetByIdAsync(command.BookId).Returns(book);

        // Act
        await _handler.Handle(command, cancellationToken);

        // Assert
        await _unitOfWorkMock.Received(1).SaveChangesAsync(cancellationToken);
    }
}
