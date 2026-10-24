using AwesomeAssertions;
using Bunit;
using Domain.Books;
using Domain.Primitives;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Web.Components.Pages.Dialogs;
using Web.Components.SharedComponents;
using Web.Models;
using Web.Services;
using Xunit;

namespace Web.Tests.Components.SharedComponents;

public sealed class BookActionsSheetTests : IAsyncDisposable
{
    private readonly BunitContext _ctx = new();
    private readonly IBooksService _booksService = Substitute.For<IBooksService>();
    private readonly IBookMoveWorkflow _workflow = Substitute.For<IBookMoveWorkflow>();
    private readonly IDialogService _dialogService = Substitute.For<IDialogService>();
    private readonly ISnackbar _snackbar = Substitute.For<ISnackbar>();
    private readonly BookActionsTarget _book = new(Guid.CreateVersion7(), "Âme rouge", "Blacksad · #4", "https://c/4.jpg");
    private int _closed;
    private int _gone;

    public BookActionsSheetTests()
    {
        _ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        _ctx.Services.AddMudServices();
        _ctx.Services.AddSingleton(_booksService);
        _ctx.Services.AddSingleton(_workflow);
        _ctx.Services.AddSingleton(_dialogService);
        _ctx.Services.AddSingleton(_snackbar);
    }

    public async ValueTask DisposeAsync()
    {
        _snackbar.Dispose();
        await _ctx.DisposeAsync();
    }

    private IRenderedComponent<BookActionsSheet> RenderSheet(bool isDigital = false) =>
        _ctx.Render<BookActionsSheet>(p => p
            .Add(c => c.Book, _book)
            .Add(c => c.Open, true)
            .Add(c => c.IsDigital, isDigital)
            .Add(c => c.OnClose, () => _closed++)
            .Add(c => c.OnBookGone, () => _gone++));

    private static Task ClickAsync(IRenderedComponent<BookActionsSheet> cut, string action) =>
        cut.Find($".action-sheet-item:contains('{action}')").ClickAsync(new());

    private void GivenConfirmation(bool confirmed)
    {
        var dialog = Substitute.For<IDialogReference>();
        dialog.Result.Returns(confirmed ? DialogResult.Ok(true) : DialogResult.Cancel());
        _dialogService.ShowAsync<ConfirmationDialog>(Arg.Any<string>(), Arg.Any<DialogParameters>(), Arg.Any<DialogOptions>()).Returns(dialog);
    }

    [Fact]
    public void BookActionsSheet_Should_ShowTheBook_AndLeaveOutTheSelection_WhenThePageCannotSelect()
    {
        var cut = RenderSheet(isDigital: true);

        cut.Find("[role=dialog]").GetAttribute("aria-label").Should().Be("Actions for Âme rouge");
        cut.Find(".book-actions-serie").TextContent.Should().Be("Blacksad · #4");
        cut.FindAll(".action-sheet-item").Select(i => i.TextContent.Trim())
            .Should().Equal("Read", "Edit", "Import info from the web", "Find ISBN", "Download CBZ", "Move to another library", "Delete");
    }

    [Fact]
    public async Task DownloadAsync_Should_CloseTheActions_AndDownloadTheArchive()
    {
        var cut = RenderSheet(isDigital: true);

        await ClickAsync(cut, "Download CBZ");

        _closed.Should().Be(1);
        _ctx.JSInterop.VerifyInvoke("open").Arguments.Should().Equal($"/api/books/{_book.Id}/download", "_blank");
    }

    [Theory]
    [InlineData(true, 1)]
    [InlineData(false, 0)]
    public async Task MoveAsync_Should_TellThePage_OnlyWhenTheBookWasMoved(bool moved, int gone)
    {
        _workflow.ChooseAndMoveAsync(_book.Id, Arg.Any<CancellationToken>()).Returns(moved);
        var cut = RenderSheet();

        await ClickAsync(cut, "Move to another library");

        await _workflow.Received(1).ChooseAndMoveAsync(_book.Id, Arg.Any<CancellationToken>());
        _closed.Should().Be(1);
        _gone.Should().Be(gone);
    }

    [Fact]
    public async Task MoveAsync_Should_IgnoreASecondRequest_WhenAMoveIsInProgress()
    {
        var pending = new TaskCompletionSource<bool>();
        _workflow.ChooseAndMoveAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(pending.Task);
        var cut = RenderSheet();

        var first = cut.InvokeAsync(cut.Instance.MoveAsync);
        await cut.InvokeAsync(cut.Instance.MoveAsync);
        pending.SetResult(true);
        await first;

        await _workflow.Received(1).ChooseAndMoveAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        _gone.Should().Be(1);
    }

    [Fact]
    public async Task MoveAsync_Should_SwallowCancellation_WhenThePageIsLeftDuringTheMove()
    {
        _workflow.ChooseAndMoveAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).ThrowsAsync(new OperationCanceledException());
        var cut = RenderSheet();

        var move = () => cut.InvokeAsync(cut.Instance.MoveAsync);

        await move.Should().NotThrowAsync();
        _gone.Should().Be(0);
    }

    [Fact]
    public async Task DeleteAsync_Should_DeleteTheBook_AndTellThePage_WhenTheUserConfirms()
    {
        GivenConfirmation(confirmed: true);
        _booksService.Delete(_book.Id.ToString(), Arg.Any<CancellationToken>()).Returns(Result.Success());
        var cut = RenderSheet();

        await ClickAsync(cut, "Delete");

        await _booksService.Received(1).Delete(_book.Id.ToString(), Arg.Any<CancellationToken>());
        _closed.Should().Be(1);
        _gone.Should().Be(1);
    }

    [Fact]
    public async Task DeleteAsync_Should_KeepTheBook_WhenTheUserCancels()
    {
        GivenConfirmation(confirmed: false);
        var cut = RenderSheet();

        await ClickAsync(cut, "Delete");

        await _booksService.DidNotReceive().Delete(Arg.Any<string?>(), Arg.Any<CancellationToken>());
        _gone.Should().Be(0);
    }

    [Fact]
    public async Task DeleteAsync_Should_ReportTheError_WhenTheBookCannotBeDeleted()
    {
        GivenConfirmation(confirmed: true);
        _booksService.Delete(_book.Id.ToString(), Arg.Any<CancellationToken>()).Returns(Result.Failure(BooksError.NotFound));
        var cut = RenderSheet();

        await ClickAsync(cut, "Delete");

        _snackbar.Received(1).Add("Failed to delete book", Severity.Error, Arg.Any<Action<SnackbarOptions>>(), Arg.Any<string>());
        _gone.Should().Be(0);
    }

    [Fact]
    public async Task DeleteAsync_Should_SwallowCancellation_WhenThePageIsLeftDuringTheDeletion()
    {
        GivenConfirmation(confirmed: true);
        _booksService.Delete(_book.Id.ToString(), Arg.Any<CancellationToken>()).ThrowsAsync(new OperationCanceledException());
        var cut = RenderSheet();

        var delete = () => ClickAsync(cut, "Delete");

        await delete.Should().NotThrowAsync();
        _gone.Should().Be(0);
    }
}
