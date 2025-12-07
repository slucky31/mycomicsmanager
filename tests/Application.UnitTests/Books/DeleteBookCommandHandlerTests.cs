using Application.Books.Delete;
using Application.Interfaces;
using Domain.Books;
using Domain.Primitives;
using NSubstitute;

namespace Application.UnitTests.Books;

public class DeleteBookCommandHandlerTests
{
    private static readonly Guid BookId = Guid.CreateVersion7();
    private static readonly DeleteBookCommand Command = new(BookId);
    private static readonly Book ExistingBook = Book.Create("Test Serie", "Test Title", "978-3-16-148410-0");

    private readonly DeleteBookCommandHandler _handler;
    private readonly IBookRepository _bookRepositoryMock;
    private readonly IUnitOfWork _unitOfWorkMock;

    public DeleteBookCommandHandlerTests()
    {
        _bookRepositoryMock = Substitute.For<IBookRepository>();
        _unitOfWorkMock = Substitute.For<IUnitOfWork>();

        _handler = new DeleteBookCommandHandler(_bookRepositoryMock, _unitOfWorkMock);
    }

    [Fact]
    public async Task Handle_Should_ReturnSuccess_WhenBookExists()
    {
        // Arrange
        _bookRepositoryMock.GetByIdAsync(Command.Id).Returns(ExistingBook);

        // Act
        var result = await _handler.Handle(Command, default);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _bookRepositoryMock.Received(1).Remove(Arg.Any<Book>());
        await _unitOfWorkMock.Received(1).SaveChangesAsync(CancellationToken.None);
    }

    [Fact]
    public async Task Handle_Should_ExecuteSaveChangesAsyncOnce()
    {
        // Arrange
        _bookRepositoryMock.GetByIdAsync(Command.Id).Returns(ExistingBook);

        // Act
        await _handler.Handle(Command, default);

        // Assert
        await _unitOfWorkMock.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenBookIsNull()
    {
        // Arrange
        _bookRepositoryMock.GetByIdAsync(Command.Id).Returns((Book?)null);

        // Act
        var result = await _handler.Handle(Command, default);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(BooksError.NotFound);
        _bookRepositoryMock.Received(0).Remove(Arg.Any<Book>());
        await _unitOfWorkMock.Received(0).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenBookDoesNotExist()
    {
        // Arrange
        var nonExistentId = Guid.CreateVersion7();
        var nonExistentCommand = new DeleteBookCommand(nonExistentId);
        _bookRepositoryMock.GetByIdAsync(nonExistentId).Returns((Book?)null);

        // Act
        var result = await _handler.Handle(nonExistentCommand, default);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(BooksError.NotFound);
        _bookRepositoryMock.Received(0).Remove(Arg.Any<Book>());
        await _unitOfWorkMock.Received(0).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_CallRemoveWithCorrectBook()
    {
        // Arrange
        _bookRepositoryMock.GetByIdAsync(Command.Id).Returns(ExistingBook);

        // Act
        await _handler.Handle(Command, default);

        // Assert
        _bookRepositoryMock.Received(1).Remove(ExistingBook);
    }

    [Fact]
    public async Task Handle_Should_PassCorrectCancellationToken()
    {
        // Arrange
        var cancellationToken = new CancellationToken();
        _bookRepositoryMock.GetByIdAsync(Command.Id).Returns(ExistingBook);

        // Act
        await _handler.Handle(Command, cancellationToken);

        // Assert
        await _bookRepositoryMock.Received(1).GetByIdAsync(Command.Id);
        await _unitOfWorkMock.Received(1).SaveChangesAsync(cancellationToken);
    }

    [Fact]
    public async Task Handle_Should_CallGetByIdAsyncOnce()
    {
        // Arrange
        _bookRepositoryMock.GetByIdAsync(Command.Id).Returns(ExistingBook);

        // Act
        await _handler.Handle(Command, default);

        // Assert
        await _bookRepositoryMock.Received(1).GetByIdAsync(Command.Id);
    }

    [Fact]
    public async Task Handle_Should_NotCallRemove_WhenBookNotFound()
    {
        // Arrange
        _bookRepositoryMock.GetByIdAsync(Command.Id).Returns((Book?)null);

        // Act
        await _handler.Handle(Command, default);

        // Assert
        _bookRepositoryMock.DidNotReceive().Remove(Arg.Any<Book>());
    }

    [Fact]
    public async Task Handle_Should_NotCallSaveChanges_WhenBookNotFound()
    {
        // Arrange
        _bookRepositoryMock.GetByIdAsync(Command.Id).Returns((Book?)null);

        // Act
        await _handler.Handle(Command, default);

        // Assert
        await _unitOfWorkMock.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
