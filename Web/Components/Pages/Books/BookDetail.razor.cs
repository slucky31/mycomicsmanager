using Application.Books.IsbnScan;
using Domain.Books;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using Web.Extensions;
using Web.Models;
using Web.Services;

namespace Web.Components.Pages.Books;

public sealed partial class BookDetail : IDisposable
{
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;
    [Inject] private IBooksService BooksService { get; set; } = default!;
    [Inject] private ILibrariesService LibrariesService { get; set; } = default!;
    [Inject] private IBookMoveWorkflow BookMoveWorkflow { get; set; } = default!;
    [Inject] private IDialogService DialogService { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;
    [Inject] private PopupAnchorLocator PopupAnchorLocator { get; set; } = default!;
    [Inject] private ILogger<BookDetail> Logger { get; set; } = default!;
    [Inject] private IIsbnScanService IsbnScanService { get; set; } = default!;
    [Inject] private IBookReaderService BookReaderService { get; set; } = default!;

    public static readonly string Msg_LoadFailed = "Unable to load the book";
    public static readonly string Msg_StartOverFailed = "Unable to reset the reading progress";

    private const string ActionsButtonId = "book-actions";

    [Parameter]
    public string BookId { get; set; } = string.Empty;

    // The screen the book was opened from (#1261): back goes there, and the edit pages keep it.
    [SupplyParameterFromQuery(Name = ReturnLinks.QueryName)]
    public string? From { get; set; }

    private readonly CancellationTokenSource _disposalCts = new();
    private Book? _book;
    private BookDetailViewModel? _view;
    private string? _libraryName;
    private bool _isLoading = true;
    private bool _showAddReading;
    private int _newRating;
    private bool _isAddingReading;
    private bool _isMoving;
    private bool _isScanningIsbn;
    private bool _isStartingOver;
    private bool _actionsOpen;
    private PopupAnchor? _sheetAnchor;

    private string HereHref => ReturnLinks.With($"/books/{BookId}", From);
    private string EditHref => ReturnLinks.With($"/books/{BookId}/edit", From);
    private string FindDetailsHref => ReturnLinks.With($"/books/{BookId}/find-details", From);

    // Leaving the reader comes back to this page, which still knows where it was opened from.
    private string ReadHref => ReturnLinks.With($"/books/{BookId}/read", HereHref);

    protected override async Task OnInitializedAsync()
    {
        await LoadBookAsync();
        _isLoading = false;
    }

    // On a failure the book already shown stays (after an action), or "Book not found" is shown.
    private async Task LoadBookAsync()
    {
        try
        {
            var result = await BooksService.GetById(BookId);
            if (result.IsSuccess && result.Value is not null)
            {
                var libraryChanged = _book?.LibraryId != result.Value.LibraryId;
                _book = result.Value;
                _view = BookDetailViewModel.From(result.Value);
                if (libraryChanged)
                {
                    await LoadLibraryNameAsync(result.Value.LibraryId);
                }
            }
            else if (result.IsFailure)
            {
                Logger.LogError("Unable to load the book {BookId}: {Error}", BookId, result.Error?.Description);
                Snackbar.Add(Msg_LoadFailed, Severity.Error);
            }
        }
        catch (Exception ex) when (ex is OperationCanceledException or InvalidOperationException)
        {
            Logger.LogError(ex, "Unexpected error while loading book {BookId}", BookId);
            Snackbar.Add(Msg_LoadFailed, Severity.Error);
        }
    }

    // The name of the library in the bar; "Library" when it cannot be loaded.
    private async Task LoadLibraryNameAsync(Guid libraryId)
    {
        var result = await LibrariesService.GetById(libraryId.ToString());
        if (result.IsSuccess && result.Value is not null)
        {
            _libraryName = result.Value.Name;
        }
        else
        {
            _libraryName = null;
            Logger.LogWarning("Unable to load the library {LibraryId} of book {BookId}: {Error}", libraryId, BookId, result.Error?.Description);
        }
    }

    // ── Reading history ────────────────────────────────────────────────

    private void ShowAddReading()
    {
        _newRating = 0;
        _showAddReading = true;
    }

    private void CancelAddReading() => _showAddReading = false;

    private async Task AddReadingDateAsync()
    {
        if (_newRating == 0 || _isAddingReading)
        {
            return;
        }

        _isAddingReading = true;
        try
        {
            var result = await BooksService.AddReadingDate(BookId, _newRating, _disposalCts.Token);
            if (result.IsSuccess)
            {
                _showAddReading = false;
                await LoadBookAsync();
            }
            else
            {
                Snackbar.Add("Unexpected error while adding reading date", Severity.Error);
                Logger.LogError("Unexpected error while adding reading date: {Error}", result.Error?.Description);
            }
        }
        catch (Exception ex) when (ex is OperationCanceledException or InvalidOperationException)
        {
            Snackbar.Add("Unexpected error while adding reading date", Severity.Error);
            Logger.LogError(ex, "Unexpected error while adding reading date");
        }
        finally
        {
            _isAddingReading = false;
        }
    }

    // The next reading starts again from the first page; the readings are kept.
    private async Task StartOverAsync()
    {
        if (_book is null || _isStartingOver)
        {
            return;
        }

        _isStartingOver = true;
        try
        {
            var result = await BookReaderService.SaveProgressAsync(_book.Id, 0, _disposalCts.Token);
            if (result.IsSuccess)
            {
                Snackbar.Add("Reading progress reset", Severity.Success);
                await LoadBookAsync();
            }
            else
            {
                Snackbar.Add(Msg_StartOverFailed, Severity.Error);
                Logger.LogError("Unable to reset the reading progress of book {BookId}: {Error}", BookId, result.Error?.Code);
            }
        }
        catch (Exception ex) when (ex is OperationCanceledException or InvalidOperationException)
        {
            Snackbar.Add(Msg_StartOverFailed, Severity.Error);
            Logger.LogError(ex, "Unexpected error while resetting the reading progress of book {BookId}", BookId);
        }
        finally
        {
            _isStartingOver = false;
        }
    }

    // ── ISBN ───────────────────────────────────────────────────────────

    // Reads the pages on the server right away (a few seconds), then shows what they gave.
    private async Task ScanIsbnAsync()
    {
        if (_book is null || _isScanningIsbn)
        {
            return;
        }

        _isScanningIsbn = true;
        try
        {
            var result = await IsbnScanService.ScanBookAsync(_book.Id, CancellationToken.None);
            if (result.IsFailure)
            {
                Snackbar.Add("Unable to scan the pages of this book", Severity.Error);
                Logger.LogError("Unable to scan the pages of book {BookId} for its ISBN: {Error}", BookId, result.Error?.Code);
                return;
            }

            await ShowIsbnScanOutcomeAsync(result.Value);
        }
        catch (Exception ex) when (ex is OperationCanceledException or InvalidOperationException)
        {
            Snackbar.Add("Unable to scan the pages of this book", Severity.Error);
            Logger.LogError(ex, "Unexpected error while scanning the pages of book {BookId} for its ISBN", BookId);
        }
        finally
        {
            _isScanningIsbn = false;
        }
    }

    private async Task ShowIsbnScanOutcomeAsync(IsbnScanOutcome outcome)
    {
        switch (outcome)
        {
            case IsbnScanOutcome.Assigned:
                // The book has its ISBN now: fetch its information right away.
                Snackbar.Add("ISBN found in the pages", Severity.Success);
                NavigationManager.NavigateTo(FindDetailsHref);
                return;
            case IsbnScanOutcome.WithCandidates:
                Snackbar.Add("Several ISBNs found in the pages: pick the book's own", Severity.Info);
                break;
            case IsbnScanOutcome.WithoutIsbn:
                Snackbar.Add("No ISBN found in the pages", Severity.Warning);
                break;
            default:
                Snackbar.Add("The pages could not be read", Severity.Error);
                return;
        }

        await LoadBookAsync();
    }

    private void UseIsbnCandidate(string isbn) =>
        NavigationManager.NavigateTo(ReturnLinks.With($"/books/{BookId}/find-details?isbn={Uri.EscapeDataString(isbn)}", From));

    private void FindIsbn() => NavigationManager.NavigateTo(ReturnLinks.With($"/books/{BookId}/read?mode={BookReader.IsbnSearchMode}", HereHref));

    // ── Actions ────────────────────────────────────────────────────────

    private async Task OpenActionsAsync()
    {
        var anchor = await PopupAnchorLocator.AnchorOfAsync(ActionsButtonId, _disposalCts.Token);
        _sheetAnchor = anchor;
        _actionsOpen = true;
    }

    private void CloseActions() => _actionsOpen = false;

    private Task MoveFromActionsAsync()
    {
        CloseActions();
        return MoveBookAsync();
    }

    private Task DeleteFromActionsAsync()
    {
        CloseActions();
        return DeleteBookAsync();
    }

    private async Task DeleteBookAsync()
    {
        try
        {
            var confirmed = await DialogService.ShowConfirmationAsync(
                "Delete book",
                $"“{_view?.Title}” will be deleted with its readings. This cannot be undone.",
                "Delete book");

            if (confirmed)
            {
                var res = await BooksService.Delete(BookId, _disposalCts.Token);

                if (res.IsSuccess)
                {
                    GoBack();
                }
                else
                {
                    Snackbar.Add("Failed to delete book", Severity.Error);
                    Logger.LogError("Failed to delete book: {Description}", res.Error!.Description);
                }
            }
        }
        catch (Exception ex) when (ex is OperationCanceledException or InvalidOperationException)
        {
            Snackbar.Add("Failed to delete book", Severity.Error);
            Logger.LogError(ex, "Unexpected error deleting book {BookId}", BookId);
        }
    }

    internal async Task MoveBookAsync()
    {
        if (_book is null || _isMoving)
        {
            return;
        }

        _isMoving = true;
        try
        {
            if (await BookMoveWorkflow.ChooseAndMoveAsync(_book.Id, _disposalCts.Token))
            {
                await LoadBookAsync();
            }
        }
        finally
        {
            _isMoving = false;
        }
    }

    private void GoBack()
    {
        NavigationManager.NavigateTo(ReturnLinks.Local(From) ?? (_book is not null ? $"/libraries/{_book.LibraryId}" : "/libraries/list"));
    }

    public void Dispose()
    {
        _disposalCts.Cancel();
        _disposalCts.Dispose();
    }
}
