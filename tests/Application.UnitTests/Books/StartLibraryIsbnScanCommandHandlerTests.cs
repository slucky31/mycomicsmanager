using Application.Books.IsbnScan;
using Application.Interfaces;
using Domain.Libraries;
using NSubstitute;

namespace Application.UnitTests.Books;

public sealed class StartLibraryIsbnScanCommandHandlerTests
{
    private static readonly Guid s_userId = Guid.CreateVersion7();

    private readonly IRepository<Library, Guid> _libraryRepository = Substitute.For<IRepository<Library, Guid>>();
    private readonly IBookRepository _bookRepository = Substitute.For<IBookRepository>();
    private readonly IIsbnScanJobEnqueuer _enqueuer = Substitute.For<IIsbnScanJobEnqueuer>();
    private readonly StartLibraryIsbnScanCommandHandler _handler;

    public StartLibraryIsbnScanCommandHandlerTests()
    {
        _handler = new StartLibraryIsbnScanCommandHandler(_libraryRepository, _bookRepository, _enqueuer);
    }

    private Library CreateLibrary(LibraryBookType bookType = LibraryBookType.Digital, Guid? userId = null)
    {
        var library = Library.Create("Comics", "#000000", "book", bookType, userId ?? s_userId).Value!;
        _libraryRepository.GetByIdAsync(library.Id).Returns(library);
        return library;
    }

    [Fact]
    public async Task Handle_Should_EnqueueTheScanAndReturnTheNumberOfBooks_WhenSomeBooksHaveNoIsbn()
    {
        var library = CreateLibrary();
        _bookRepository.ListIdsToScanForIsbnAsync(library.Id, Arg.Any<CancellationToken>())
            .Returns([Guid.CreateVersion7(), Guid.CreateVersion7()]);

        var result = await _handler.Handle(new StartLibraryIsbnScanCommand(library.Id, s_userId), TestContext.Current.CancellationToken);

        result.Value.Should().Be(2);
        _enqueuer.Received(1).Enqueue(library.Id);
    }

    [Fact]
    public async Task Handle_Should_NotEnqueueAnything_WhenNoBookNeedsAScan()
    {
        var library = CreateLibrary();
        _bookRepository.ListIdsToScanForIsbnAsync(library.Id, Arg.Any<CancellationToken>()).Returns([]);

        var result = await _handler.Handle(new StartLibraryIsbnScanCommand(library.Id, s_userId), TestContext.Current.CancellationToken);

        result.Value.Should().Be(0);
        _enqueuer.DidNotReceive().Enqueue(Arg.Any<Guid>());
    }

    [Fact]
    public async Task Handle_Should_ReturnBadRequest_WhenAnIdIsEmpty()
    {
        var result = await _handler.Handle(new StartLibraryIsbnScanCommand(Guid.Empty, s_userId), TestContext.Current.CancellationToken);
        var noUser = await _handler.Handle(new StartLibraryIsbnScanCommand(Guid.CreateVersion7(), Guid.Empty), TestContext.Current.CancellationToken);

        result.Error.Should().Be(LibrariesError.BadRequest);
        noUser.Error.Should().Be(LibrariesError.BadRequest);
        await _libraryRepository.DidNotReceive().GetByIdAsync(Arg.Any<Guid>());
    }

    [Fact]
    public async Task Handle_Should_ReturnNotFound_WhenTheLibraryBelongsToAnotherUser()
    {
        var library = CreateLibrary(userId: Guid.CreateVersion7());

        var result = await _handler.Handle(new StartLibraryIsbnScanCommand(library.Id, s_userId), TestContext.Current.CancellationToken);

        result.Error.Should().Be(LibrariesError.NotFound);
        _enqueuer.DidNotReceive().Enqueue(Arg.Any<Guid>());
    }

    [Fact]
    public async Task Handle_Should_ReturnNotFound_WhenTheLibraryDoesNotExist()
    {
        var result = await _handler.Handle(new StartLibraryIsbnScanCommand(Guid.CreateVersion7(), s_userId), TestContext.Current.CancellationToken);

        result.Error.Should().Be(LibrariesError.NotFound);
    }

    [Fact]
    public async Task Handle_Should_ReturnBookTypeMismatch_WhenTheLibraryHoldsPhysicalBooks()
    {
        var library = CreateLibrary(LibraryBookType.Physical);

        var result = await _handler.Handle(new StartLibraryIsbnScanCommand(library.Id, s_userId), TestContext.Current.CancellationToken);

        result.Error.Should().Be(LibrariesError.BookTypeMismatch);
    }
}
