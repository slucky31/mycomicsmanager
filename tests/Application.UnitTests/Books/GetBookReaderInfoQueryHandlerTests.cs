using Application.Books.Read;
using Application.Interfaces;
using Domain.Books;
using Domain.Errors;
using Domain.Libraries;
using NSubstitute;

namespace Application.UnitTests.Books;

public class GetBookReaderInfoQueryHandlerTests
{
    private static readonly Guid s_userId = Guid.CreateVersion7();
    private const string FilePath = "/library/BD/blacksad-01.cbz";

    private readonly IBookRepository _bookRepository = Substitute.For<IBookRepository>();
    private readonly IRepository<Library, Guid> _libraryRepository = Substitute.For<IRepository<Library, Guid>>();
    private readonly IComicPageReader _pageReader = Substitute.For<IComicPageReader>();
    private readonly Library _library = Library.Create("BD", "#5C6BC0", "Bookmark", LibraryBookType.Digital, s_userId).Value!;
    private readonly GetBookReaderInfoQueryHandler _handler;

    public GetBookReaderInfoQueryHandlerTests()
    {
        _libraryRepository.GetByIdAsync(_library.Id).Returns(_library);
        _handler = new GetBookReaderInfoQueryHandler(_bookRepository, _libraryRepository, _pageReader);
    }

    private DigitalBook CreateDigitalBook(int lastReadPage = 0)
    {
        var book = DigitalBook.Create(new BookMetadata("Blacksad", "Quelque part entre les ombres", null), _library.Id, FilePath, 1_000).Value!;
        book.UpdateReadingProgress(lastReadPage);
        _bookRepository.GetByIdAsync(book.Id).Returns(book);
        return book;
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public async Task Handle_Should_ReturnBadRequest_WhenBookIdOrUserIdIsEmpty(bool emptyBookId, bool emptyUserId)
    {
        var query = new GetBookReaderInfoQuery(emptyBookId ? Guid.Empty : Guid.NewGuid(), emptyUserId ? Guid.Empty : s_userId);

        var result = await _handler.Handle(query, TestContext.Current.CancellationToken);

        result.Error.Should().Be(BooksError.BadRequest);
        await _bookRepository.DidNotReceive().GetByIdAsync(Arg.Any<Guid>());
    }

    [Fact]
    public async Task Handle_Should_ReturnNotFound_WhenBookBelongsToAnotherUser()
    {
        var book = CreateDigitalBook();

        var result = await _handler.Handle(new GetBookReaderInfoQuery(book.Id, Guid.CreateVersion7()), TestContext.Current.CancellationToken);

        result.Error.Should().Be(BooksError.NotFound);
        await _pageReader.DidNotReceive().CountPagesAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_ReturnNotDigital_WhenBookIsPhysical()
    {
        var book = PhysicalBook.Create(new BookMetadata("Blacksad", "Arctic Nation", "978-3-16-148410-0"), _library.Id).Value!;
        _bookRepository.GetByIdAsync(book.Id).Returns(book);

        var result = await _handler.Handle(new GetBookReaderInfoQuery(book.Id, s_userId), TestContext.Current.CancellationToken);

        result.Error.Should().Be(BooksError.NotDigital);
    }

    [Fact]
    public async Task Handle_Should_ReturnReaderError_WhenArchiveCannotBeRead()
    {
        var book = CreateDigitalBook();
        _pageReader.CountPagesAsync(FilePath, Arg.Any<CancellationToken>()).Returns(FileProcessingError.CorruptArchive);

        var result = await _handler.Handle(new GetBookReaderInfoQuery(book.Id, s_userId), TestContext.Current.CancellationToken);

        result.Error.Should().Be(FileProcessingError.CorruptArchive);
    }

    [Fact]
    public async Task Handle_Should_ReturnPageNotFound_WhenArchiveHasNoPage()
    {
        var book = CreateDigitalBook();
        _pageReader.CountPagesAsync(FilePath, Arg.Any<CancellationToken>()).Returns(0);

        var result = await _handler.Handle(new GetBookReaderInfoQuery(book.Id, s_userId), TestContext.Current.CancellationToken);

        result.Error.Should().Be(BooksError.PageNotFound);
    }

    [Theory]
    [InlineData(7, 7)]
    [InlineData(42, 19)] // archive replaced by a shorter one since the progress was saved
    public async Task Handle_Should_ReturnPageCountAndLastReadPage_WhenBookIsReadable(int savedPage, int expectedPage)
    {
        var book = CreateDigitalBook(savedPage);
        _pageReader.CountPagesAsync(FilePath, Arg.Any<CancellationToken>()).Returns(20);

        var result = await _handler.Handle(new GetBookReaderInfoQuery(book.Id, s_userId), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(new BookReaderInfoDto(book.Id, "Blacksad", "Quelque part entre les ombres", 1, 20, expectedPage));
    }
}
