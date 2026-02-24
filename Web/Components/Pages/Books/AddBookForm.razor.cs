using Application.Helpers;
using Application.Interfaces;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using Serilog;
using Web.Services;
using Web.Validators;

namespace Web.Components.Pages.Books;

public partial class AddBookForm
{
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;
    [Inject] private IBooksService BooksService { get; set; } = default!;
    [Inject] private IComicSearchService ComicSearchService { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;

    [SupplyParameterFromQuery]
    public string? Isbn { get; set; }

    private BookForm? _bookForm;
    private BookUiDto _bookModel = new();
    private ComicSearchResult? _searchResult;
    private bool _isLoading;
    private bool _isSaving;

    protected override async Task OnInitializedAsync()
    {
        if (!string.IsNullOrWhiteSpace(Isbn))
        {
            _isLoading = true;
            StateHasChanged();

            var cleanedIsbn = IsbnHelper.NormalizeIsbn(Isbn);

            try
            {
                _searchResult = await ComicSearchService.SearchByIsbnAsync(cleanedIsbn);

                if (_searchResult.Found)
                {
                    _bookModel = new BookUiDto
                    {
                        Title = _searchResult.Title,
                        Serie = _searchResult.Serie,
                        ISBN = _searchResult.Isbn,
                        VolumeNumber = _searchResult.VolumeNumber,
                        ImageLink = _searchResult.ImageUrl,
                        Rating = 0,
                        Authors = _searchResult.Authors,
                        Publishers = _searchResult.Publishers,
                        PublishDate = _searchResult.PublishDate,
                        NumberOfPages = _searchResult.NumberOfPages
                    };
                    Snackbar.Add($"Found: {_searchResult.Title}", Severity.Success);
                }
                else
                {
                    _bookModel = new BookUiDto
                    {
                        ISBN = cleanedIsbn,
                        VolumeNumber = 1
                    };
                    Snackbar.Add("Book not found. Please fill in the details manually.", Severity.Warning);
                }
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or OperationCanceledException or InvalidOperationException)
            {
                _bookModel = new BookUiDto { ISBN = cleanedIsbn, VolumeNumber = 1 };
                Snackbar.Add("Error searching for book", Severity.Error);
                Log.Error(ex, "Error searching for book");
            }

            _isLoading = false;
        }
        else
        {
            _bookModel = new BookUiDto { VolumeNumber = 1 };
        }
    }

    private async Task SaveBookAsync()
    {
        if (_bookForm is null)
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

        var request = new CreateBookRequest(
            _bookModel.Serie,
            _bookModel.Title,
            _bookModel.ISBN,
            _bookModel.VolumeNumber,
            _bookModel.ImageLink,
            _bookModel.Rating,
            _bookModel.Authors,
            _bookModel.Publishers,
            _bookModel.PublishDate,
            _bookModel.NumberOfPages
        );

        var result = await BooksService.Create(request);

        if (result.IsSuccess)
        {
            Snackbar.Add("Book added successfully!", Severity.Success);
            NavigationManager.NavigateTo("/books/list");
        }
        else
        {
            Snackbar.Add($"Failed to add book: {result.Error?.Description}", Severity.Error);
        }

        _isSaving = false;

    }

    private void GoBack()
    {
        NavigationManager.NavigateTo("/books/add");
    }
}
