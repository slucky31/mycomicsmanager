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
    private bool _scannerVisible;
    private FormMode _formMode = FormMode.Create;
    private MudForm _form = default!;
    private BookUiDto _bookModel = new();
    private readonly BookValidator _bookValidator = new();
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
        if (result.IsSuccess && result.Value is not null)
        {
            Books = result.Value;
            StateHasChanged();
        }
        else
        {
            Snackbar.Add("Failed to load books", Severity.Error);
        }
    }

    private void CreateOrEdit(FormMode formMode, Book? book)
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
                    _bookModel.Serie,
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
                    Snackbar.Add($"Failed to create book: {result.Error?.Description}", Severity.Error);
                }
            }
            else
            {
             
                var result = await BooksService.Update(
                    _bookModel.Id.ToString(),
                    _bookModel.Serie,
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
                    Snackbar.Add($"Failed to update book: {result.Error?.Description}", Severity.Error);
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

    private void OnClickDelete(Guid? bookId)
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
            Snackbar.Add($"Failed to delete book: {result.Error?.Description}", Severity.Error);
        }

        CancelDelete();
    }

    private void CancelDelete()
    {
        _deleteDialogVisible = false;
        _bookToDeleteId = Guid.Empty;
        StateHasChanged();
    }

    private void ScanISBN()
    {
        _scannerVisible = true;
        StateHasChanged();
    }

    private void OnIsbnScanned(string isbn)
    {
        _bookModel.ISBN = isbn;
        _scannerVisible = false;
        Snackbar.Add($"ISBN scanned: {isbn}", Severity.Success);
        StateHasChanged();
    }
}

public enum FormMode
{
    Create,
    Edit
}
