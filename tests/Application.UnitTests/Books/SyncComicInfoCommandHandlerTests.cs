using Application.Books.ComicInfoSync;
using Application.Interfaces;
using Domain.Books;
using Domain.Primitives;
using NSubstitute;

namespace Application.UnitTests.Books;

public sealed class SyncComicInfoCommandHandlerTests
{
    private static readonly Guid s_userId = Guid.CreateVersion7();

    private readonly IBookRepository _bookRepository = Substitute.For<IBookRepository>();
    private readonly IComicInfoArchiveWriter _comicInfoWriter = Substitute.For<IComicInfoArchiveWriter>();
    private readonly SyncComicInfoCommandHandler _handler;

    public SyncComicInfoCommandHandlerTests()
    {
        _handler = new SyncComicInfoCommandHandler(_bookRepository, _comicInfoWriter, NullLogger<SyncComicInfoCommandHandler>.Instance);
    }

    [Fact]
    public async Task Handle_Should_WriteTheComicInfoOfEveryDigitalBook_AndGoOnAfterAFailure()
    {
        var missing = DigitalBook.Create(new BookMetadata("Blacksad", "Arctic Nation", null, 2), Guid.CreateVersion7(), "/data/a.cbz", 1024).Value!;
        var present = DigitalBook.Create(new BookMetadata("Blacksad", "Âme rouge", null, 3), Guid.CreateVersion7(), "/data/b.cbz", 1024).Value!;
        var physical = PhysicalBook.Create(new BookMetadata("Blacksad", "Quelque part entre les ombres", "9782205050196"), Guid.CreateVersion7()).Value!;
        _bookRepository.ListByUserIdAsync(s_userId, Arg.Any<CancellationToken>()).Returns([missing, physical, present]);
        _comicInfoWriter.WriteAsync(missing.FilePath, Arg.Any<ComicInfoData>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure(BooksError.FileNotFound));
        _comicInfoWriter.WriteAsync(present.FilePath, Arg.Any<ComicInfoData>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        var result = await _handler.Handle(new SyncComicInfoCommand(s_userId), TestContext.Current.CancellationToken);

        result.Value.Should().Be(new ComicInfoSyncSummary(Updated: 1, Failed: 1));
        await _comicInfoWriter.Received(1).WriteAsync(present.FilePath,
            Arg.Is<ComicInfoData>(d => d.Title == "Âme rouge" && d.Number == 3), Arg.Any<CancellationToken>());
        await _comicInfoWriter.Received(2).WriteAsync(Arg.Any<string>(), Arg.Any<ComicInfoData>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_ReturnBadRequest_WhenTheUserIdIsEmpty()
    {
        var result = await _handler.Handle(new SyncComicInfoCommand(Guid.Empty), TestContext.Current.CancellationToken);

        result.Error.Should().Be(BooksError.BadRequest);
        await _bookRepository.DidNotReceive().ListByUserIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }
}
