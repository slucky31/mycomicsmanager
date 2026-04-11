using Application.Helpers;
using Application.Interfaces;
using Domain.Libraries;
using Domain.Primitives;
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
    [Inject] private ILibrariesService LibrariesService { get; set; } = default!;
    [Inject] private IComicSearchService ComicSearchService { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;

    [SupplyParameterFromQuery]
    public string? Isbn { get; set; }

    [SupplyParameterFromQuery]
    public string? LibraryId { get; set; }

    private BookForm? _bookForm;
    private BookUiDto _bookModel = new();
    private List<LibraryUiDto> _libraries = [];
    private ComicSearchResult? _searchResult;
    private bool _isLoading;
    private bool _isSaving;

    protected override async Task OnInitializedAsync()
    {
        // Load user's libraries (Physical + All only)
        var libResult = await LibrariesService.FilterBy(
            "", LibrariesColumn.Name, SortOrder.Ascending, 1, 100);

        if (libResult.IsSuccess && libResult.Value?.Items is not null)
        {
            _libraries = libResult.Value.Items
                .Select(LibraryUiDto.Convert)
                .Where(l => l.BookType == LibraryBookType.Physical)
                .ToList();

            // Pre-select library from query param if provided
            if (Guid.TryParse(LibraryId, out var preselectedId))
            {
                var match = _libraries.FirstOrDefault(l => l.Id == preselectedId);
                if (match is not null)
                {
                    _bookModel.LibraryId = match.Id;
                }
            }
        }

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
                        NumberOfPages = _searchResult.NumberOfPages,
                        LibraryId = _bookModel.LibraryId
                    };
                }
                else
                {
                    _bookModel = new BookUiDto
                    {
                        ISBN = cleanedIsbn,
                        VolumeNumber = 1,
                        LibraryId = _bookModel.LibraryId
                    };
                    Snackbar.Add("Book not found. Please fill in the details manually.", Severity.Warning);
                }
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or OperationCanceledException or InvalidOperationException)
            {
                _bookModel = new BookUiDto { ISBN = cleanedIsbn, VolumeNumber = 1, LibraryId = _bookModel.LibraryId };
                Snackbar.Add("Error searching for book", Severity.Error);
                Log.Error(ex, "Error searching for book");
            }

            _isLoading = false;
        }
        else
        {
            _bookModel = new BookUiDto { VolumeNumber = 1, LibraryId = _bookModel.LibraryId };
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
            _bookModel.ISBN ?? string.Empty,
            _bookModel.LibraryId,
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
            var selectedLibraryId = _bookModel.LibraryId.ToString();
            var redirect = !string.IsNullOrEmpty(selectedLibraryId)
                ? $"/libraries/{LibraryId}"
                : "/libraries/list";
            NavigationManager.NavigateTo(redirect);
        }
        else
        {
            Snackbar.Add($"Failed to add book", Severity.Error);
            Log.Error("Failed to add book: {Description}", result.Error?.Description);
        }

        _isSaving = false;

    }

    private void GoBack()
    {
        var selectedLibraryId = _bookModel.LibraryId.ToString() ?? LibraryId;
        var url = string.IsNullOrWhiteSpace(selectedLibraryId) ? "/libraries/list" : $"/libraries/{selectedLibraryId}";
        NavigationManager.NavigateTo(url);
    }
}
