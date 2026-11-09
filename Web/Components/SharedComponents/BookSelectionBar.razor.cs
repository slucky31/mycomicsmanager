using Microsoft.AspNetCore.Components;
using MudBlazor;
using Web.Extensions;
using Web.Models;
using Web.Services;

namespace Web.Components.SharedComponents;

// The bar of the books being chosen in a library or a series: moves or deletes them at once (#1150, #1194).
public partial class BookSelectionBar : IAsyncDisposable
{
    [Inject] private IBooksService BooksService { get; set; } = default!;
    [Inject] private IBookMoveWorkflow BookMoveWorkflow { get; set; } = default!;
    [Inject] private IDialogService DialogService { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;
    [Inject] private ILogger<BookSelectionBar> Logger { get; set; } = default!;

    [Parameter, EditorRequired]
    public BookSelection Selection { get; set; } = BookSelection.None;

    // Where the books are chosen: the library or the series.
    [Parameter, EditorRequired]
    public string Subtitle { get; set; } = string.Empty;

    // The files of a digital library are deleted with the books.
    [Parameter]
    public bool IsDigital { get; set; }

    // Taller from the tablet on, like the bar of a library.
    [Parameter]
    public bool Responsive { get; set; }

    [Parameter]
    public EventCallback OnStop { get; set; }

    [Parameter]
    public EventCallback OnSelectAll { get; set; }

    // Some books were moved or deleted (how many): the page shows the books left, none checked.
    [Parameter]
    public EventCallback<int> OnBooksGone { get; set; }

    private readonly CancellationTokenSource _disposalCts = new();
    // Checked books being moved or deleted: neither can start again until it is done.
    private bool _isChangingBooks;

    private bool IsDisabled => Selection.Count == 0 || _isChangingBooks;

    private async Task MoveCheckedBooksAsync()
    {
        if (IsDisabled)
        {
            return;
        }

        _isChangingBooks = true;
        try
        {
            var moved = await BookMoveWorkflow.ChooseAndMoveAsync(Selection.BookIds, _disposalCts.Token);
            if (moved > 0)
            {
                await OnBooksGone.InvokeAsync(moved);
            }
        }
        catch (OperationCanceledException)
        {
            // Component disposed while moving; nothing to update.
        }
        finally
        {
            _isChangingBooks = false;
        }
    }

    private async Task DeleteCheckedBooksAsync()
    {
        if (IsDisabled)
        {
            return;
        }

        var bookIds = Selection.BookIds;
        var confirmed = await DialogService.ShowConfirmationAsync(
            BookDeleteText.Title(bookIds.Count), BookDeleteText.Confirmation(bookIds.Count, IsDigital), BookDeleteText.Title(bookIds.Count));
        if (!confirmed)
        {
            return;
        }

        _isChangingBooks = true;
        try
        {
            var result = await BooksService.Delete(bookIds, _disposalCts.Token);
            if (result.IsFailure)
            {
                Snackbar.Add(result.Error!.Description ?? "Failed to delete the books", Severity.Error);
                Logger.LogError("Failed to delete {Count} books: {Description}", bookIds.Count, result.Error.Description);
                return;
            }

            var deleted = result.Value!;
            Snackbar.Add(BookDeleteText.Outcome(deleted), BookDeleteText.Severity(deleted));
            if (deleted.Deleted > 0)
            {
                await OnBooksGone.InvokeAsync(deleted.Deleted);
            }
        }
        catch (OperationCanceledException)
        {
            // Component disposed while deleting; nothing to update.
        }
        finally
        {
            _isChangingBooks = false;
        }
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA1816", Justification = "No finalizer; S3971 prohibits GC.SuppressFinalize in DisposeAsync.")]
    public async ValueTask DisposeAsync()
    {
        await _disposalCts.CancelAsync();
        _disposalCts.Dispose();
    }
}
