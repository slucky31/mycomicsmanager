using Application.Books.Delete;
using Application.Interfaces;
using Domain.Books;
using NSubstitute;

namespace Application.UnitTests.Books;

public class DeleteBookCommandHandlerTests
{
    private static readonly Guid s_bookId = Guid.CreateVersion7();
    private static readonly DeleteBookCommand s_command = new(s_bookId);
    private static readonly Book s_existingBook = Book.Create("Test Serie", "Test Title", "978-3-16-148410-0");

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
        _bookRepositoryMock.GetByIdAsync(s_command.Id).Returns(s_existingBook);

        // Act
        var result = await _handler.Handle(s_command, default);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _bookRepositoryMock.Received(1).Remove(Arg.Any<Book>());
        await _unitOfWorkMock.Received(1).SaveChangesAsync(CancellationToken.None);
    }

    [Fact]
    public async Task Handle_Should_ExecuteSaveChangesAsyncOnce()
    {
        // Arrange
        _bookRepositoryMock.GetByIdAsync(s_command.Id).Returns(s_existingBook);

        // Act
        await _handler.Handle(s_command, default);

        // Assert
        await _unitOfWorkMock.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenBookIsNull()
    {
        // Arrange
        _bookRepositoryMock.GetByIdAsync(s_command.Id).Returns((Book?)null);

        // Act
        var result = await _handler.Handle(s_command, default);

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
        _bookRepositoryMock.GetByIdAsync(s_command.Id).Returns(s_existingBook);

        // Act
        await _handler.Handle(s_command, default);

        // Assert
        _bookRepositoryMock.Received(1).Remove(s_existingBook);
    }

    [Fact]
    public async Task Handle_Should_PassCorrectCancellationToken()
    {
        // Arrange
        var cancellationToken = new CancellationToken();
        _bookRepositoryMock.GetByIdAsync(s_command.Id).Returns(s_existingBook);

        // Act
        await _handler.Handle(s_command, cancellationToken);

        // Assert
        await _bookRepositoryMock.Received(1).GetByIdAsync(s_command.Id);
        await _unitOfWorkMock.Received(1).SaveChangesAsync(cancellationToken);
    }

    [Fact]
    public async Task Handle_Should_CallGetByIdAsyncOnce()
    {
        // Arrange
        _bookRepositoryMock.GetByIdAsync(s_command.Id).Returns(s_existingBook);

        // Act
        await _handler.Handle(s_command, default);

        // Assert
        await _bookRepositoryMock.Received(1).GetByIdAsync(s_command.Id);
    }

    [Fact]
    public async Task Handle_Should_NotCallRemove_WhenBookNotFound()
    {
        // Arrange
        _bookRepositoryMock.GetByIdAsync(s_command.Id).Returns((Book?)null);

        // Act
        await _handler.Handle(s_command, default);

        // Assert
        _bookRepositoryMock.DidNotReceive().Remove(Arg.Any<Book>());
    }

    [Fact]
    public async Task Handle_Should_NotCallSaveChanges_WhenBookNotFound()
    {
        // Arrange
        _bookRepositoryMock.GetByIdAsync(s_command.Id).Returns((Book?)null);

        // Act
        await _handler.Handle(s_command, default);

        // Assert
        await _unitOfWorkMock.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
