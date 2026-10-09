using MudBlazor;
using Web.Components.Pages.Dialogs;
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
        var targetLibraryId = await ChooseTargetLibraryAsync(bookId, cancellationToken);
        if (targetLibraryId is null)
        {
            return false;
        }

        var result = await bookMoveService.MoveAsync(bookId, targetLibraryId.Value, cancellationToken);
        if (result.IsFailure)
        {
            snackbar.Add(result.Error!.Description ?? "Failed to move book", Severity.Error);
            logger.LogError("Failed to move book {BookId}: {Description}", bookId, result.Error!.Description);
            return false;
        }

        snackbar.Add("Book moved", Severity.Success);
        return true;
    }

    // Null when there is nowhere to go, the targets could not be loaded or the user cancelled.
    private async Task<Guid?> ChooseTargetLibraryAsync(Guid bookId, CancellationToken cancellationToken)
    {
        var targets = await bookMoveService.GetTargetsAsync(bookId, cancellationToken);
        if (targets.IsFailure)
        {
            snackbar.Add("Failed to load libraries", Severity.Error);
            logger.LogError("Failed to load move targets for book {BookId}: {Description}", bookId, targets.Error!.Description);
            return null;
        }

        var options = BookMoveTargetViewModel.From(targets.Value!);
        if (options.Count == 0)
        {
            snackbar.Add("No other library of the same type", Severity.Info);
            return null;
        }

        var parameters = new DialogParameters<MoveBookDialog> { { x => x.Targets, options } };
        var dialog = await dialogService.ShowAsync<MoveBookDialog>(
            "Move to another library", parameters, new DialogOptions { CloseOnEscapeKey = true, MaxWidth = MaxWidth.ExtraSmall, FullWidth = true });
        var choice = await dialog.Result;
        return choice is { Canceled: false, Data: Guid libraryId } ? libraryId : null;
    }
}
