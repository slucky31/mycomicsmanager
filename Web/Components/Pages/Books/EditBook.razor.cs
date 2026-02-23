using Domain.Books;
using Microsoft.AspNetCore.Components;
using MudBlazor;
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
            }
        }
        catch (Exception ex)
        {
            _loadError = true;
            Snackbar.Add($"Error loading book: {ex.Message}", Severity.Error);
        }

        _isLoading = false;
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

        try
        {
            var request = new UpdateBookRequest(
                _bookModel.Id.ToString(),
                _bookModel.Serie,
                _bookModel.Title,
                _bookModel.ISBN,
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
                Snackbar.Add("Book updated successfully!", Severity.Success);
                NavigationManager.NavigateTo($"/books/{BookId}");
            }
            else
            {
                Snackbar.Add($"Failed to update book: {result.Error?.Description}", Severity.Error);
            }
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Unexpected error: {ex.Message}", Severity.Error);
        }
        finally
        {
            _isSaving = false;
            StateHasChanged();
        }
    }

    private async Task DeleteReadingDateAsync(Guid readingDateId)
    {
        var confirmed = await DialogService.ShowConfirmationAsync(
            "Confirmer la suppression",
            "Voulez-vous vraiment supprimer cette lecture ?",
            "Supprimer");

        if (confirmed)
        {
            var res = await BooksService.DeleteReadingDate(BookId, readingDateId.ToString());

            if (res.IsSuccess)
            {
                Snackbar.Add("Lecture supprimée.", Severity.Success);
                await LoadBookAsync();
            }
            else
            {
                Snackbar.Add($"Erreur : {res.Error?.Description}", Severity.Error);
            }
        }
    }

    private void GoBack()
    {
        NavigationManager.NavigateTo($"/books/{BookId}");
    }
}
