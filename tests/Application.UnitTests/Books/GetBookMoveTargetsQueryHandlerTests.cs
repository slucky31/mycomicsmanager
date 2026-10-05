using Application.Books;
using Application.Books.MoveTargets;
using Application.Interfaces;
using Application.Libraries;
using Domain.Books;
using Domain.Libraries;
using Domain.Primitives;
using NSubstitute;

namespace Application.UnitTests.Books;

public class GetBookMoveTargetsQueryHandlerTests
{
    private static readonly Guid s_userId = Guid.CreateVersion7();

    private readonly IBookRepository _bookRepository = Substitute.For<IBookRepository>();
    private readonly IRepository<Library, Guid> _libraryRepository = Substitute.For<IRepository<Library, Guid>>();
    private readonly ILibraryReadService _libraryReadService = Substitute.For<ILibraryReadService>();
    private readonly IBookReadService _bookReadService = Substitute.For<IBookReadService>();
    private readonly GetBookMoveTargetsQueryHandler _handler;

    public GetBookMoveTargetsQueryHandlerTests()
    {
        _handler = new GetBookMoveTargetsQueryHandler(_bookRepository, _libraryRepository, _libraryReadService, _bookReadService);
    }

    private static Library Digital(string name) => Library.Create(name, "#5C6BC0", "Bookmark", LibraryBookType.Digital, s_userId).Value!;

    private void GivenLibraries(params Library[] libraries)
    {
        var page = Substitute.For<IPagedList<Library>>();
        page.Items.Returns(libraries.ToList());
        _libraryReadService.GetLibrariesAsync(null, LibrariesColumn.Name, SortOrder.Ascending, 1, Arg.Any<int>(), s_userId, Arg.Any<CancellationToken>())
            .Returns(page);
    }

    private DigitalBook GivenBookIn(Library library, string serie)
    {
        var book = DigitalBook.Create(new BookMetadata(serie, "Titre", null), library.Id, "/data/x.cbz", 1).Value!;
        _bookRepository.GetByIdAsync(book.Id).Returns(book);
        _libraryRepository.GetByIdAsync(library.Id).Returns(library);
        return book;
    }

    [Fact]
    public async Task Handle_Should_SuggestLibraryHoldingMostBooksOfTheSerie_AmongOtherLibrariesOfSameType()
    {
        var toSort = Digital("À trier");
        var bd = Digital("BD");
        var mangas = Digital("Mangas");
        var paper = Library.Create("Papier", "#5C6BC0", "Bookmark", LibraryBookType.Physical, s_userId).Value!;
        GivenLibraries(toSort, bd, mangas, paper);
        var book = GivenBookIn(toSort, "Blacksad");
        _bookReadService.ListSerieLocationsAsync(s_userId, Arg.Any<CancellationToken>()).Returns(
        [
            new BookSerieLocationDto(toSort.Id, "Blacksad"),
            new BookSerieLocationDto(bd.Id, "blacksad"),
            new BookSerieLocationDto(bd.Id, "Blacksad"),
            new BookSerieLocationDto(mangas.Id, "Blacksad"),
            new BookSerieLocationDto(mangas.Id, "One Piece")
        ]);

        var result = await _handler.Handle(new GetBookMoveTargetsQuery(book.Id, s_userId), TestContext.Current.CancellationToken);

        result.Value!.Libraries.Select(l => l.Name).Should().Equal("BD", "Mangas");
        result.Value.Libraries.Select(l => l.SameSerieCount).Should().Equal(2, 1);
        result.Value.SuggestedLibraryId.Should().Be(bd.Id);
    }

    [Fact]
    public async Task Handle_Should_SuggestNothing_WhenNoOtherLibraryHoldsTheSerie()
    {
        var toSort = Digital("À trier");
        var bd = Digital("BD");
        GivenLibraries(toSort, bd);
        var book = GivenBookIn(toSort, "Blacksad");
        _bookReadService.ListSerieLocationsAsync(s_userId, Arg.Any<CancellationToken>()).Returns([new BookSerieLocationDto(toSort.Id, "Blacksad")]);

        var result = await _handler.Handle(new GetBookMoveTargetsQuery(book.Id, s_userId), TestContext.Current.CancellationToken);

        result.Value!.Libraries.Should().ContainSingle();
        result.Value.SuggestedLibraryId.Should().BeNull();
    }

    [Fact]
    public async Task Handle_Should_ReturnNotFound_WhenBookBelongsToAnotherUser()
    {
        var foreign = Library.Create("À trier", "#5C6BC0", "Bookmark", LibraryBookType.Digital, Guid.CreateVersion7()).Value!;
        var book = GivenBookIn(foreign, "Blacksad");

        var result = await _handler.Handle(new GetBookMoveTargetsQuery(book.Id, s_userId), TestContext.Current.CancellationToken);

        result.Error.Should().Be(BooksError.NotFound);
    }
}
