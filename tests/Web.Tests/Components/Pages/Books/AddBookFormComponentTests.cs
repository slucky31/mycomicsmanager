using Application.Interfaces;
using AwesomeAssertions;
using Bunit;
using Bunit.TestDoubles;
using Domain.Books;
using Domain.Libraries;
using Domain.Primitives;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Web.Components.Pages.Books;
using Web.Services;
using Xunit;

namespace Web.Tests.Components.Pages.Books;

public sealed class AddBookFormComponentTests : IAsyncDisposable
{
    private const string Isbn = "9782413015406";

    private readonly BunitContext _ctx = new();
    private readonly ILibrariesService _librariesService = Substitute.For<ILibrariesService>();
    private readonly IBooksService _booksService = Substitute.For<IBooksService>();
    private readonly IComicSearchService _searchService = Substitute.For<IComicSearchService>();
    private readonly ISnackbar _snackbar = Substitute.For<ISnackbar>();
    private readonly BunitNavigationManager _navigation;
    private readonly Library _readBooks = Library.Create("Read Books", "#2E7D32", "Star", LibraryBookType.Physical, Guid.CreateVersion7()).Value!;
    private readonly Library _wishlist = Library.Create("Wishlist", "#C62828", "Bookmark", LibraryBookType.Physical, Guid.CreateVersion7()).Value!;
    private readonly Library _ebooks = Library.Create("Ebooks", "#5C6BC0", "Bookmark", LibraryBookType.Digital, Guid.CreateVersion7()).Value!;

    private static readonly ComicSearchResult Extremity = new(
        "Extremity", "Extremity", Isbn, 1, "/covers/extremity.webp",
        "Daniel Warren Johnson", "Delcourt", new DateOnly(2019, 4, 3), 271, Found: true)
    {
        Source = "BnF",
    };

    public AddBookFormComponentTests()
    {
        var page = Substitute.For<IPagedList<Library>>();
        page.Items.Returns([_readBooks, _wishlist, _ebooks]);
        _librariesService.FilterBy(Arg.Any<string?>(), Arg.Any<LibrariesColumn?>(), Arg.Any<SortOrder?>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(_ => Result<IPagedList<Library>>.Success(page));
        _booksService.Create(Arg.Any<CreateBookRequest>(), Arg.Any<CancellationToken>())
            .Returns(_ => Result<Book>.Success(PhysicalBook.Create(new BookMetadata("Extremity", "Extremity", Isbn), _readBooks.Id).Value!));
        _searchService.SearchByIsbnAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(Extremity);

        _ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        _ctx.Services.AddMudServices();
        _ctx.Services.AddSingleton(_librariesService);
        _ctx.Services.AddSingleton(_booksService);
        _ctx.Services.AddSingleton(_searchService);
        _ctx.Services.AddSingleton(_snackbar);
        _navigation = _ctx.Services.GetRequiredService<BunitNavigationManager>();
    }

    public async ValueTask DisposeAsync()
    {
        _snackbar.Dispose();
        await _ctx.DisposeAsync();
    }

    private IRenderedComponent<AddBookForm> Render(string query)
    {
        _navigation.NavigateTo($"/books/add/form{query}");
        return _ctx.Render<AddBookForm>();
    }

    private IRenderedComponent<AddBookForm> RenderFound() => Render($"?isbn={Isbn}&libraryId={_readBooks.Id}");

    private static Task TypeAsync(IRenderedComponent<AddBookForm> cut, string id, string value) =>
        cut.Find($"#{id}").InputAsync(new() { Value = value });

    [Fact]
    public void AddBookForm_Should_ShowTheBookFound_WithTheLibraryOfThePage()
    {
        var cut = RenderFound();

        cut.Find("h1").TextContent.Should().Be("Book found");
        cut.Find(".form-bar-subtitle").TextContent.Should().Be($"ISBN {Isbn}");
        cut.Find(".found-title").TextContent.Should().Be("Extremity");
        cut.Find(".found-source").TextContent.Should().Be("Found on BnF");
        cut.Find("#book-library").GetAttribute("value").Should().Be(_readBooks.Id.ToString());
        cut.FindAll("#book-library option").Select(o => o.TextContent).Should().Equal("Read Books", "Wishlist");
        cut.Find(".found-actions .form-button-primary").TextContent.Should().Be("Add to Read Books");
    }

    [Fact]
    public async Task SaveAsync_Should_AddTheBookFound_WithItsRating_InOneTap()
    {
        var cut = RenderFound();
        await cut.Find("button[aria-label='4 stars']").ClickAsync(new());

        await cut.Find(".found-actions .form-button-primary").ClickAsync(new());

        await _booksService.Received(1).Create(
            Arg.Is<CreateBookRequest>(r => r.Title == "Extremity" && r.Isbn == Isbn && r.LibraryId == _readBooks.Id && r.Rating == 4 && r.NumberOfPages == 271),
            Arg.Any<CancellationToken>());
        _navigation.Uri.Should().EndWith($"/libraries/{_readBooks.Id}");
        _snackbar.Received(1).Add("Extremity was added to Read Books", Severity.Success);
    }

    [Fact]
    public async Task ChooseLibrary_Should_AddTheBookToTheChosenLibrary()
    {
        var cut = RenderFound();

        await cut.Find("#book-library").ChangeAsync(new() { Value = _wishlist.Id.ToString() });
        await cut.Find(".found-actions .form-button-primary").ClickAsync(new());

        await _booksService.Received(1).Create(Arg.Is<CreateBookRequest>(r => r.LibraryId == _wishlist.Id && r.Rating == 0), Arg.Any<CancellationToken>());
        _navigation.Uri.Should().EndWith($"/libraries/{_wishlist.Id}");
    }

    [Fact]
    public async Task EditDetails_Should_OpenTheFormWithTheBookFound_AndBackShouldReturnToIt()
    {
        var cut = RenderFound();

        await cut.Find(".found-actions .form-button-text").ClickAsync(new());

        cut.Find("h1").TextContent.Should().Be("Add a book");
        cut.Find("#book-title").GetAttribute("value").Should().Be("Extremity");
        cut.Find("#book-isbn").GetAttribute("value").Should().Be(Isbn);
        await cut.Find("button[aria-label='Back']").ClickAsync(new());
        cut.Find("h1").TextContent.Should().Be("Book found");
    }

    [Fact]
    public async Task SaveAsync_Should_ShowTheErrorsUnderTheFields_AndTheirCount_WhenTheBookWasNotFound()
    {
        _searchService.SearchByIsbnAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Extremity with { Found = false, Title = "", Serie = "", Source = null });
        var cut = Render($"?isbn={Isbn}&libraryId={_readBooks.Id}");
        cut.Find(".form-bar-subtitle").TextContent.Should().Be("Not found online: fill in the details");

        await cut.Find("form").SubmitAsync();

        cut.Find("[role=alert]").TextContent.Should().Be("2 fields need your attention.");
        cut.Find("#book-title").GetAttribute("aria-invalid").Should().Be("true");
        cut.Find("#book-title-error").TextContent.Should().Be("Enter a title");
        cut.Find("label[for=book-serie]").TextContent.Should().Be("Series *");
        await _booksService.DidNotReceive().Create(Arg.Any<CreateBookRequest>(), Arg.Any<CancellationToken>());

        await TypeAsync(cut, "book-title", "Extremity");

        cut.Find("[role=alert]").TextContent.Should().Be("1 field needs your attention.");
        cut.Find("#book-title").HasAttribute("aria-invalid").Should().BeFalse();
    }

    [Fact]
    public async Task SaveAsync_Should_AddTheBookTypedByHand()
    {
        var cut = Render($"?libraryId={_readBooks.Id}");
        cut.Find(".form-bar-subtitle").TextContent.Should().Be("Adding to Read Books");

        await TypeAsync(cut, "book-title", "Tangente");
        await TypeAsync(cut, "book-serie", "Tangente");
        await TypeAsync(cut, "book-isbn", "978-2-917237-35-9");
        await cut.Find("#book-pages").ChangeAsync(new() { Value = "82" });
        await cut.Find("form").SubmitAsync();

        await _booksService.Received(1).Create(
            Arg.Is<CreateBookRequest>(r => r.Title == "Tangente" && r.Isbn == "978-2-917237-35-9" && r.NumberOfPages == 82 && r.Rating == 0),
            Arg.Any<CancellationToken>());
        await _searchService.DidNotReceive().SearchByIsbnAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SaveAsync_Should_AskForALibrary_WhenNoneIsChosen()
    {
        var cut = Render(string.Empty);
        cut.Find("#book-library option").TextContent.Should().Be("Choose a library");

        await cut.Find("form").SubmitAsync();

        cut.Find("#book-library").GetAttribute("aria-invalid").Should().Be("true");
        cut.Find("#book-library-error").TextContent.Should().Be("Choose a library");
    }

    [Fact]
    public async Task SaveAsync_Should_SayTheBookIsAlreadyThere_WhenItsIsbnIsKnown()
    {
        _booksService.Create(Arg.Any<CreateBookRequest>(), Arg.Any<CancellationToken>()).Returns(_ => Result<Book>.Failure(BooksError.Duplicate));
        var cut = RenderFound();

        await cut.Find(".found-actions .form-button-primary").ClickAsync(new());

        _snackbar.Received(1).Add("This book is already in your libraries", Severity.Error);
        cut.Find("h1").TextContent.Should().Be("Book found");
        cut.Find(".found-actions .form-button-primary").HasAttribute("disabled").Should().BeFalse();
    }

    [Fact]
    public void AddBookForm_Should_OpenTheForm_WhenTheCataloguesCannotBeSearched()
    {
        _searchService.SearchByIsbnAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).ThrowsAsync(new HttpRequestException("timeout"));

        var cut = Render($"?isbn=978-2-413-01540-6&libraryId={_readBooks.Id}");

        cut.Find("h1").TextContent.Should().Be("Add a book");
        cut.Find("#book-isbn").GetAttribute("value").Should().Be(Isbn);
        _snackbar.Received(1).Add("The catalogues could not be searched: fill in the details", Severity.Warning);
    }

    [Fact]
    public void AddBookForm_Should_SayTheLibrariesCouldNotBeLoaded()
    {
        _librariesService.FilterBy(Arg.Any<string?>(), Arg.Any<LibrariesColumn?>(), Arg.Any<SortOrder?>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(_ => Result<IPagedList<Library>>.Failure(LibrariesError.NotFound));

        Render(string.Empty);

        _snackbar.Received(1).Add("Unable to load your libraries", Severity.Error);
    }
}
