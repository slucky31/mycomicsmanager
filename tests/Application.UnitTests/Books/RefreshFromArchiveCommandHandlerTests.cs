using Application.Books;
using Application.Books.ArchiveRefresh;
using Application.Interfaces;
using Domain.Books;
using Domain.Primitives;
using NSubstitute;

namespace Application.UnitTests.Books;

public sealed class RefreshFromArchiveCommandHandlerTests
{
    private static readonly Guid s_userId = Guid.CreateVersion7();
    private const string WebCover = "https://res.cloudinary.com/demo/covers/9782205050196.jpg";
    private const string ArchiveCover = "https://res.cloudinary.com/demo/digital-covers/9782205050196.webp";
    private static readonly Uri s_uploadedCover = new("https://res.cloudinary.com/demo/digital-covers/new.webp");

    private readonly IBookRepository _bookRepository = Substitute.For<IBookRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IComicInfoArchiveWriter _comicInfoWriter = Substitute.For<IComicInfoArchiveWriter>();
    private readonly IComicPageReader _pageReader = Substitute.For<IComicPageReader>();
    private readonly ICloudinaryService _cloudinary = Substitute.For<ICloudinaryService>();
    private readonly RefreshFromArchiveCommandHandler _handler;

    public RefreshFromArchiveCommandHandlerTests()
    {
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(Result<int>.Success(1));
        _comicInfoWriter.WriteAsync(Arg.Any<string>(), Arg.Any<ComicInfoData>(), Arg.Any<CancellationToken>()).Returns(Result.Success());
        _pageReader.ReadPageAsync(Arg.Any<string>(), 0, Arg.Any<CancellationToken>())
            .Returns(Result<ComicPage>.Success(new ComicPage(new byte[] { 1, 2, 3 }, "image/webp", DateTime.UtcNow)));
        _cloudinary.UploadImageFromStreamAsync(Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new CloudinaryUploadResult(s_uploadedCover, "new", true, null));
        _handler = new RefreshFromArchiveCommandHandler(_bookRepository, _pageReader, _cloudinary,
            new BookSaver(_bookRepository, _unitOfWork, _comicInfoWriter, NullLogger<BookSaver>.Instance),
            NullLogger<RefreshFromArchiveCommandHandler>.Instance);
    }

    private DigitalBook BookWithPages(string path, string? isbn, string cover, int? pages, Result<int> archivePages)
    {
        var book = DigitalBook.Create(new BookMetadata("Blacksad", path, isbn, ImageLink: cover, NumberOfPages: pages),
            Guid.CreateVersion7(), path, 1024).Value!;
        _pageReader.CountPagesAsync(path, Arg.Any<CancellationToken>()).Returns(archivePages);
        return book;
    }

    [Fact]
    public async Task Handle_Should_ReadTheCoverAndThePagesOfEveryDigitalBook_AndGoOnAfterAFailure()
    {
        var replaced = BookWithPages("/data/a.cbz", "9782205050196", WebCover, 56, 48);
        var upToDate = BookWithPages("/data/b.cbz", null, ArchiveCover, 48, 48);
        var unreadable = BookWithPages("/data/c.cbz", null, ArchiveCover, 48, Result<int>.Failure(BooksError.FileNotFound));
        var empty = BookWithPages("/data/d.cbz", null, ArchiveCover, 48, 0);
        var physical = PhysicalBook.Create(new BookMetadata("Blacksad", "Arctic Nation", "9782205050197", ImageLink: WebCover, NumberOfPages: 56), Guid.CreateVersion7()).Value!;
        _bookRepository.ListByUserIdAsync(s_userId, Arg.Any<CancellationToken>()).Returns([replaced, physical, upToDate, unreadable, empty]);

        var result = await _handler.Handle(new RefreshFromArchiveCommand(s_userId), TestContext.Current.CancellationToken);

        result.Value.Should().Be(new ArchiveRefreshSummary(Updated: 1, Unchanged: 1, Failed: 2));
        replaced.NumberOfPages.Should().Be(48);
        replaced.ImageLink.Should().Be(s_uploadedCover.ToString());
        physical.NumberOfPages.Should().Be(56);
        await _cloudinary.Received(1).UploadImageFromStreamAsync(Arg.Any<Stream>(), "cover.webp", DigitalCovers.Folder, "9782205050196", Arg.Any<CancellationToken>());
        await _comicInfoWriter.Received(1).WriteAsync("/data/a.cbz", Arg.Is<ComicInfoData>(d => d.PageCount == 48), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Handle_Should_KeepTheCurrentCover_WhenTheFirstPageCannotBeUploaded(bool pageIsReadable)
    {
        var book = BookWithPages("/data/a.cbz", null, WebCover, null, 48);
        _bookRepository.ListByUserIdAsync(s_userId, Arg.Any<CancellationToken>()).Returns([book]);
        if (pageIsReadable)
        {
            _cloudinary.UploadImageFromStreamAsync(Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns(new CloudinaryUploadResult(null, null, false, "upload error"));
        }
        else
        {
            _pageReader.ReadPageAsync("/data/a.cbz", 0, Arg.Any<CancellationToken>()).Returns(Result<ComicPage>.Failure(BooksError.PageNotFound));
        }

        var result = await _handler.Handle(new RefreshFromArchiveCommand(s_userId), TestContext.Current.CancellationToken);

        result.Value.Should().Be(new ArchiveRefreshSummary(Updated: 1, Unchanged: 0, Failed: 0));
        book.ImageLink.Should().Be(WebCover);
        book.NumberOfPages.Should().Be(48);
    }

    [Fact]
    public async Task Handle_Should_UploadAnyOtherPageAsAJpeg_NamedByTheIdOfTheBook_WhenItHasNoIsbn()
    {
        var book = BookWithPages("/data/a.cbz", null, string.Empty, 48, 48);
        _bookRepository.ListByUserIdAsync(s_userId, Arg.Any<CancellationToken>()).Returns([book]);
        _pageReader.ReadPageAsync("/data/a.cbz", 0, Arg.Any<CancellationToken>())
            .Returns(Result<ComicPage>.Success(new ComicPage(new byte[] { 1 }, "image/jpeg", DateTime.UtcNow)));

        await _handler.Handle(new RefreshFromArchiveCommand(s_userId), TestContext.Current.CancellationToken);

        await _cloudinary.Received(1).UploadImageFromStreamAsync(Arg.Any<Stream>(), "cover.jpg", DigitalCovers.Folder, $"digital-{book.Id}", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_CountTheBookAsFailed_WhenItCannotBeSaved()
    {
        var book = BookWithPages("/data/a.cbz", null, ArchiveCover, 56, 48);
        _bookRepository.ListByUserIdAsync(s_userId, Arg.Any<CancellationToken>()).Returns([book]);
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(Result<int>.Failure(BooksError.BadRequest));

        var result = await _handler.Handle(new RefreshFromArchiveCommand(s_userId), TestContext.Current.CancellationToken);

        result.Value.Should().Be(new ArchiveRefreshSummary(Updated: 0, Unchanged: 0, Failed: 1));
    }

    [Fact]
    public async Task Handle_Should_ReturnBadRequest_WhenTheUserIdIsEmpty()
    {
        var result = await _handler.Handle(new RefreshFromArchiveCommand(Guid.Empty), TestContext.Current.CancellationToken);

        result.Error.Should().Be(BooksError.BadRequest);
        await _bookRepository.DidNotReceive().ListByUserIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }
}
