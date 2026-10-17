using Application.Books;
using Application.Books.MoveTargets;
using Application.Interfaces;
using Application.Libraries;
using Application.Libraries.Summaries;
using Domain.Books;
using Domain.Libraries;
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

    // Their summaries, each library holding as many books as the length of its name.
    private void GivenLibraries(params Library[] libraries)
    {
        IReadOnlyList<LibrarySummary> summaries = [.. libraries.Select(l => new LibrarySummary(l.Id, l.Name, l.Color, l.Icon, l.BookType, l.Name.Length))];
        _libraryReadService.GetSummariesAsync(s_userId, Arg.Any<CancellationToken>()).Returns(summaries);
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
        result.Value.Libraries.Select(l => l.BookCount).Should().Equal(2, 6);
        result.Value.SuggestedLibraryId.Should().Be(bd.Id);
        result.Value.Current.Should().Be(new BookMoveTarget(toSort.Id, "À trier", "#5C6BC0", "Bookmark", 0, 7));
        result.Value.BookType.Should().Be(LibraryBookType.Digital);
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

    [Fact]
    public async Task Handle_Should_GiveTheCurrentLibraryAlone_WhenNoOtherLibraryHasItsType()
    {
        var toSort = Digital("À trier");
        GivenLibraries();
        var book = GivenBookIn(toSort, "Blacksad");

        var result = await _handler.Handle(new GetBookMoveTargetsQuery(book.Id, s_userId), TestContext.Current.CancellationToken);

        result.Value!.Libraries.Should().BeEmpty();
        result.Value.Current.Should().Be(new BookMoveTarget(toSort.Id, "À trier", "#5C6BC0", "Bookmark", 0));
    }

    [Fact]
    public async Task Handle_Should_CountTheBooksOfEverySerie_WhenSeveralBooksAreMoved()
    {
        var toSort = Digital("À trier");
        var bd = Digital("BD");
        var mangas = Digital("Mangas");
        GivenLibraries(toSort, bd, mangas);
        var blacksad = GivenBookIn(toSort, "Blacksad");
        var onePiece = GivenBookIn(toSort, "One Piece");
        _bookReadService.ListSerieLocationsAsync(s_userId, Arg.Any<CancellationToken>()).Returns(
        [
            new BookSerieLocationDto(bd.Id, "Blacksad"),
            new BookSerieLocationDto(mangas.Id, "One Piece"),
            new BookSerieLocationDto(mangas.Id, "one piece"),
            new BookSerieLocationDto(mangas.Id, "Naruto")
        ]);

        var result = await _handler.Handle(new GetBookMoveTargetsQuery([blacksad.Id, onePiece.Id, blacksad.Id], s_userId), TestContext.Current.CancellationToken);

        result.Value!.Libraries.Select(l => l.SameSerieCount).Should().Equal(1, 2);
        result.Value.SuggestedLibraryId.Should().Be(mangas.Id);
        result.Value.BookCount.Should().Be(2);
    }

    [Fact]
    public async Task Handle_Should_ReturnNotFound_WhenABookIsMissing_OrInAnotherLibrary()
    {
        var toSort = Digital("À trier");
        var bd = Digital("BD");
        var first = GivenBookIn(toSort, "Blacksad");
        var elsewhere = GivenBookIn(bd, "Blacksad");

        var missing = await _handler.Handle(new GetBookMoveTargetsQuery([first.Id, Guid.CreateVersion7()], s_userId), TestContext.Current.CancellationToken);
        var mixed = await _handler.Handle(new GetBookMoveTargetsQuery([first.Id, elsewhere.Id], s_userId), TestContext.Current.CancellationToken);

        missing.Error.Should().Be(BooksError.NotFound);
        mixed.Error.Should().Be(BooksError.NotFound);
    }

    [Fact]
    public async Task Handle_Should_ReturnBadRequest_WhenTheBooksOrTheUserAreNotGiven()
    {
        var tooMany = Enumerable.Range(0, GetBookMoveTargetsQueryHandler.MaxBooks + 1).Select(_ => Guid.CreateVersion7()).ToList();

        foreach (var query in new[]
        {
            new GetBookMoveTargetsQuery([], s_userId),
            new GetBookMoveTargetsQuery(tooMany, s_userId),
            new GetBookMoveTargetsQuery(Guid.Empty, s_userId),
            new GetBookMoveTargetsQuery(Guid.CreateVersion7(), Guid.Empty),
        })
        {
            (await _handler.Handle(query, TestContext.Current.CancellationToken)).Error.Should().Be(BooksError.BadRequest);
        }

        await _bookRepository.DidNotReceive().GetByIdAsync(Arg.Any<Guid>());
    }
}
