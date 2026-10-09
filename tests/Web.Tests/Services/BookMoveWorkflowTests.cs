using Application.Books.MoveTargets;
using AwesomeAssertions;
using Domain.Books;
using Domain.Primitives;
using Microsoft.Extensions.Logging.Abstractions;
using MudBlazor;
using NSubstitute;
using Web.Components.Pages.Dialogs;
using Web.Services;
using Xunit;

namespace Web.Tests.Services;

public sealed class BookMoveWorkflowTests
{
    private static readonly Guid s_bookId = Guid.CreateVersion7();
    private static readonly Guid s_targetId = Guid.CreateVersion7();

    private readonly IBookMoveService _moveService = Substitute.For<IBookMoveService>();
    private readonly IDialogService _dialogs = Substitute.For<IDialogService>();
    private readonly ISnackbar _snackbar = Substitute.For<ISnackbar>();
    private readonly BookMoveWorkflow _workflow;

    public BookMoveWorkflowTests()
    {
        _workflow = new BookMoveWorkflow(_moveService, _dialogs, _snackbar, NullLogger<BookMoveWorkflow>.Instance);
    }

    private void SetupTargetsAndDialog(DialogResult dialogResult)
    {
        _moveService.GetTargetsAsync(s_bookId, Arg.Any<CancellationToken>())
            .Returns(new BookMoveTargets([new BookMoveTarget(s_targetId, "BD", "#111111", "Bookmark", 2)], s_targetId));
        var dialog = Substitute.For<IDialogReference>();
        dialog.Result.Returns(dialogResult);
        _dialogs.ShowAsync<MoveBookDialog>(Arg.Any<string?>(), Arg.Any<DialogParameters>(), Arg.Any<DialogOptions?>()).Returns(dialog);
    }

    [Fact]
    public async Task ChooseAndMoveAsync_Should_MoveToChosenLibrary_WhenUserConfirms()
    {
        SetupTargetsAndDialog(DialogResult.Ok(s_targetId));
        var book = DigitalBook.Create(new BookMetadata("Blacksad", "Âme rouge", null), s_targetId, "/data/A/b.cbz", 1).Value!;
        _moveService.MoveAsync(s_bookId, s_targetId, Arg.Any<CancellationToken>()).Returns(Result<Book>.Success(book));

        var moved = await _workflow.ChooseAndMoveAsync(s_bookId, TestContext.Current.CancellationToken);

        moved.Should().BeTrue();
        await _moveService.Received(1).MoveAsync(s_bookId, s_targetId, Arg.Any<CancellationToken>());
        _snackbar.Received(1).Add("Book moved", Severity.Success, Arg.Any<Action<SnackbarOptions>>(), Arg.Any<string>());
    }

    [Fact]
    public async Task ChooseAndMoveAsync_Should_NotMove_WhenUserCancels()
    {
        SetupTargetsAndDialog(DialogResult.Cancel());

        var moved = await _workflow.ChooseAndMoveAsync(s_bookId, TestContext.Current.CancellationToken);

        moved.Should().BeFalse();
        await _moveService.DidNotReceive().MoveAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ChooseAndMoveAsync_Should_NotOpenDialog_WhenNoOtherLibraryOfSameType()
    {
        _moveService.GetTargetsAsync(s_bookId, Arg.Any<CancellationToken>()).Returns(new BookMoveTargets([], null));

        var moved = await _workflow.ChooseAndMoveAsync(s_bookId, TestContext.Current.CancellationToken);

        moved.Should().BeFalse();
        await _dialogs.DidNotReceive().ShowAsync<MoveBookDialog>(Arg.Any<string?>(), Arg.Any<DialogParameters>(), Arg.Any<DialogOptions?>());
        _snackbar.Received(1).Add("No other library of the same type", Severity.Info, Arg.Any<Action<SnackbarOptions>>(), Arg.Any<string>());
        await _moveService.DidNotReceive().MoveAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ChooseAndMoveAsync_Should_ShowError_WhenMoveFails()
    {
        SetupTargetsAndDialog(DialogResult.Ok(s_targetId));
        _moveService.MoveAsync(s_bookId, s_targetId, Arg.Any<CancellationToken>()).Returns(Result<Book>.Failure(BooksError.FileAlreadyExists));

        var moved = await _workflow.ChooseAndMoveAsync(s_bookId, TestContext.Current.CancellationToken);

        moved.Should().BeFalse();
        _snackbar.Received(1).Add(BooksError.FileAlreadyExists.Description!, Severity.Error, Arg.Any<Action<SnackbarOptions>>(), Arg.Any<string>());
    }
}
