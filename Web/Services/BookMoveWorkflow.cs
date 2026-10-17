using Application.Books.MoveTargets;
using Domain.Primitives;
using MudBlazor;
using Web.Components.Pages.Dialogs;
using Web.Extensions;
using Web.Models;

namespace Web.Services;

public class BookMoveWorkflow(
    IBookMoveService bookMoveService,
    IDialogService dialogService,
    ISnackbar snackbar,
    ILogger<BookMoveWorkflow> logger) : IBookMoveWorkflow
{
    public async Task<bool> ChooseAndMoveAsync(Guid bookId, CancellationToken cancellationToken = default)
    {
        var targets = await bookMoveService.GetTargetsAsync(bookId, cancellationToken);
        var targetLibraryId = await ChooseTargetLibraryAsync(targets, "Move to another library", bookId.ToString());
        if (targetLibraryId is null)
        {
            return false;
        }

        var result = await bookMoveService.MoveAsync(bookId, targetLibraryId.Value, cancellationToken);
        if (result.IsFailure)
        {
            snackbar.Add(result.Error!.Description ?? "Failed to move book", Severity.Error);
            logger.LogError("Failed to move book {BookId}: {Description}", bookId, result.Error.Description);
            return false;
        }

        snackbar.Add("Book moved", Severity.Success);
        return true;
    }

    public async Task<int> ChooseAndMoveAsync(IReadOnlyList<Guid> bookIds, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(bookIds);

        var targets = await bookMoveService.GetTargetsAsync(bookIds, cancellationToken);
        var targetLibraryId = await ChooseTargetLibraryAsync(targets, BookMoveText.Title(bookIds.Count), $"{bookIds.Count} books");
        if (targetLibraryId is null)
        {
            return 0;
        }

        var result = await bookMoveService.MoveAsync(bookIds, targetLibraryId.Value, cancellationToken);
        if (result.IsFailure)
        {
            snackbar.Add(result.Error!.Description ?? "Failed to move the books", Severity.Error);
            logger.LogError("Failed to move {Count} books: {Description}", bookIds.Count, result.Error.Description);
            return 0;
        }

        var moved = result.Value!;
        snackbar.Add(BookMoveText.Outcome(moved), BookMoveText.Severity(moved));
        return moved.Moved;
    }

    // Null when there is nowhere to go, the targets could not be loaded or the user cancelled.
    private async Task<Guid?> ChooseTargetLibraryAsync(Result<BookMoveTargets> targets, string title, string what)
    {
        if (targets.IsFailure)
        {
            snackbar.Add("Failed to load libraries", Severity.Error);
            logger.LogError("Failed to load move targets for {What}: {Description}", what, targets.Error!.Description);
            return null;
        }

        var choices = BookMoveChoices.From(targets.Value!);
        if (!choices.HasTargets)
        {
            snackbar.Add("No other library of the same type", Severity.Info);
            return null;
        }

        var parameters = new DialogParameters<MoveBookDialog> { { x => x.Choices, choices } };
        var dialog = await dialogService.ShowAsync<MoveBookDialog>(title, parameters, AppDialogs.Options);
        var choice = await dialog.Result;
        return choice is { Canceled: false, Data: Guid libraryId } ? libraryId : null;
    }
}
