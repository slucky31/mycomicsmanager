using Application.Books.ArchiveRefresh;
using Application.Interfaces;
using Domain.Books;
using NSubstitute;

namespace Application.UnitTests.Books;

public sealed class StartArchiveRefreshCommandHandlerTests
{
    private static readonly Guid s_userId = Guid.CreateVersion7();

    private readonly IBookRepository _bookRepository = Substitute.For<IBookRepository>();
    private readonly IArchiveRefreshJobEnqueuer _enqueuer = Substitute.For<IArchiveRefreshJobEnqueuer>();
    private readonly StartArchiveRefreshCommandHandler _handler;

    public StartArchiveRefreshCommandHandlerTests()
    {
        _handler = new StartArchiveRefreshCommandHandler(_bookRepository, _enqueuer);
    }

    [Fact]
    public async Task Handle_Should_EnqueueTheRefreshAndReturnTheNumberOfDigitalBooks()
    {
        _bookRepository.ListByUserIdAsync(s_userId, Arg.Any<CancellationToken>()).Returns([
            DigitalBook.Create(new BookMetadata("Blacksad", "Arctic Nation", null), Guid.CreateVersion7(), "/data/a.cbz", 1024).Value!,
            DigitalBook.Create(new BookMetadata("Blacksad", "Âme rouge", null), Guid.CreateVersion7(), "/data/b.cbz", 1024).Value!,
            PhysicalBook.Create(new BookMetadata("Blacksad", "Quelque part entre les ombres", "9782205050196"), Guid.CreateVersion7()).Value!,
        ]);

        var result = await _handler.Handle(new StartArchiveRefreshCommand(s_userId), TestContext.Current.CancellationToken);

        result.Value.Should().Be(2);
        _enqueuer.Received(1).Enqueue(s_userId);
    }

    [Fact]
    public async Task Handle_Should_NotEnqueueAnything_WhenTheUserHasNoDigitalBook()
    {
        _bookRepository.ListByUserIdAsync(s_userId, Arg.Any<CancellationToken>()).Returns([]);

        var result = await _handler.Handle(new StartArchiveRefreshCommand(s_userId), TestContext.Current.CancellationToken);

        result.Value.Should().Be(0);
        _enqueuer.DidNotReceive().Enqueue(Arg.Any<Guid>());
    }

    [Fact]
    public async Task Handle_Should_ReturnBadRequest_WhenTheUserIdIsEmpty()
    {
        var result = await _handler.Handle(new StartArchiveRefreshCommand(Guid.Empty), TestContext.Current.CancellationToken);

        result.Error.Should().Be(BooksError.BadRequest);
        await _bookRepository.DidNotReceive().ListByUserIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }
}
