using Application.Books.Covers;
using Application.Interfaces;
using Domain.Books;
using Domain.Primitives;
using NSubstitute;

namespace Application.UnitTests.Books.Covers;

public sealed class UnusedCoversTests
{
    private readonly IBookRepository _bookRepository = Substitute.For<IBookRepository>();
    private readonly ICloudinaryService _cloudinary = Substitute.For<ICloudinaryService>();
    private readonly PhysicalBook _book;

    public UnusedCoversTests()
    {
        _book = PhysicalBook.Create(new BookMetadata("Blacksad", "Arctic Nation", "9782205050196"), Guid.CreateVersion7()).Value!;
        _book.UseStoredCover($"https://res.cloudinary.com/mcm/image/upload/v1/covers/{_book.Id}.webp");
        _bookRepository.ListAsync(Arg.Any<CancellationToken>()).Returns([_book]);
        _cloudinary.ListImagesAsync("covers", Arg.Any<CancellationToken>()).Returns(Result.Success<IReadOnlyList<CloudinaryImage>>(
            [new CloudinaryImage($"covers/{_book.Id}", 100), new CloudinaryImage("covers/9782205050196", 2_000)]));
        _cloudinary.ListImagesAsync("digital-covers", Arg.Any<CancellationToken>()).Returns(Result.Success<IReadOnlyList<CloudinaryImage>>(
            [new CloudinaryImage("digital-covers/9782756015996", 3_000)]));
        _cloudinary.DeleteImagesAsync(Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => Result.Success(callInfo.ArgAt<IReadOnlyList<string>>(0).Count));
    }

    private UnusedCoverFinder Finder() => new(_bookRepository, _cloudinary, TestCoverStores.Create(_cloudinary));

    [Fact]
    public async Task Handle_Should_FindTheImagesOfTheCoverFoldersThatNoBookUses()
    {
        var handler = new FindUnusedCoversQueryHandler(Finder());

        var result = await handler.Handle(new FindUnusedCoversQuery(), TestContext.Current.CancellationToken);

        result.Value!.PublicIds.Should().Equal("covers/9782205050196", "digital-covers/9782756015996");
        result.Value.Bytes.Should().Be(5_000);
    }

    [Fact]
    public async Task FindAsync_Should_Refuse_WhenABookStillUsesASharedImage()
    {
        var shared = PhysicalBook.Create(new BookMetadata("Blacksad", "Âme rouge", "9782205050197",
            ImageLink: "https://res.cloudinary.com/mcm/image/upload/v1/covers/9782205050197.jpg"), Guid.CreateVersion7()).Value!;
        _bookRepository.ListAsync(Arg.Any<CancellationToken>()).Returns([_book, shared]);

        var result = await Finder().FindAsync(TestContext.Current.CancellationToken);

        result.Error.Should().Be(BooksError.CoversNotMoved);
        await _cloudinary.DidNotReceiveWithAnyArgs().ListImagesAsync(default!, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Handle_Should_ReturnTheError_WhenTheImagesCannotBeListed()
    {
        _cloudinary.ListImagesAsync("digital-covers", Arg.Any<CancellationToken>())
            .Returns(Result.Failure<IReadOnlyList<CloudinaryImage>>(BooksError.CoversNotListed));
        var handler = new FindUnusedCoversQueryHandler(Finder());

        var result = await handler.Handle(new FindUnusedCoversQuery(), TestContext.Current.CancellationToken);

        result.Error.Should().Be(BooksError.CoversNotListed);
    }

    [Fact]
    public async Task Handle_Should_DeleteOnlyTheImagesStillUnused()
    {
        var handler = new DeleteUnusedCoversCommandHandler(Finder(), _cloudinary, NullLogger<DeleteUnusedCoversCommandHandler>.Instance);

        // The image of the book was shown as unused before the book took it.
        var result = await handler.Handle(new DeleteUnusedCoversCommand([$"covers/{_book.Id}", "covers/9782205050196"]), TestContext.Current.CancellationToken);

        result.Value.Should().Be(new DeletedCovers(Deleted: 1, Bytes: 2_000));
        await _cloudinary.Received(1).DeleteImagesAsync(
            Arg.Is<IReadOnlyList<string>>(ids => ids.Count == 1 && ids[0] == "covers/9782205050196"), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_DeleteNothing_WhenNoImageAskedIsStillUnused()
    {
        var handler = new DeleteUnusedCoversCommandHandler(Finder(), _cloudinary, NullLogger<DeleteUnusedCoversCommandHandler>.Instance);

        var result = await handler.Handle(new DeleteUnusedCoversCommand([$"covers/{_book.Id}"]), TestContext.Current.CancellationToken);

        result.Value.Should().Be(new DeletedCovers(0, 0));
        await _cloudinary.DidNotReceiveWithAnyArgs().DeleteImagesAsync(default!, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Handle_Should_ReturnBadRequest_WhenNoImageIsAsked()
    {
        var handler = new DeleteUnusedCoversCommandHandler(Finder(), _cloudinary, NullLogger<DeleteUnusedCoversCommandHandler>.Instance);

        var result = await handler.Handle(new DeleteUnusedCoversCommand([]), TestContext.Current.CancellationToken);

        result.Error.Should().Be(BooksError.BadRequest);
        await _bookRepository.DidNotReceive().ListAsync(Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Handle_Should_ReturnTheError_WhenTheImagesCannotBeFoundOrDeleted(bool listFails)
    {
        var error = listFails ? BooksError.CoversNotListed : BooksError.CoversNotDeleted;
        if (listFails)
        {
            _cloudinary.ListImagesAsync("covers", Arg.Any<CancellationToken>())
                .Returns(Result.Failure<IReadOnlyList<CloudinaryImage>>(error));
        }
        else
        {
            _cloudinary.DeleteImagesAsync(Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>()).Returns(Result.Failure<int>(error));
        }

        var handler = new DeleteUnusedCoversCommandHandler(Finder(), _cloudinary, NullLogger<DeleteUnusedCoversCommandHandler>.Instance);

        var result = await handler.Handle(new DeleteUnusedCoversCommand(["covers/9782205050196"]), TestContext.Current.CancellationToken);

        result.Error.Should().Be(error);
    }
}
