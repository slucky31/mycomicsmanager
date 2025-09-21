using Domain.Books;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;
using Web.Services;
using Web.Validators;

namespace Web.Components.Pages.Books;

public partial class BooksList
{
    [Inject] private IBooksService BooksService { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;
    [Inject] private IJSRuntime _jsRuntime { get; set; } = default!;

    private bool _visible;
    private bool _deleteDialogVisible;
    private FormMode _formMode = FormMode.Create;
    private MudForm _form = default!;
    private BookUiDto _bookModel = new();
    private BookValidator _bookValidator = new();
    private Guid _bookToDeleteId;

    private List<Book> Books { get; set; } = [];

    private readonly DialogOptions _dialogOptions = new()
    {
        FullWidth = true,
        CloseButton = true,
        CloseOnEscapeKey = true
    };

    private readonly DialogOptions _deleteDialogOptions = new()
    {
        CloseButton = true,
        CloseOnEscapeKey = true
    };

    protected override async Task OnInitializedAsync()
    {
        await LoadBooks();
    }

    private async Task LoadBooks()
    {
        var result = await BooksService.GetAll();
        if (result.IsSuccess)
        {
            Books = result.Value;
            StateHasChanged();
        }
        else
        {
            Snackbar.Add("Failed to load books", Severity.Error);
        }
    }

    private async Task CreateOrEdit(FormMode formMode, Book? book)
    {
        _formMode = formMode;
        
        if (formMode == FormMode.Create)
        {
            _bookModel = new BookUiDto();
        }
        else if (book != null)
        {
            _bookModel = BookUiDto.Convert(book);
        }

        _visible = true;
        StateHasChanged();
    }

    private async Task Submit()
    {
        await _form.Validate();

        if (_form.IsValid)
        {
            if (_formMode == FormMode.Create)
            {
                var result = await BooksService.Create(
                    _bookModel.Series,
                    _bookModel.Title,
                    _bookModel.ISBN,
                    _bookModel.VolumeNumber,
                    _bookModel.ImageLink
                );

                if (result.IsSuccess)
                {
                    Snackbar.Add("Book created successfully", Severity.Success);
                    await LoadBooks();
                    Cancel();
                }
                else
                {
                    Snackbar.Add($"Failed to create book: {result.Error.Description}", Severity.Error);
                }
            }
            else
            {
                var result = await BooksService.Update(
                    _bookModel.Id!.ToString(),
                    _bookModel.Series,
                    _bookModel.Title,
                    _bookModel.ISBN,
                    _bookModel.VolumeNumber,
                    _bookModel.ImageLink
                );

                if (result.IsSuccess)
                {
                    Snackbar.Add("Book updated successfully", Severity.Success);
                    await LoadBooks();
                    Cancel();
                }
                else
                {
                    Snackbar.Add($"Failed to update book: {result.Error.Description}", Severity.Error);
                }
            }
        }
    }

    private void Cancel()
    {
        _visible = false;
        _bookModel = new BookUiDto();
        StateHasChanged();
    }

    private async Task OnClickDelete(Guid? bookId)
    {
        if (bookId.HasValue)
        {
            _bookToDeleteId = bookId.Value;
            _deleteDialogVisible = true;
            StateHasChanged();
        }
    }

    private async Task ConfirmDelete()
    {
        var result = await BooksService.Delete(_bookToDeleteId.ToString());
        
        if (result.IsSuccess)
        {
            Snackbar.Add("Book deleted successfully", Severity.Success);
            await LoadBooks();
        }
        else
        {
            Snackbar.Add($"Failed to delete book: {result.Error.Description}", Severity.Error);
        }

        CancelDelete();
    }

    private void CancelDelete()
    {
        _deleteDialogVisible = false;
        _bookToDeleteId = Guid.Empty;
        StateHasChanged();
    }

    private async Task ScanISBN()
    {
        try
        {
            // This would be implemented with a barcode scanner library
            // For now, we'll just show a placeholder message
            Snackbar.Add("ISBN scanning feature will be implemented next", Severity.Info);
            
            // TODO: Implement actual ISBN scanning using camera
            // This would typically involve:
            // 1. ZXing.Net.Maui or similar barcode scanning library
            // 2. Camera access permissions
            // 3. Barcode detection and ISBN validation
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Failed to scan ISBN: {ex.Message}", Severity.Error);
        }
    }
}

public enum FormMode
{
    Create,
    Edit
}