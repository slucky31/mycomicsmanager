using AwesomeAssertions;
using Bunit;
using Domain.Books;
using Domain.Libraries;
using Domain.Primitives;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Web.Components.Pages.Books;
using Web.Components.Pages.Dialogs;
using Web.Infrastructure;
using Web.Models;
using Web.Services;
using Xunit;

namespace Web.Tests.Components.Pages.Books;

public sealed class BookDetailComponentTests : IAsyncDisposable
{
    private readonly BunitContext _ctx = new();
    private readonly IBooksService _booksService = Substitute.For<IBooksService>();
    private readonly ILibrariesService _librariesService = Substitute.For<ILibrariesService>();
    private readonly IBookMoveWorkflow _moveWorkflow = Substitute.For<IBookMoveWorkflow>();
    private readonly IDialogService _dialogService = Substitute.For<IDialogService>();
    private readonly ISnackbar _snackbar = Substitute.For<ISnackbar>();
    private readonly BunitJSModuleInterop _module;
    private readonly Library _library = Library.Create("Read Books", "#2E7D32", "Star", LibraryBookType.Physical, Guid.CreateVersion7()).Value!;

    public BookDetailComponentTests()
    {
        _ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        _module = _ctx.JSInterop.SetupModule(AppVersion.Versioned("./js/popupAnchor.js"));
        _ctx.Services.AddMudServices();
        _ctx.Services.AddSingleton(_booksService);
        _ctx.Services.AddSingleton(_librariesService);
        _ctx.Services.AddSingleton(_moveWorkflow);
        _ctx.Services.AddSingleton(_dialogService);
        _ctx.Services.AddSingleton(_snackbar);
        _ctx.Services.AddSingleton(Substitute.For<IIsbnScanService>());
        _ctx.Services.AddScoped<PopupAnchorLocator>();
        _librariesService.GetById(Arg.Any<string?>()).Returns(Result<Library>.Success(_library));
    }

    public async ValueTask DisposeAsync()
    {
        _snackbar.Dispose();
        await _ctx.DisposeAsync();
    }

    private static DigitalBook DigitalBook(long fileSize = 1) =>
        Domain.Books.DigitalBook.Create(new BookMetadata("Blacksad", "Âme rouge", null, 4, NumberOfPages: 56), Guid.CreateVersion7(), "/data/A/b.cbz", fileSize).Value!;

    private static PhysicalBook PaperBook() =>
        PhysicalBook.Create(new BookMetadata("Extremity", "Extremity", "9782413015406"), Guid.CreateVersion7()).Value!;

    private async Task<IRenderedComponent<BookDetail>> RenderAsync(Book book)
    {
        _booksService.GetById(book.Id.ToString()).Returns(Result<Book>.Success(book));
        var cut = _ctx.Render<BookDetail>(p => p.Add(c => c.BookId, book.Id.ToString()));
        await cut.WaitForAssertionAsync(() => cut.Find("h1").TextContent.Should().Be(book.Title));
        return cut;
    }

    private static Task ClickAsync(IRenderedComponent<BookDetail> cut, string selector, string text) =>
        cut.FindAll(selector).Single(e => e.TextContent.Contains(text, StringComparison.Ordinal)).ClickAsync(new());

    private string CurrentUri => _ctx.Services.GetRequiredService<NavigationManager>().Uri;

    private void ShouldShow(string message, Severity severity) =>
        _snackbar.Received(1).Add(message, severity, Arg.Any<Action<SnackbarOptions>>(), Arg.Any<string>());

    [Fact]
    public async Task LoadBookAsync_Should_ShowTheLoadError_WhenLoadingThrows()
    {
        _booksService.GetById(Arg.Any<string?>()).ThrowsAsync(new InvalidOperationException());

        var cut = _ctx.Render<BookDetail>(p => p.Add(c => c.BookId, Guid.CreateVersion7().ToString()));

        await cut.WaitForAssertionAsync(() => cut.Markup.Should().Contain("Book not found"));
        ShouldShow(BookDetail.Msg_LoadFailed, Severity.Error);
    }

    [Fact]
    public async Task LoadBookAsync_Should_ShowAnError_WhenTheBookCannotBeLoaded()
    {
        _booksService.GetById(Arg.Any<string?>()).Returns(Result<Book>.Failure(BooksError.NotFound));

        var cut = _ctx.Render<BookDetail>(p => p.Add(c => c.BookId, Guid.CreateVersion7().ToString()));

        await cut.WaitForAssertionAsync(() => cut.Markup.Should().Contain("Book not found"));
        ShouldShow(BookDetail.Msg_LoadFailed, Severity.Error);
    }

    [Fact]
    public async Task LoadBookAsync_Should_ShowTheDigitalBook_WithItsMainActionAndItsActions()
    {
        var book = DigitalBook();
        book.UpdateReadingProgress(27);

        var cut = await RenderAsync(book);

        cut.Find(".book-bar-trail a").TextContent.Should().Be("Read Books");
        cut.Find(".book-serie").TextContent.Should().Be("Blacksad · Volume 4");
        cut.Find(".book-format").TextContent.Should().Be("Digital · 56 pages");
        cut.Find("a.book-primary-button").GetAttribute("href").Should().Be($"/books/{book.Id}/read");
        cut.Find(".book-primary-text").TextContent.Should().Contain("Continue reading").And.Contain("Page 28 of 56");
        cut.Find(".book-progress > div").GetAttribute("style").Should().Contain("width: 50%");
        cut.FindAll(".book-action").Select(a => a.TextContent).Should().Equal("Import info", "Find ISBN", "CBZ", "Move");
        cut.Find("a.book-action[download]").GetAttribute("href").Should().Be($"/api/books/{book.Id}/download");
        cut.Find(".book-history-empty").TextContent.Should().Be("Not read yet");
    }

    [Fact]
    public async Task LoadBookAsync_Should_ShowThePaperBook_WithItsRatingAndReadings()
    {
        var book = PaperBook();
        book.AddReadingDate(new DateTime(2026, 8, 21, 0, 0, 0, DateTimeKind.Utc), 3);

        var cut = await RenderAsync(book);

        cut.Find(".book-serie").TextContent.Should().Be("Volume 1");
        cut.FindAll(".book-format").Should().BeEmpty();
        cut.Find(".book-status .book-stars").GetAttribute("aria-label").Should().Be("Rated 3 out of 5");
        cut.Find(".book-read").TextContent.Should().Be("Read");
        cut.Find("button.book-primary-button").TextContent.Should().Contain("Add a reading");
        cut.FindAll(".book-action").Select(a => a.TextContent).Should().Equal("Import info", "Move");
        cut.Find(".book-history li").TextContent.Should().Contain("21/08/2026");
        cut.Find(".book-isbn").TextContent.Should().Be("9782413015406");
    }

    [Fact]
    public async Task LoadLibraryNameAsync_Should_ShowLibrary_WhenTheLibraryCannotBeLoaded()
    {
        _librariesService.GetById(Arg.Any<string?>()).Returns(Result<Library>.Failure(LibrariesError.NotFound));

        var cut = await RenderAsync(PaperBook());

        cut.Find(".book-bar-trail a").TextContent.Should().Be("Library");
    }

    [Fact]
    public async Task AddReadingDateAsync_Should_SaveTheChosenRating_AndReloadTheBook()
    {
        var book = PaperBook();
        _booksService.AddReadingDate(book.Id.ToString(), 4, Arg.Any<CancellationToken>()).Returns(Result<ReadingDate>.Success(ReadingDate.Create(DateTime.UtcNow, 4, book.Id)));
        var cut = await RenderAsync(book);

        await cut.Find("button.book-primary-button").ClickAsync(new());
        cut.Find(".book-reading-save").HasAttribute("disabled").Should().BeTrue();
        await cut.Find("button[aria-label='4 stars']").ClickAsync(new());
        cut.Find("button[aria-label='4 stars']").GetAttribute("aria-pressed").Should().Be("true");
        cut.FindAll(".book-reading-stars .book-star-on").Should().HaveCount(4);
        await cut.Find(".book-reading-save").ClickAsync(new());

        await _booksService.Received(1).AddReadingDate(book.Id.ToString(), 4, Arg.Any<CancellationToken>());
        await _booksService.Received(2).GetById(book.Id.ToString());
        cut.FindAll(".book-reading-form").Should().BeEmpty();
    }

    [Fact]
    public async Task AddReadingDateAsync_Should_KeepTheFormAndShowAnError_WhenSavingFails()
    {
        var book = PaperBook();
        _booksService.AddReadingDate(book.Id.ToString(), 1, Arg.Any<CancellationToken>()).Returns(Result<ReadingDate>.Failure(BooksError.BadRequest));
        var cut = await RenderAsync(book);

        await ClickAsync(cut, "button.book-history-add", "Add a reading");
        await cut.Find("button[aria-label='1 star']").ClickAsync(new());
        await cut.Find(".book-reading-save").ClickAsync(new());

        ShouldShow("Unexpected error while adding reading date", Severity.Error);
        cut.FindAll(".book-reading-form").Should().ContainSingle();
    }

    [Fact]
    public async Task AddReadingDateAsync_Should_ShowAnError_WhenSavingThrows()
    {
        var book = PaperBook();
        _booksService.AddReadingDate(book.Id.ToString(), 2, Arg.Any<CancellationToken>()).ThrowsAsync(new InvalidOperationException());
        var cut = await RenderAsync(book);

        await ClickAsync(cut, "button.book-history-add", "Add a reading");
        await cut.Find("button[aria-label='2 stars']").ClickAsync(new());
        await cut.Find(".book-reading-save").ClickAsync(new());

        ShouldShow("Unexpected error while adding reading date", Severity.Error);
    }

    [Fact]
    public async Task CancelAddReading_Should_CloseTheForm()
    {
        var cut = await RenderAsync(PaperBook());

        await ClickAsync(cut, "button.book-history-add", "Add a reading");
        await cut.Find(".book-reading-cancel").ClickAsync(new());

        cut.FindAll(".book-reading-form").Should().BeEmpty();
        cut.FindAll("button.book-history-add").Should().ContainSingle();
    }

    [Fact]
    public async Task OpenActionsAsync_Should_OpenTheMenuNextToTheButton()
    {
        var book = DigitalBook();
        _module.Setup<PopupAnchor?>("anchorOf", "book-actions").SetResult(new PopupAnchor(1200, 10, 1244, 54, 1280, 800));
        var cut = await RenderAsync(book);

        await cut.Find("button[aria-label='More actions']").ClickAsync(new());

        var sheet = cut.Find("[role='dialog']");
        sheet.GetAttribute("aria-label").Should().Be("Actions for Âme rouge");
        sheet.ClassList.Should().NotContain("action-sheet-centered");
        cut.Find(".book-sheet-details").TextContent.Should().Be("Volume 4 · Digital");
        cut.FindAll(".action-sheet-item").Select(i => i.TextContent.Trim()).Should().Equal(
            "Edit", "Import info from the web", "Move to another library", "Delete");
        cut.FindAll(".action-sheet-item-danger").Single().TextContent.Trim().Should().Be("Delete");
    }

    [Fact]
    public async Task MoveFromActionsAsync_Should_CloseTheMenuAndReloadTheMovedBook()
    {
        var book = DigitalBook(52_428_800L);
        _moveWorkflow.ChooseAndMoveAsync(book.Id, Arg.Any<CancellationToken>()).Returns(true);
        var cut = await RenderAsync(book);

        await cut.Find("button[aria-label='More actions']").ClickAsync(new());
        await ClickAsync(cut, ".action-sheet-item", "Move to another library");

        await _moveWorkflow.Received(1).ChooseAndMoveAsync(book.Id, Arg.Any<CancellationToken>());
        await _booksService.Received(2).GetById(book.Id.ToString());
        cut.FindAll("[role='dialog']").Should().BeEmpty();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task DeleteFromActionsAsync_Should_DeleteTheBookAndGoBackToItsLibrary_WhenConfirmed(bool confirmed)
    {
        var book = PaperBook();
        _dialogService.ShowAsync<ConfirmationDialog>(Arg.Any<string>(), Arg.Any<DialogParameters>(), Arg.Any<DialogOptions>())
            .Returns(_ => DialogReturning(confirmed));
        _booksService.Delete(book.Id.ToString(), Arg.Any<CancellationToken>()).Returns(Result.Success());
        var cut = await RenderAsync(book);

        await cut.Find("button[aria-label='More actions']").ClickAsync(new());
        await ClickAsync(cut, ".action-sheet-item", "Delete");

        if (confirmed)
        {
            await _booksService.Received(1).Delete(book.Id.ToString(), Arg.Any<CancellationToken>());
            CurrentUri.Should().EndWith($"/libraries/{book.LibraryId}");
        }
        else
        {
            await _booksService.DidNotReceive().Delete(Arg.Any<string?>(), Arg.Any<CancellationToken>());
        }
    }

    [Fact]
    public async Task DeleteBookAsync_Should_ShowAnError_WhenTheDeletionFails()
    {
        var book = PaperBook();
        _dialogService.ShowAsync<ConfirmationDialog>(Arg.Any<string>(), Arg.Any<DialogParameters>(), Arg.Any<DialogOptions>())
            .Returns(_ => DialogReturning(true));
        _booksService.Delete(book.Id.ToString(), Arg.Any<CancellationToken>()).Returns(Result.Failure(BooksError.NotFound));
        var cut = await RenderAsync(book);

        await cut.Find("button[aria-label='More actions']").ClickAsync(new());
        await ClickAsync(cut, ".action-sheet-item", "Delete");

        ShouldShow("Failed to delete book", Severity.Error);
    }

    [Fact]
    public async Task FileSize_Should_BeShown_WhenTheBookIsDigital()
    {
        var cut = await RenderAsync(DigitalBook(52_428_800L));

        cut.Markup.Should().Contain("File size").And.Contain("50 MB");
    }

    private static IDialogReference DialogReturning(bool confirmed)
    {
        var dialog = Substitute.For<IDialogReference>();
        dialog.Result.Returns(confirmed ? DialogResult.Ok(true) : DialogResult.Cancel());
        return dialog;
    }
}
