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

public sealed class ImportBookMetaFromWebComponentTests : IAsyncDisposable
{
    private const string PageIsbn = "9782800112343";

    private readonly BunitContext _ctx = new();
    private readonly IBooksService _booksService = Substitute.For<IBooksService>();
    private readonly IBnfCatalogueService _bnfService = Substitute.For<IBnfCatalogueService>();
    private readonly IBedethequeService _bedethequeService = Substitute.For<IBedethequeService>();
    private readonly IOpenLibraryService _openLibraryService = Substitute.For<IOpenLibraryService>();
    private readonly IGoogleBooksService _googleBooksService = Substitute.For<IGoogleBooksService>();
    private readonly Book _book = DigitalBook.Create(new BookMetadata("Blacksad", "Âme rouge", null), Guid.CreateVersion7(), "/data/A/b.cbz", 1).Value!;

    public ImportBookMetaFromWebComponentTests()
    {
        _ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        _ctx.Services.AddMudServices();
        _ctx.Services.AddSingleton(_booksService);
        _ctx.Services.AddSingleton(_bnfService);
        _ctx.Services.AddSingleton(_bedethequeService);
        _ctx.Services.AddSingleton(_openLibraryService);
        _ctx.Services.AddSingleton(_googleBooksService);
        _ctx.Services.AddSingleton(Substitute.For<IComicSearchService>());

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
        _ctx.Services.GetRequiredService<NavigationManager>().NavigateTo($"/books/{_book.Id}/import?isbn=978-2-8001-1234-3");
        var cut = _ctx.Render<ImportBookMetaFromWeb>(p => p.Add(c => c.BookId, _book.Id.ToString()));
        await cut.WaitForAssertionAsync(() => cut.Markup.Should().Contain(PageIsbn));

        await cut.FindAll("button").Single(b => b.TextContent.Contains("Apply", StringComparison.Ordinal)).ClickAsync(new());

        await _bedethequeService.Received().SearchByIsbnAsync(PageIsbn, Arg.Any<CancellationToken>());
        await _booksService.Received(1).Update(Arg.Is<UpdateBookRequest>(r => r.Isbn == PageIsbn), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task LoadBookThenFetchAsync_Should_IgnoreTheIsbnParameter_WhenItIsInvalid()
    {
        _ctx.Services.GetRequiredService<NavigationManager>().NavigateTo($"/books/{_book.Id}/import?isbn=9782800112344");
        var cut = _ctx.Render<ImportBookMetaFromWeb>(p => p.Add(c => c.BookId, _book.Id.ToString()));
        await cut.WaitForAssertionAsync(() => cut.Markup.Should().Contain("Âme rouge"));

        cut.Markup.Should().NotContain("read on the book's page");
        await _bedethequeService.DidNotReceive().SearchByIsbnAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task OnParametersSetAsync_Should_SearchEachServiceOnce_WhenThePageOpens()
    {
        _ctx.Services.GetRequiredService<NavigationManager>().NavigateTo($"/books/{_book.Id}/import?isbn={PageIsbn}");
        var cut = _ctx.Render<ImportBookMetaFromWeb>(p => p.Add(c => c.BookId, _book.Id.ToString()));
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
        _ctx.Services.GetRequiredService<NavigationManager>().NavigateTo($"/books/{_book.Id}/import?isbn={PageIsbn}");
        var cut = _ctx.Render<ImportBookMetaFromWeb>(p => p.Add(c => c.BookId, _book.Id.ToString()));
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
        var cut = _ctx.Render<ImportBookMetaFromWeb>(p => p.Add(c => c.BookId, _book.Id.ToString()));
        await cut.WaitForAssertionAsync(() => cut.Markup.Should().Contain("BNF"));

        cut.Markup.Should().NotContain("BEDETHEQUE");
        await _bedethequeService.DidNotReceive().SearchByIsbnAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task FetchWebServicesAsync_Should_ShowTheProgress_UntilEverySourceHasAnswered()
    {
        var google = new TaskCompletionSource<GoogleBooksBookResult>();
        _googleBooksService.SearchByIsbnAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(google.Task);
        _ctx.Services.GetRequiredService<NavigationManager>().NavigateTo($"/books/{_book.Id}/import?isbn={PageIsbn}");
        var cut = _ctx.Render<ImportBookMetaFromWeb>(p => p.Add(c => c.BookId, _book.Id.ToString()));

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
        _ctx.Services.GetRequiredService<NavigationManager>().NavigateTo($"/books/{_book.Id}/import?isbn={PageIsbn}");
        var cut = _ctx.Render<ImportBookMetaFromWeb>(p => p.Add(c => c.BookId, _book.Id.ToString()));

        await cut.WaitForAssertionAsync(() => cut.Markup.Should().Contain("Search complete"));
        cut.Markup.Should().Contain("BNF: could not be searched")
            .And.Contain("OPENLIBRARY: could not be searched")
            .And.Contain("GOOGLE BOOKS: book not found");
    }
}
