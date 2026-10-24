using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;
using Web.Extensions;
using Web.Models;
using Web.Services;

namespace Web.Components.SharedComponents;

// The actions of a book, from the covers of a library or of a series (#1220).
public partial class BookActionsSheet : IAsyncDisposable
{
    [Inject] private IBooksService BooksService { get; set; } = default!;
    [Inject] private IBookMoveWorkflow BookMoveWorkflow { get; set; } = default!;
    [Inject] private IDialogService DialogService { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;
    [Inject] private IJSRuntime JS { get; set; } = default!;
    [Inject] private ILogger<BookActionsSheet> Logger { get; set; } = default!;

    [Parameter, EditorRequired]
    public BookActionsTarget Book { get; set; } = default!;

    [Parameter]
    public bool Open { get; set; }

    // A digital book can also be read, searched for its ISBN and downloaded.
    [Parameter]
    public bool IsDigital { get; set; }

    [Parameter]
    public PopupAnchor? Anchor { get; set; }

    [Parameter]
    public bool AlignEnd { get; set; }

    [Parameter]
    public EventCallback OnClose { get; set; }

    // Starts choosing several books from this one; the action is left out without it.
    [Parameter]
    public EventCallback OnSelect { get; set; }

    // The book was moved to another library or deleted: the page shows its books again.
    [Parameter]
    public EventCallback OnBookGone { get; set; }

    private readonly CancellationTokenSource _disposalCts = new();
    private bool _isMoving;

    private async Task DownloadAsync()
    {
        await OnClose.InvokeAsync();
        await JS.InvokeVoidAsync("open", $"/api/books/{Book.Id}/download", "_blank");
    }

    internal async Task MoveAsync()
    {
        await OnClose.InvokeAsync();
        if (_isMoving)
        {
            return;
        }

        _isMoving = true;
        try
        {
            if (await BookMoveWorkflow.ChooseAndMoveAsync(Book.Id, _disposalCts.Token))
            {
                await OnBookGone.InvokeAsync();
            }
        }
        catch (OperationCanceledException)
        {
            // Page left while moving; nothing to update.
        }
        finally
        {
            _isMoving = false;
        }
    }

    private async Task DeleteAsync()
    {
        await OnClose.InvokeAsync();
        var bookId = Book.Id;
        var confirmed = await DialogService.ShowConfirmationAsync("Delete book", "This book will be deleted with its readings. This cannot be undone.", "Delete book");
        if (!confirmed)
        {
            return;
        }

        try
        {
            var result = await BooksService.Delete(bookId.ToString(), _disposalCts.Token);
            if (result.IsSuccess)
            {
                await OnBookGone.InvokeAsync();
            }
            else if (result.IsFailure)
            {
                Snackbar.Add("Failed to delete book", Severity.Error);
                Logger.LogError("Failed to delete book with ID {BookId}: {ErrorDescription}", bookId, result.Error!.Description);
            }
        }
        catch (OperationCanceledException)
        {
            // Page left while deleting; nothing to update.
        }
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA1816", Justification = "No finalizer; S3971 prohibits GC.SuppressFinalize in DisposeAsync.")]
    public async ValueTask DisposeAsync()
    {
        await _disposalCts.CancelAsync();
        _disposalCts.Dispose();
    }
}
