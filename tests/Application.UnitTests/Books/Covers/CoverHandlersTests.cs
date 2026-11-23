using Application.Books.Covers;
using Application.Interfaces;
using Domain.Books;
using Domain.Primitives;
using NSubstitute;

namespace Application.UnitTests.Books.Covers;

public sealed class CoverHandlersTests
{
    private const string SharedCover = "https://res.cloudinary.com/mcm/image/upload/v1/covers/9782205050196.jpg";
    private const string WebCover = "https://books.google.com/cover.jpg";

    private readonly IBookRepository _bookRepository = Substitute.For<IBookRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ICloudinaryService _cloudinary = Substitute.For<ICloudinaryService>();
    private readonly ICoverMoveJobEnqueuer _enqueuer = Substitute.For<ICoverMoveJobEnqueuer>();

    public CoverHandlersTests()
    {
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(Result.Success(1));
        _cloudinary.UploadImageFromUrlAsync(Arg.Any<Uri>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => new CloudinaryUploadResult(
                new Uri($"https://res.cloudinary.com/mcm/image/upload/v2/{callInfo.ArgAt<string>(1)}/{callInfo.ArgAt<string>(2)}.jpg"), null, true, null));
    }

    private static PhysicalBook Book(string imageLink) =>
        PhysicalBook.Create(new BookMetadata("Blacksad", "Arctic Nation", "9782205050196", ImageLink: imageLink), Guid.CreateVersion7()).Value!;

    private static PhysicalBook OwnCoverBook()
    {
        var book = Book(string.Empty);
        book.UseStoredCover($"https://res.cloudinary.com/mcm/image/upload/v1/covers/{book.Id}.webp");
        return book;
    }

    private void Books(params Book[] books) => _bookRepository.ListAsync(Arg.Any<CancellationToken>()).Returns([.. books]);

    [Fact]
    public async Task Handle_Should_CountTheBooksWhoseCoverIsAnImageOfAnotherName()
    {
        Books(Book(SharedCover), Book(WebCover), OwnCoverBook(), Book(string.Empty));
        var handler = new CountCoversToMoveQueryHandler(_bookRepository, TestCoverStores.Create(_cloudinary));

        var result = await handler.Handle(new CountCoversToMoveQuery(), TestContext.Current.CancellationToken);

        result.Value.Should().Be(1);
    }

    [Fact]
    public async Task Handle_Should_StartTheMoveInTheBackground_WhenACoverIsToCopy()
    {
        Books(Book(SharedCover), Book(WebCover), OwnCoverBook());
        var handler = new StartCoverMoveCommandHandler(_bookRepository, TestCoverStores.Create(_cloudinary), _enqueuer);

        var result = await handler.Handle(new StartCoverMoveCommand(), TestContext.Current.CancellationToken);

        result.Value.Should().Be(2);
        _enqueuer.Received(1).Enqueue();
    }

    [Fact]
    public async Task Handle_Should_StartNothing_WhenEveryCoverIsTheImageOfItsBook()
    {
        Books(OwnCoverBook(), Book(string.Empty));
        var handler = new StartCoverMoveCommandHandler(_bookRepository, TestCoverStores.Create(_cloudinary), _enqueuer);

        var result = await handler.Handle(new StartCoverMoveCommand(), TestContext.Current.CancellationToken);

        result.Value.Should().Be(0);
        _enqueuer.DidNotReceive().Enqueue();
    }

    [Fact]
    public async Task Handle_Should_CopyEveryCoverToTheImageOfItsBook_AndGoOnAfterAFailure()
    {
        var shared = Book(SharedCover);
        var notCopied = Book("https://www.bedetheque.com/cover.jpg");
        var notSaved = Book(WebCover);
        var own = OwnCoverBook();
        Books(shared, notCopied, notSaved, own);
        _cloudinary.UploadImageFromUrlAsync(new Uri(notCopied.ImageLink), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new CloudinaryUploadResult(null, null, false, "404"));
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(Result.Success(1), Result.Failure<int>(BooksError.BadRequest));
        var handler = new MoveCoversCommandHandler(_bookRepository, _unitOfWork, TestCoverStores.Create(_cloudinary), NullLogger<MoveCoversCommandHandler>.Instance);

        var result = await handler.Handle(new MoveCoversCommand(), TestContext.Current.CancellationToken);

        result.Value.Should().Be(new CoverMoveSummary(Moved: 1, Failed: 2));
        shared.ImageLink.Should().Be($"https://res.cloudinary.com/mcm/image/upload/v2/covers/{shared.Id}.jpg");
        notCopied.ImageLink.Should().Be("https://www.bedetheque.com/cover.jpg");
        _bookRepository.DidNotReceive().Update(own);
    }
}
