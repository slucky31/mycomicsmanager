using Application.Interfaces;
using AwesomeAssertions;
using Bunit;
using Domain.Books;
using Domain.Primitives;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;
using NSubstitute;
using Web.Components.Pages.Books;
using Web.Services;
using Xunit;

namespace Web.Tests.Components.Pages.Books;

public sealed class FindBookDetailsComponentTests : IAsyncDisposable
{
    private const string PageIsbn = "9782800112343";

    private readonly BunitContext _ctx = new();
    private readonly IBooksService _booksService = Substitute.For<IBooksService>();
    private readonly IBnfCatalogueService _bnfService = Substitute.For<IBnfCatalogueService>();
    private readonly IBedethequeService _bedethequeService = Substitute.For<IBedethequeService>();
    private readonly IOpenLibraryService _openLibraryService = Substitute.For<IOpenLibraryService>();
    private readonly IGoogleBooksService _googleBooksService = Substitute.For<IGoogleBooksService>();
    private readonly IComicSearchService _comicSearchService = Substitute.For<IComicSearchService>();
    private readonly Book _book = DigitalBook.Create(new BookMetadata("Blacksad", "Âme rouge", null), Guid.CreateVersion7(), "/data/A/b.cbz", 1).Value!;

    public FindBookDetailsComponentTests()
    {
        _ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        _ctx.Services.AddMudServices();
        _ctx.Services.AddSingleton(_booksService);
        _ctx.Services.AddSingleton(_bnfService);
        _ctx.Services.AddSingleton(_bedethequeService);
        _ctx.Services.AddSingleton(_openLibraryService);
        _ctx.Services.AddSingleton(_googleBooksService);
        _ctx.Services.AddSingleton(_comicSearchService);

        _booksService.GetById(_book.Id.ToString()).Returns(Result<Book>.Success(_book));
        _booksService.Update(Arg.Any<UpdateBookRequest>(), Arg.Any<CancellationToken>()).Returns(Result<Book>.Success(_book));
        _bedethequeService.IsEnabled.Returns(true);
        _bnfService.SearchByIsbnAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new BnfBookResult(string.Empty, null, [], [], null, null, null, Found: false));
        _bedethequeService.SearchByIsbnAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new BedethequeBookResult(string.Empty, string.Empty, 0, [], [], null, null, null, Found: false));
        _openLibraryService.SearchByIsbnAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new OpenLibraryBookResult(string.Empty, null, [], [], null, null, null, Found: false));
        _googleBooksService.SearchByIsbnAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new GoogleBooksBookResult(string.Empty, null, [], [], null, null, null, null, [], null, Found: false));
    }

    public async ValueTask DisposeAsync() => await _ctx.DisposeAsync();

    [Fact]
    public async Task ApplyAndSaveAsync_Should_SearchAndSaveTheIsbnReadOnThePage_WhenTheBookHasNone()
    {
        _ctx.Services.GetRequiredService<NavigationManager>().NavigateTo($"/books/{_book.Id}/find-details?isbn=978-2-8001-1234-3");
        var cut = _ctx.Render<FindBookDetails>(p => p.Add(c => c.BookId, _book.Id.ToString()));
        await cut.WaitForAssertionAsync(() => cut.Markup.Should().Contain(PageIsbn));

        await cut.FindAll("button").Single(b => b.TextContent.Contains("Apply", StringComparison.Ordinal)).ClickAsync(new());

        await _bedethequeService.Received().SearchByIsbnAsync(PageIsbn, Arg.Any<CancellationToken>());
        await _booksService.Received(1).Update(Arg.Is<UpdateBookRequest>(r => r.Isbn == PageIsbn), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task LoadBookThenFetchAsync_Should_IgnoreTheIsbnParameter_WhenItIsInvalid()
    {
        _ctx.Services.GetRequiredService<NavigationManager>().NavigateTo($"/books/{_book.Id}/find-details?isbn=9782800112344");
        var cut = _ctx.Render<FindBookDetails>(p => p.Add(c => c.BookId, _book.Id.ToString()));
        await cut.WaitForAssertionAsync(() => cut.Markup.Should().Contain("Âme rouge"));

        cut.Markup.Should().NotContain("read on the book's page");
        await _bedethequeService.DidNotReceive().SearchByIsbnAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task OnParametersSetAsync_Should_SearchEachServiceOnce_WhenThePageOpens()
    {
        _ctx.Services.GetRequiredService<NavigationManager>().NavigateTo($"/books/{_book.Id}/find-details?isbn={PageIsbn}");
        var cut = _ctx.Render<FindBookDetails>(p => p.Add(c => c.BookId, _book.Id.ToString()));
        await cut.WaitForAssertionAsync(() => cut.Markup.Should().Contain(PageIsbn));

        cut.Render();

        await _bnfService.Received(1).SearchByIsbnAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _bedethequeService.Received(1).SearchByIsbnAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _openLibraryService.Received(1).SearchByIsbnAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _googleBooksService.Received(1).SearchByIsbnAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ApplyAndSaveAsync_Should_SaveTheValuesPickedFromTheBnf()
    {
        _bnfService.SearchByIsbnAsync(PageIsbn, Arg.Any<CancellationToken>())
            .Returns(new BnfBookResult("Tangente", null, ["Céline Wagner"], ["des Ronds dans l'O"], new DateOnly(2012, 1, 1), 82, null, Found: true));
        _ctx.Services.GetRequiredService<NavigationManager>().NavigateTo($"/books/{_book.Id}/find-details?isbn={PageIsbn}");
        var cut = _ctx.Render<FindBookDetails>(p => p.Add(c => c.BookId, _book.Id.ToString()));
        await cut.WaitForAssertionAsync(() => cut.Markup.Should().Contain("Céline Wagner"));

        // The author radio of the BnF column.
        await cut.FindAll("input[type=radio]").Single(r => r.ParentElement!.ParentElement!.TextContent.Contains("Céline Wagner", StringComparison.Ordinal))
            .ClickAsync(new());
        await cut.FindAll("button").Single(b => b.TextContent.Contains("Apply", StringComparison.Ordinal)).ClickAsync(new());

        await _booksService.Received(1).Update(Arg.Is<UpdateBookRequest>(r => r.Authors == "Céline Wagner"), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task OnParametersSetAsync_Should_HideBedetheque_WhenItIsTurnedOff()
    {
        _bedethequeService.IsEnabled.Returns(false);
        var cut = _ctx.Render<FindBookDetails>(p => p.Add(c => c.BookId, _book.Id.ToString()));
        await cut.WaitForAssertionAsync(() => cut.Markup.Should().Contain("BNF"));

        cut.Markup.Should().NotContain("BEDETHEQUE");
        await _bedethequeService.DidNotReceive().SearchByIsbnAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task FetchWebServicesAsync_Should_ShowTheProgress_UntilEverySourceHasAnswered()
    {
        var google = new TaskCompletionSource<GoogleBooksBookResult>();
        _googleBooksService.SearchByIsbnAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(google.Task);
        _ctx.Services.GetRequiredService<NavigationManager>().NavigateTo($"/books/{_book.Id}/find-details?isbn={PageIsbn}");
        var cut = _ctx.Render<FindBookDetails>(p => p.Add(c => c.BookId, _book.Id.ToString()));

        await cut.WaitForAssertionAsync(() => cut.Markup.Should().Contain("Searching the sources… (3/4)"));
        cut.Markup.Should().Contain("GOOGLE BOOKS: searching…").And.Contain("BNF: book not found");

        google.SetResult(new GoogleBooksBookResult("Âme rouge", null, [], [], null, null, null, null, [], null, Found: true));

        await cut.WaitForAssertionAsync(() => cut.Markup.Should().Contain("Search complete"));
        cut.Markup.Should().Contain("GOOGLE BOOKS: book found");
    }

    [Fact]
    public async Task FetchWebServicesAsync_Should_ShowTheSourcesThatCouldNotBeSearched()
    {
        _bnfService.SearchByIsbnAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new BnfBookResult(string.Empty, null, [], [], null, null, null, Found: false, Failed: true));
        _openLibraryService.SearchByIsbnAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<OpenLibraryBookResult>(new HttpRequestException("Network unreachable")));
        _ctx.Services.GetRequiredService<NavigationManager>().NavigateTo($"/books/{_book.Id}/find-details?isbn={PageIsbn}");
        var cut = _ctx.Render<FindBookDetails>(p => p.Add(c => c.BookId, _book.Id.ToString()));

        await cut.WaitForAssertionAsync(() => cut.Markup.Should().Contain("Search complete"));
        cut.Markup.Should().Contain("BNF: could not be searched")
            .And.Contain("OPENLIBRARY: could not be searched")
            .And.Contain("GOOGLE BOOKS: book not found");
    }

    private const string UploadedCover = "https://res.cloudinary.com/demo/cover.jpg";

    // Every source knows the book, with values of its own.
    private void EverySourceFindsTheBook()
    {
        _comicSearchService.ParseTitleInfo(Arg.Any<string>(), Arg.Any<string?>())
            .Returns(call => (call.ArgAt<string>(0), "Parsed serie", 2));
        _bedethequeService.SearchByIsbnAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new BedethequeBookResult("Bedetheque title", "Bedetheque serie", 3, ["Bedetheque author"], ["Bedetheque publisher"],
                new DateOnly(2005, 3, 1), 56, new Uri("https://www.bedetheque.com/media/Couvertures/Couv_1.jpg"), Found: true));
        _openLibraryService.SearchByIsbnAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new OpenLibraryBookResult("OpenLibrary title", null, ["OpenLibrary author"], ["OpenLibrary publisher"],
                new DateOnly(2006, 4, 2), 57, new Uri("https://covers.openlibrary.org/b/id/1-L.jpg"), Found: true));
        _googleBooksService.SearchByIsbnAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new GoogleBooksBookResult("Google title", null, ["Google author"], ["Google publisher"],
                new DateOnly(2007, 5, 3), 58, new Uri("https://books.google.com/cover.jpg"), null, [], null, Found: true));
        _comicSearchService.UploadCoverAsync(Arg.Any<Uri>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(UploadedCover);
    }

    private async Task<IRenderedComponent<FindBookDetails>> RenderOnceSearchedAsync()
    {
        _ctx.Services.GetRequiredService<NavigationManager>().NavigateTo($"/books/{_book.Id}/find-details?isbn={PageIsbn}");
        var cut = _ctx.Render<FindBookDetails>(p => p.Add(c => c.BookId, _book.Id.ToString()));
        await cut.WaitForAssertionAsync(() => cut.Markup.Should().Contain("Search complete"));
        return cut;
    }

    // Picks the given source in every field.
    private static async Task PickSourceAsync(IRenderedComponent<FindBookDetails> cut, string source)
    {
        var radios = cut.FindAll("label.mud-radio")
            .Where(label => label.QuerySelector(".find-details-source-label")?.TextContent == source)
            .Select(label => label.QuerySelector("input[type=radio]")!)
            .ToList();
        radios.Should().HaveCount(8);
        foreach (var radio in radios)
        {
            await radio.ClickAsync(new());
        }
    }

    private static Task ApplyAsync(IRenderedComponent<FindBookDetails> cut) =>
        cut.FindAll("button").Single(b => b.TextContent.Contains("Apply", StringComparison.Ordinal)).ClickAsync(new());

    [Theory]
    [InlineData("BEDETHEQUE", "Bedetheque title", "Bedetheque serie", 3, "Bedetheque author", 2005, 56)]
    [InlineData("OPENLIBRARY", "OpenLibrary title", "Parsed serie", 2, "OpenLibrary author", 2006, 57)]
    [InlineData("GOOGLE BOOKS", "Google title", "Parsed serie", 2, "Google author", 2007, 58)]
    public async Task ApplyAndSaveAsync_Should_SaveEveryValueOfTheSourcePicked(
        string source, string title, string serie, int volume, string author, int year, int pages)
    {
        EverySourceFindsTheBook();
        var cut = await RenderOnceSearchedAsync();

        await PickSourceAsync(cut, source);
        await ApplyAsync(cut);

        await _booksService.Received(1).Update(Arg.Is<UpdateBookRequest>(r =>
            r.Title == title && r.Series == serie && r.VolumeNumber == volume && r.Authors == author
            && r.PublishDate!.Value.Year == year && r.NumberOfPages == pages && r.ImageLink == UploadedCover), Arg.Any<CancellationToken>());
        _ctx.Services.GetRequiredService<NavigationManager>().Uri.Should().EndWith($"/books/{_book.Id}");
    }

    [Fact]
    public async Task ApplyAndSaveAsync_Should_KeepTheCoverUrl_WhenTheCoverCannotBeUploaded()
    {
        EverySourceFindsTheBook();
        _comicSearchService.UploadCoverAsync(Arg.Any<Uri>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<string>(new HttpRequestException("Cloudinary unreachable")));
        var cut = await RenderOnceSearchedAsync();

        await PickSourceAsync(cut, "GOOGLE BOOKS");
        await ApplyAsync(cut);

        await _booksService.Received(1).Update(Arg.Is<UpdateBookRequest>(r => r.ImageLink == "https://books.google.com/cover.jpg"), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ApplyAndSaveAsync_Should_StayOnThePage_WhenTheBookCannotBeSaved()
    {
        _booksService.Update(Arg.Any<UpdateBookRequest>(), Arg.Any<CancellationToken>())
            .Returns(Result<Book>.Failure(BooksError.NotFound));
        var cut = await RenderOnceSearchedAsync();

        await ApplyAsync(cut);

        _ctx.Services.GetRequiredService<NavigationManager>().Uri.Should().EndWith($"/find-details?isbn={PageIsbn}");
        cut.FindAll("button").Single(b => b.TextContent.Contains("Apply", StringComparison.Ordinal)).HasAttribute("disabled").Should().BeFalse();
    }

    [Fact]
    public async Task LoadBookThenFetchAsync_Should_ShowBookNotFound_WhenTheBookDoesNotExist()
    {
        _booksService.GetById(_book.Id.ToString()).Returns(Result<Book>.Failure(BooksError.NotFound));

        var cut = _ctx.Render<FindBookDetails>(p => p.Add(c => c.BookId, _book.Id.ToString()));

        await cut.WaitForAssertionAsync(() => cut.Markup.Should().Contain("Book not found"));
        await _bnfService.DidNotReceive().SearchByIsbnAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Cancel_Should_GoBackToTheBook()
    {
        var cut = await RenderOnceSearchedAsync();

        await cut.FindAll("button").Single(b => b.TextContent.Trim() == "Cancel").ClickAsync(new());

        _ctx.Services.GetRequiredService<NavigationManager>().Uri.Should().EndWith($"/books/{_book.Id}");
        await _booksService.DidNotReceive().Update(Arg.Any<UpdateBookRequest>(), Arg.Any<CancellationToken>());
    }
}
