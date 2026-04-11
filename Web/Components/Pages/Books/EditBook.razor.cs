using Domain.Books;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using Serilog;
using Web.Extensions;
using Web.Services;
using Web.Validators;

namespace Web.Components.Pages.Books;

public partial class EditBook
{
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;
    [Inject] private IBooksService BooksService { get; set; } = default!;
    [Inject] private IDialogService DialogService { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;

    [Parameter]
    public string BookId { get; set; } = string.Empty;

    private BookForm? _bookForm;
    private BookUiDto? _bookModel;
    private Book? _book;
    private bool _isLoading = true;
    private bool _loadError;
    private bool _isSaving;

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
                _bookModel = BookUiDto.Convert(result.Value);
            }
            else
            {
                _loadError = true;
                Snackbar.Add("Book not found.", Severity.Error);
                Log.Error("Error loading book {BookId}", BookId);
            }
        }
        catch (OperationCanceledException ex)
        {
            _loadError = true;
            Snackbar.Add("The operation was cancelled.", Severity.Warning);
            Log.Warning(ex, "Loading book {BookId} was cancelled", BookId);
        }
        catch (InvalidOperationException ex)
        {
            _loadError = true;
            Snackbar.Add("An unexpected error occurred while loading the book.", Severity.Error);
            Log.Error(ex, "Unexpected error loading book {BookId}", BookId);
        }
        finally
        {
            _isLoading = false;
        }
    }

    private async Task SaveBookAsync()
    {
        if (_bookForm is null || _bookModel is null)
        {
            return;
        }

        var isValid = await _bookForm.ValidateAsync();
        if (!isValid)
        {
            return;
        }

        _isSaving = true;
        StateHasChanged();

        var request = new UpdateBookRequest(
            _bookModel.Id.ToString(),
            _bookModel.Serie,
            _bookModel.Title,
            _bookModel.ISBN ?? string.Empty,
            _bookModel.VolumeNumber,
            _bookModel.ImageLink,
            _bookModel.Authors,
            _bookModel.Publishers,
            _bookModel.PublishDate,
            _bookModel.NumberOfPages
        );

        var result = await BooksService.Update(request);

        if (result.IsSuccess)
        {
            NavigationManager.NavigateTo($"/books/{BookId}");
        }
        else
        {
            _isSaving = false;
            Snackbar.Add("Failed to update book", Severity.Error);
            Log.Error("Failed to update book: {Description}", result.Error?.Description);
            StateHasChanged();
        }

    }

    private async Task DeleteReadingDateAsync(Guid readingDateId)
    {
        try
        {
            var confirmed = await DialogService.ShowConfirmationAsync(
                "Confirm Deletion",
                "Are you sure you want to delete this reading?",
                "Delete");

            if (confirmed)
            {
                var res = await BooksService.DeleteReadingDate(BookId, readingDateId.ToString());

                if (res.IsSuccess)
                {
                    await LoadBookAsync();
                }
                else
                {
                    Snackbar.Add("An unexpected error occurred while deleting the reading.", Severity.Error);
                    Log.Error("An unexpected error occurred while deleting reading date {ReadingDateId} for book {BookId}: {Description}", readingDateId, BookId, res.Error?.Description);
                }
            }
        }
        catch (OperationCanceledException ex)
        {
            Snackbar.Add("The operation was cancelled.", Severity.Error);
            Log.Warning(ex, "Delete reading date {ReadingDateId} for book {BookId} was cancelled", readingDateId, BookId);
        }
        catch (InvalidOperationException ex)
        {
            Snackbar.Add("An unexpected error occurred while deleting the reading.", Severity.Error);
            Log.Error(ex, "Unexpected error deleting reading date {ReadingDateId} for book {BookId}", readingDateId, BookId);
        }
    }

    private void GoBack()
    {
        NavigationManager.NavigateTo($"/books/{BookId}");
    }
}
