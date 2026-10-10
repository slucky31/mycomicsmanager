using Application.Books.IsbnScan;
using Domain.Books;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using Web.Extensions;
using Web.Services;

namespace Web.Components.Pages.Books;

public partial class BookDetail
{
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;
    [Inject] private IBooksService BooksService { get; set; } = default!;
    [Inject] private IBookMoveWorkflow BookMoveWorkflow { get; set; } = default!;
    [Inject] private IDialogService DialogService { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;
    [Inject] private ILogger<BookDetail> Logger { get; set; } = default!;
    [Inject] private IIsbnScanService IsbnScanService { get; set; } = default!;

    [Parameter]
    public string BookId { get; set; } = string.Empty;

    private Book? _book;
    private bool _isLoading = true;
    private bool _loadError;
    private bool _showAddReading;
    private int _newRating = 1;
    private bool _isAddingReading;
    private bool _isMoving;
    private bool _isScanningIsbn;

    protected override async Task OnInitializedAsync()
    {
        await LoadBookAsync();
    }

    private async Task LoadBookAsync()
    {
        _isLoading = true;
        _loadError = false;
        StateHasChanged();

        try
        {
            var result = await BooksService.GetById(BookId);

            if (result.IsSuccess && result.Value is not null)
            {
                _book = result.Value;
            }
            else
            {
                _loadError = true;
            }
        }
        catch (Exception ex) when (ex is OperationCanceledException or InvalidOperationException)
        {
            _loadError = true;
            Logger.LogError(ex, "Unexpected error while loading book");
        }
        finally
        {
            _isLoading = false;
        }
    }

    private async Task AddReadingDateAsync()
    {
        _isAddingReading = true;
        StateHasChanged();

        try
        {
            var result = await BooksService.AddReadingDate(BookId, _newRating);

            if (result.IsSuccess)
            {
                _showAddReading = false;
                _newRating = 1;
                await LoadBookAsync();
            }
            else
            {
                Snackbar.Add($"Unexpected error while adding reading date", Severity.Error);
                Logger.LogError("Unexpected error while adding reading date");
            }
        }
        catch (Exception ex) when (ex is OperationCanceledException or InvalidOperationException)
        {
            Snackbar.Add($"Unexpected error while adding reading date", Severity.Error);
            Logger.LogError(ex, "Unexpected error while adding reading date");
        }
        finally
        {
            _isAddingReading = false;
        }
    }

    private void ImportFromWeb() => NavigationManager.NavigateTo($"/books/{BookId}/import");

    private void EditBook() => NavigationManager.NavigateTo($"/books/{BookId}/edit");

    private void ReadBook() => NavigationManager.NavigateTo($"/books/{BookId}/read");

    private IsbnScanState IsbnScanState => _book is DigitalBook digitalBook ? digitalBook.GetIsbnScanState() : IsbnScanState.HasIsbn;

    private int IsbnCandidateCount => _book is DigitalBook digitalBook ? digitalBook.IsbnCandidates.Count : 0;

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
                NavigationManager.NavigateTo($"/books/{BookId}/import");
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
        NavigationManager.NavigateTo($"/books/{BookId}/import?isbn={Uri.EscapeDataString(isbn)}");

    private void FindIsbn() => NavigationManager.NavigateTo($"/books/{BookId}/read?mode={BookReader.IsbnSearchMode}");

    private async Task DeleteBookAsync()
    {
        try
        {
            var confirmed = await DialogService.ShowConfirmationAsync(
                "Confirm Delete",
                "Do you really want to delete this book? This process cannot be undone.",
                "Delete");

            if (confirmed)
            {
                var res = await BooksService.Delete(BookId);

                if (res.IsSuccess)
                {
                    NavigationManager.NavigateTo(_book is not null ? $"/libraries/{_book.LibraryId}" : "/libraries/list");
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
            if (await BookMoveWorkflow.ChooseAndMoveAsync(_book.Id))
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
        NavigationManager.NavigateTo(_book is not null ? $"/libraries/{_book.LibraryId}" : "/libraries/list");
    }
}
