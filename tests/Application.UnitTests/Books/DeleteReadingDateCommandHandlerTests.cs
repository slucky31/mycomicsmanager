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

    [Fact]
    public async Task Handle_Should_RemoveCorrectReadingDate_WhenBookHasMultipleReadingDates()
    {
        // Arrange
        var book = Book.Create("Serie", "Title", "978-3-16-148410-0");
        var readingDate1 = book.AddReadingDate(DateTime.UtcNow.AddDays(-60), 3);
        var readingDate2 = book.AddReadingDate(DateTime.UtcNow.AddDays(-30), 4);
        var readingDate3 = book.AddReadingDate(DateTime.UtcNow, 5);
        var command = new DeleteReadingDateCommand(book.Id, readingDate2.Id);
        _bookRepositoryMock.GetByIdAsync(command.BookId).Returns(book);

        // Act
        var result = await _handler.Handle(command, default);

        // Assert
        result.IsSuccess.Should().BeTrue();
        book.ReadingDates.Should().HaveCount(2);
        book.ReadingDates.Should().Contain(rd => rd.Id == readingDate1.Id && rd.Rating == 3);
        book.ReadingDates.Should().NotContain(rd => rd.Id == readingDate2.Id);
        book.ReadingDates.Should().Contain(rd => rd.Id == readingDate3.Id && rd.Rating == 5);
    }

    [Fact]
    public async Task Handle_Should_RemoveAllReadingDates_WhenCalledMultipleTimes()
    {
        // Arrange
        var book = Book.Create("Serie", "Title", "978-3-16-148410-0");
        var readingDate1 = book.AddReadingDate(DateTime.UtcNow.AddDays(-30), 3);
        var readingDate2 = book.AddReadingDate(DateTime.UtcNow, 5);
        _bookRepositoryMock.GetByIdAsync(book.Id).Returns(book);

        // Act
        var result1 = await _handler.Handle(new DeleteReadingDateCommand(book.Id, readingDate1.Id), default);
        var result2 = await _handler.Handle(new DeleteReadingDateCommand(book.Id, readingDate2.Id), default);

        // Assert
        result1.IsSuccess.Should().BeTrue();
        result2.IsSuccess.Should().BeTrue();
        book.ReadingDates.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_Should_StillUpdateBook_EvenWhenReadingDateDoesNotExist()
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
        _bookRepositoryMock.Received(1).Update(book);
        await _unitOfWorkMock.Received(1).SaveChangesAsync(CancellationToken.None);
    }
}