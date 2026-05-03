using Application.Books.Delete;
using Application.Interfaces;
using Domain.Books;
using Domain.Libraries;
using NSubstitute;

namespace Application.UnitTests.Books;

public class DeleteBookCommandHandlerTests
{
    private static readonly Guid s_userId = Guid.CreateVersion7();
    private static readonly Guid s_bookId = Guid.CreateVersion7();
    private static readonly Guid s_libraryId = Guid.CreateVersion7();
    private static readonly DeleteBookCommand s_command = new(s_bookId, s_userId);
    private static readonly Book s_existingBook = PhysicalBook.Create("Test Serie", "Test Title", "978-3-16-148410-0", libraryId: s_libraryId).Value!;

    private readonly DeleteBookCommandHandler _handler;
    private readonly IBookRepository _bookRepositoryMock;
    private readonly IUnitOfWork _unitOfWorkMock;
    private readonly IRepository<Library, Guid> _libraryRepositoryMock;
    private readonly IBookFileService _bookFileServiceMock;

    public DeleteBookCommandHandlerTests()
    {
        _bookRepositoryMock = Substitute.For<IBookRepository>();
        _unitOfWorkMock = Substitute.For<IUnitOfWork>();
        _libraryRepositoryMock = Substitute.For<IRepository<Library, Guid>>();
        _bookFileServiceMock = Substitute.For<IBookFileService>();

        _handler = new DeleteBookCommandHandler(_bookRepositoryMock, _unitOfWorkMock, _libraryRepositoryMock, _bookFileServiceMock);
    }

    private static Library CreateLibrary(Guid userId)
        => Library.Create("Test", "#FF0000", "book", LibraryBookType.Physical, userId).Value!;

    [Fact]
    public async Task Handle_Should_ReturnSuccess_WhenBookExists()
    {
        // Arrange
        _bookRepositoryMock.GetByIdAsync(s_command.Id).Returns(s_existingBook);
        _libraryRepositoryMock.GetByIdAsync(s_libraryId).Returns(CreateLibrary(s_userId));

        // Act
        var result = await _handler.Handle(s_command, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _bookRepositoryMock.Received(1).Remove(Arg.Any<Book>());
        await _unitOfWorkMock.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_ExecuteSaveChangesAsyncOnce()
    {
        // Arrange
        _bookRepositoryMock.GetByIdAsync(s_command.Id).Returns(s_existingBook);
        _libraryRepositoryMock.GetByIdAsync(s_libraryId).Returns(CreateLibrary(s_userId));

        // Act
        await _handler.Handle(s_command, TestContext.Current.CancellationToken);

        // Assert
        await _unitOfWorkMock.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenBookIsNull()
    {
        // Arrange
        _bookRepositoryMock.GetByIdAsync(s_command.Id).Returns((Book?)null);

        // Act
        var result = await _handler.Handle(s_command, TestContext.Current.CancellationToken);

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
        var nonExistentCommand = new DeleteBookCommand(nonExistentId, s_userId);
        _bookRepositoryMock.GetByIdAsync(nonExistentId).Returns((Book?)null);

        // Act
        var result = await _handler.Handle(nonExistentCommand, TestContext.Current.CancellationToken);

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
        _libraryRepositoryMock.GetByIdAsync(s_libraryId).Returns(CreateLibrary(s_userId));

        // Act
        await _handler.Handle(s_command, TestContext.Current.CancellationToken);

        // Assert
        _bookRepositoryMock.Received(1).Remove(s_existingBook);
    }

    [Fact]
    public async Task Handle_Should_PassCorrectCancellationToken()
    {
        // Arrange
        var cancellationToken = new CancellationToken();
        _bookRepositoryMock.GetByIdAsync(s_command.Id).Returns(s_existingBook);
        _libraryRepositoryMock.GetByIdAsync(s_libraryId).Returns(CreateLibrary(s_userId));

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
        _libraryRepositoryMock.GetByIdAsync(s_libraryId).Returns(CreateLibrary(s_userId));

        // Act
        await _handler.Handle(s_command, TestContext.Current.CancellationToken);

        // Assert
        await _bookRepositoryMock.Received(1).GetByIdAsync(s_command.Id);
    }

    [Fact]
    public async Task Handle_Should_NotCallRemove_WhenBookNotFound()
    {
        // Arrange
        _bookRepositoryMock.GetByIdAsync(s_command.Id).Returns((Book?)null);

        // Act
        await _handler.Handle(s_command, TestContext.Current.CancellationToken);

        // Assert
        _bookRepositoryMock.DidNotReceive().Remove(Arg.Any<Book>());
    }

    [Fact]
    public async Task Handle_Should_NotCallSaveChanges_WhenBookNotFound()
    {
        // Arrange
        _bookRepositoryMock.GetByIdAsync(s_command.Id).Returns((Book?)null);

        // Act
        await _handler.Handle(s_command, TestContext.Current.CancellationToken);

        // Assert
        await _unitOfWorkMock.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_ReturnNotFound_WhenBookBelongsToOtherUser()
    {
        // Arrange
        var requestingUserId = Guid.CreateVersion7();
        var command = new DeleteBookCommand(s_bookId, UserId: requestingUserId);
        var library = CreateLibrary(s_userId); // different owner
        _bookRepositoryMock.GetByIdAsync(s_bookId).Returns(s_existingBook);
        _libraryRepositoryMock.GetByIdAsync(s_libraryId).Returns(library);

        // Act
        var result = await _handler.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(BooksError.NotFound);
        _bookRepositoryMock.DidNotReceive().Remove(Arg.Any<Book>());
        await _unitOfWorkMock.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_ReturnSuccess_WhenOwnershipVerified()
    {
        // Arrange
        var command = new DeleteBookCommand(s_bookId, UserId: s_userId);
        var library = CreateLibrary(s_userId);
        _bookRepositoryMock.GetByIdAsync(s_bookId).Returns(s_existingBook);
        _libraryRepositoryMock.GetByIdAsync(s_libraryId).Returns(library);

        // Act
        var result = await _handler.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _bookRepositoryMock.Received(1).Remove(s_existingBook);
    }

    [Fact]
    public async Task Handle_Should_NotCallDeleteFile_WhenBookIsPhysical()
    {
        // Arrange
        _bookRepositoryMock.GetByIdAsync(s_command.Id).Returns(s_existingBook);
        _libraryRepositoryMock.GetByIdAsync(s_libraryId).Returns(CreateLibrary(s_userId));

        // Act
        await _handler.Handle(s_command, TestContext.Current.CancellationToken);

        // Assert
        await _bookFileServiceMock.DidNotReceive().DeleteFileAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_DeleteFile_WhenBookIsDigital()
    {
        // Arrange
        const string filePath = "/data/cbz/comic.cbz";
        var digitalBook = DigitalBook.Create("Serie", "Title", null, s_libraryId, filePath, 1024).Value!;
        var command = new DeleteBookCommand(digitalBook.Id, s_userId);
        _bookRepositoryMock.GetByIdAsync(digitalBook.Id).Returns(digitalBook);
        _libraryRepositoryMock.GetByIdAsync(s_libraryId).Returns(CreateLibrary(s_userId));

        // Act
        var result = await _handler.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.Should().BeTrue();
        await _bookFileServiceMock.Received(1).DeleteFileAsync(filePath, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_NotDeleteFile_WhenDigitalBookNotFound()
    {
        // Arrange
        _bookRepositoryMock.GetByIdAsync(s_command.Id).Returns((Book?)null);

        // Act
        await _handler.Handle(s_command, TestContext.Current.CancellationToken);

        // Assert
        await _bookFileServiceMock.DidNotReceive().DeleteFileAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
}
