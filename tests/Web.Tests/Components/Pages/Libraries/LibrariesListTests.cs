using Application.Libraries.Summaries;
using AwesomeAssertions;
using Bunit;
using Domain.Libraries;
using Domain.Primitives;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;
using NSubstitute;
using Web.Components.Pages.Dialogs;
using Web.Components.Pages.Libraries;
using Web.Infrastructure;
using Web.Models;
using Web.Services;
using Xunit;

namespace Web.Tests.Components.Pages.Libraries;

public sealed class LibrariesListTests : IAsyncDisposable
{
    private readonly BunitContext _ctx = new();
    private readonly ILibrariesService _librariesService = Substitute.For<ILibrariesService>();
    private readonly IDialogService _dialogService = Substitute.For<IDialogService>();
    private readonly ISnackbar _snackbar = Substitute.For<ISnackbar>();
    private readonly BunitJSModuleInterop _module;

    private readonly LibrarySummary _readBooks = new(Guid.CreateVersion7(), "Read Books", "#2E7D32", "Star", LibraryBookType.Physical, 24)
    {
        Covers = ["/covers/3.webp", "/covers/2.webp", "/covers/1.webp"]
    };
    private readonly LibrarySummary _toSort = new(Guid.CreateVersion7(), "À trier", "#4A5BC4", "Bookmark", LibraryBookType.Digital, 2);
    private readonly LibrarySummary _ebooks = new(Guid.CreateVersion7(), "MesEbooks", "#4A5BC4", "Bookmark", LibraryBookType.Digital, 0);

    public LibrariesListTests()
    {
        _ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        _module = _ctx.JSInterop.SetupModule(AppVersion.Versioned("./js/popupAnchor.js"));
        _module.Mode = JSRuntimeMode.Loose;
        _ctx.Services.AddMudServices();
        _ctx.Services.AddSingleton(_librariesService);
        _ctx.Services.AddSingleton(_dialogService);
        _ctx.Services.AddSingleton(_snackbar);
        _ctx.Services.AddScoped<PopupAnchorLocator>();
    }

    public async ValueTask DisposeAsync()
    {
        _snackbar.Dispose();
        await _ctx.DisposeAsync();
    }

    private void GivenLibraries(params LibrarySummary[] libraries) =>
        _librariesService.GetSummaries(Arg.Any<CancellationToken>()).Returns(Result<IReadOnlyList<LibrarySummary>>.Success(libraries));

    private void GivenConfirmation(bool confirmed)
    {
        var dialog = Substitute.For<IDialogReference>();
        dialog.Result.Returns(confirmed ? DialogResult.Ok(true) : DialogResult.Cancel());
        _dialogService.ShowAsync<ConfirmationDialog>(Arg.Any<string>(), Arg.Any<DialogParameters>(), Arg.Any<DialogOptions>()).Returns(dialog);
    }

    private IRenderedComponent<LibrariesList> Render(params LibrarySummary[] libraries)
    {
        GivenLibraries(libraries);
        return _ctx.Render<LibrariesList>();
    }

    private static async Task OpenActionsAsync(IRenderedComponent<LibrariesList> cut, LibrarySummary library) =>
        await cut.Find($"#library-actions-{library.Id}").ClickAsync(new());

    [Fact]
    public void Render_Should_ShowACardPerLibrary_WithTheTotalsInTheSubtitle()
    {
        var cut = Render(_readBooks, _toSort, _ebooks);

        cut.FindAll(".library-name").Select(n => n.TextContent).Should().Equal("Read Books", "À trier", "MesEbooks");
        cut.FindAll(".library-type").Select(n => n.TextContent).Should().Equal("Physical", "Digital", "Digital");
        cut.FindAll(".library-count").Select(n => n.TextContent).Should().Equal("24 books", "2 books", "No books yet");
        cut.Find(".libraries-bar-subtitle").TextContent.Should().Be("3 libraries · 26 books");
        cut.FindAll(".library-card-link").Select(l => l.GetAttribute("href")).Should().Equal(
            $"/libraries/{_readBooks.Id}", $"/libraries/{_toSort.Id}", $"/libraries/{_ebooks.Id}");
        cut.Find($"#library-actions-{_readBooks.Id}").GetAttribute("aria-label").Should().Be("More actions for Read Books");
    }

    [Fact]
    public void Render_Should_FanTheLatestCoversWithTheNewestInFront()
    {
        var cut = Render(_readBooks);

        // The newest cover is drawn last so that it sits on top of the others.
        cut.FindAll(".library-covers img").Select(i => (i.GetAttribute("src"), i.GetAttribute("class")))
            .Should().Equal(("/covers/1.webp", "library-cover library-cover-2"), ("/covers/2.webp", "library-cover library-cover-1"), ("/covers/3.webp", "library-cover library-cover-0"));
    }

    [Fact]
    public void Render_Should_ShowADashedSlotForAnEmptyLibrary_AndTheIconForBooksWithoutCovers()
    {
        var cut = Render(_toSort, _ebooks);

        var covers = cut.FindAll(".library-covers");
        covers[0].QuerySelector(".library-cover-none").Should().NotBeNull();
        covers[1].QuerySelector(".library-cover-empty").Should().NotBeNull();
        cut.FindAll(".library-covers img").Should().BeEmpty();
    }

    [Fact]
    public void Render_Should_InviteToCreateALibrary_WhenThereIsNone()
    {
        var cut = Render();

        cut.Find(".libraries-empty").TextContent.Should().Contain("No libraries yet");
        cut.FindAll(".libraries-bar-subtitle").Should().BeEmpty();
        cut.FindAll("a[href='/libraries/add']").Should().HaveCount(3);
    }

    [Fact]
    public void Render_Should_ReportTheError_WhenTheLibrariesCannotBeLoaded()
    {
        _librariesService.GetSummaries(Arg.Any<CancellationToken>()).Returns(Result<IReadOnlyList<LibrarySummary>>.Failure(LibrariesError.ValidationError));

        var cut = _ctx.Render<LibrariesList>();

        _snackbar.Received(1).Add(LibrariesList.Msg_LoadFailed, Severity.Error, Arg.Any<Action<SnackbarOptions>>(), Arg.Any<string>());
        cut.FindAll(".library-card").Should().BeEmpty();
    }

    [Fact]
    public async Task OpenActionsAsync_Should_ShowTheActionsOfAPhysicalLibrary_NextToItsButton()
    {
        var anchor = new PopupAnchor(700, 300, 744, 344, 1280, 800);
        _module.Setup<PopupAnchor?>("anchorOf", $"library-actions-{_readBooks.Id}").SetResult(anchor);
        var cut = Render(_readBooks, _toSort);

        await OpenActionsAsync(cut, _readBooks);

        var sheet = cut.Find("[role=dialog]");
        sheet.GetAttribute("aria-label").Should().Be("Actions for Read Books");
        sheet.GetAttribute("style").Should().Be(PopupPlacement.Style(anchor, alignEnd: true));
        sheet.QuerySelector(".library-actions-details")!.TextContent.Should().Be("Physical · 24 books");
        sheet.QuerySelectorAll(".action-sheet-item").Select(i => i.TextContent.Trim())
            .Should().Equal("Edit name, icon and color", "Delete library");
        sheet.QuerySelector("a.action-sheet-item")!.GetAttribute("href").Should().Be($"/libraries/{_readBooks.Id}/edit");
    }

    [Fact]
    public async Task OpenActionsAsync_Should_OfferTheImport_ForADigitalLibrary()
    {
        var cut = Render(_readBooks, _toSort);

        await OpenActionsAsync(cut, _toSort);

        var sheet = cut.Find("[role=dialog]");
        sheet.QuerySelectorAll(".action-sheet-item").Select(i => i.TextContent.Trim())
            .Should().Equal("Edit name, icon and color", "Import books", "Delete library");
        sheet.QuerySelector($"a[href='/import?libraryId={_toSort.Id}']").Should().NotBeNull();
    }

    [Fact]
    public async Task OpenActionsAsync_Should_CenterTheMenu_WhenTheButtonCannotBeLocated()
    {
        _module.Setup<PopupAnchor?>("anchorOf", $"library-actions-{_readBooks.Id}").SetException(new Microsoft.JSInterop.JSException("gone"));
        var cut = Render(_readBooks);

        await OpenActionsAsync(cut, _readBooks);

        cut.Find("[role=dialog]").GetAttribute("style").Should().BeNullOrEmpty();
    }

    [Fact]
    public async Task CloseActions_Should_HideTheActions()
    {
        var cut = Render(_readBooks);
        await OpenActionsAsync(cut, _readBooks);

        await cut.Find(".action-sheet-backdrop").ClickAsync(new());

        cut.FindAll("[role=dialog]").Should().BeEmpty();
    }

    [Fact]
    public async Task DeleteAsync_Should_DeleteTheLibraryAndReload_WhenTheUserConfirms()
    {
        GivenConfirmation(confirmed: true);
        _librariesService.Delete(_toSort.Id.ToString(), Arg.Any<CancellationToken>()).Returns(Result.Success());
        var cut = Render(_readBooks, _toSort);
        await OpenActionsAsync(cut, _toSort);
        GivenLibraries(_readBooks);

        await cut.FindAll(".action-sheet-item")[^1].ClickAsync(new());

        await _dialogService.Received(1).ShowAsync<ConfirmationDialog>("Delete library", Arg.Any<DialogParameters>(), Arg.Any<DialogOptions>());
        _snackbar.Received(1).Add(LibrariesList.Msg_LibCorrectlyDeleted, Severity.Success, Arg.Any<Action<SnackbarOptions>>(), Arg.Any<string>());
        cut.WaitForAssertion(() => cut.FindAll(".library-name").Select(n => n.TextContent).Should().Equal("Read Books"));
        cut.FindAll("[role=dialog]").Should().BeEmpty();
    }

    [Fact]
    public async Task DeleteAsync_Should_KeepTheLibrary_WhenTheUserCancels()
    {
        GivenConfirmation(confirmed: false);
        var cut = Render(_readBooks);
        await OpenActionsAsync(cut, _readBooks);

        await cut.FindAll(".action-sheet-item")[^1].ClickAsync(new());

        await _librariesService.DidNotReceive().Delete(Arg.Any<string?>(), Arg.Any<CancellationToken>());
        cut.FindAll(".library-card").Should().HaveCount(1);
    }

    [Fact]
    public async Task DeleteAsync_Should_ReportTheError_WhenTheLibraryCannotBeDeleted()
    {
        GivenConfirmation(confirmed: true);
        _librariesService.Delete(_readBooks.Id.ToString(), Arg.Any<CancellationToken>()).Returns(Result.Failure(LibrariesError.FolderNotDeleted));
        var cut = Render(_readBooks);
        await OpenActionsAsync(cut, _readBooks);

        await cut.FindAll(".action-sheet-item")[^1].ClickAsync(new());

        _snackbar.Received(1).Add(LibrariesList.Msg_DeleteFailed, Severity.Error, Arg.Any<Action<SnackbarOptions>>(), Arg.Any<string>());
        cut.FindAll(".library-card").Should().HaveCount(1);
    }
}
