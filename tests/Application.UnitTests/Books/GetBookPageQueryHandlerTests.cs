using Application.Books.Read;
using Application.Interfaces;
using Domain.Books;
using Domain.Libraries;
using NSubstitute;

namespace Application.UnitTests.Books;

public class GetBookPageQueryHandlerTests
{
    private static readonly Guid s_userId = Guid.CreateVersion7();
    private const string FilePath = "/library/BD/blacksad-01.cbz";

    private readonly IBookRepository _bookRepository = Substitute.For<IBookRepository>();
    private readonly IRepository<Library, Guid> _libraryRepository = Substitute.For<IRepository<Library, Guid>>();
    private readonly IComicPageReader _pageReader = Substitute.For<IComicPageReader>();
    private readonly DigitalBook _book;
    private readonly GetBookPageQueryHandler _handler;

    public GetBookPageQueryHandlerTests()
    {
        var library = Library.Create("BD", "#5C6BC0", "Bookmark", LibraryBookType.Digital, s_userId).Value!;
        _libraryRepository.GetByIdAsync(library.Id).Returns(library);
        _book = DigitalBook.Create(new BookMetadata("Blacksad", "Arctic Nation", null), library.Id, FilePath, 1_000).Value!;
        _bookRepository.GetByIdAsync(_book.Id).Returns(_book);
        _handler = new GetBookPageQueryHandler(_bookRepository, _libraryRepository, _pageReader);
    }

    [Fact]
    public async Task Handle_Should_ReturnBadRequest_WhenUserIdIsEmpty()
    {
        var result = await _handler.Handle(new GetBookPageQuery(_book.Id, Guid.Empty, 0), TestContext.Current.CancellationToken);

        result.Error.Should().Be(BooksError.BadRequest);
        await _bookRepository.DidNotReceive().GetByIdAsync(Arg.Any<Guid>());
    }

    [Fact]
    public async Task Handle_Should_ReturnPageNotFound_WhenPageIndexIsNegative()
    {
        var result = await _handler.Handle(new GetBookPageQuery(_book.Id, s_userId, -1), TestContext.Current.CancellationToken);

        result.Error.Should().Be(BooksError.PageNotFound);
        await _bookRepository.DidNotReceive().GetByIdAsync(Arg.Any<Guid>());
    }

    [Fact]
    public async Task Handle_Should_NotReadArchive_WhenBookBelongsToAnotherUser()
    {
        var result = await _handler.Handle(new GetBookPageQuery(_book.Id, Guid.CreateVersion7(), 0), TestContext.Current.CancellationToken);

        result.Error.Should().Be(BooksError.NotFound);
        await _pageReader.DidNotReceive().ReadPageAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_ReturnPageFromBookArchive_WhenBookIsOwned()
    {
        var page = new ComicPage(new byte[] { 1, 2, 3 }, "image/webp", DateTime.UtcNow);
        _pageReader.ReadPageAsync(FilePath, 4, Arg.Any<CancellationToken>()).Returns(page);

        var result = await _handler.Handle(new GetBookPageQuery(_book.Id, s_userId, 4), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeSameAs(page);
    }
}
