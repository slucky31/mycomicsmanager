using Application.Helpers;
using Application.Interfaces;
using Domain.Books;
using Domain.Libraries;
using Domain.Primitives;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using Web.Models;
using Web.Services;
using Web.Validators;

namespace Web.Components.Pages.Books;

// The book found for an ISBN, added in one tap (A5, T7); the form when it was not found or to change its details (A6, T8).
public sealed partial class AddBookForm : IDisposable
{
    private enum AddBookStep
    {
        Found,
        Form
    }

    [Inject] private NavigationManager NavigationManager { get; set; } = default!;
    [Inject] private IBooksService BooksService { get; set; } = default!;
    [Inject] private ILibrariesService LibrariesService { get; set; } = default!;
    [Inject] private IComicSearchService ComicSearchService { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;
    [Inject] private ILogger<AddBookForm> Logger { get; set; } = default!;

    [SupplyParameterFromQuery]
    public string? Isbn { get; set; }

    [SupplyParameterFromQuery]
    public string? LibraryId { get; set; }

    private readonly AddBookValidator _validator = new();
    private readonly CancellationTokenSource _disposalCts = new();

    private BookUiDto _model = new();
    private List<LibraryUiDto> _libraries = [];
    private BookFoundViewModel? _found;
    private AddBookStep _step = AddBookStep.Form;
    private FormErrors _errors = FormErrors.None;
    private bool _submitted;
    private bool _isLoading;
    private bool _isSaving;

    private string ExitHref => AddBookLinks.Exit(_model.LibraryId == Guid.Empty ? AddBookLinks.ParseLibraryId(LibraryId) : _model.LibraryId);

    private string? LibraryName => _libraries.Find(library => library.Id == _model.LibraryId)?.Name;

    private string AddToLabel => LibraryName is { } name ? $"Add to {name}" : "Add book";

    private string PageTitleText => _step == AddBookStep.Found ? "Book found" : "Add a book";

    private string FormSubtitle => (_found, Isbn) switch
    {
        (not null, _) => "Check the details, then add the book",
        (null, { Length: > 0 }) => "Not found online: fill in the details",
        _ => LibraryName is { } name ? $"Adding to {name}" : "Fill in the details",
    };

    protected override async Task OnInitializedAsync()
    {
        await LoadLibrariesAsync();

        if (string.IsNullOrWhiteSpace(Isbn))
        {
            return;
        }

        _isLoading = true;
        try
        {
            await LookUpAsync(IsbnHelper.NormalizeIsbn(Isbn));
        }
        finally
        {
            _isLoading = false;
        }
    }

    // The physical libraries (a paper book cannot go to a digital one); the one of the page that was left is chosen.
    private async Task LoadLibrariesAsync()
    {
        var result = await LibrariesService.FilterBy("", LibrariesColumn.Name, SortOrder.Ascending, 1, 100, _disposalCts.Token);
        if (result.IsFailure || result.Value?.Items is null)
        {
            Logger.LogError("Unable to load the libraries: {Description}", result.Error?.Description);
            Snackbar.Add("Unable to load your libraries", Severity.Error);
            return;
        }

        _libraries = [.. result.Value.Items.Select(LibraryUiDto.Convert).Where(library => library.BookType == LibraryBookType.Physical)];
        var preselected = AddBookLinks.ParseLibraryId(LibraryId);
        var library = _libraries.Find(l => l.Id == preselected) ?? (_libraries.Count == 1 ? _libraries[0] : null);
        _model.LibraryId = library?.Id ?? Guid.Empty;
    }

    private async Task LookUpAsync(string isbn)
    {
        try
        {
            var result = await ComicSearchService.SearchByIsbnAsync(isbn, _disposalCts.Token);
            if (result.Found)
            {
                _found = BookFoundViewModel.From(result);
                _step = AddBookStep.Found;
                _model = new BookUiDto
                {
                    Title = result.Title,
                    Serie = result.Serie,
                    ISBN = result.Isbn,
                    VolumeNumber = result.VolumeNumber,
                    ImageLink = result.ImageUrl,
                    Authors = result.Authors,
                    Publishers = result.Publishers,
                    PublishDate = result.PublishDate,
                    NumberOfPages = result.NumberOfPages,
                    LibraryId = _model.LibraryId,
                };
                return;
            }
        }
        catch (Exception ex) when (ex is HttpRequestException or OperationCanceledException or InvalidOperationException)
        {
            Logger.LogError(ex, "Error searching for the book {Isbn}", isbn);
            Snackbar.Add("The catalogues could not be searched: fill in the details", Severity.Warning);
        }

        _model.ISBN = isbn;
    }

    private void ChooseLibrary(Guid libraryId)
    {
        _model.LibraryId = libraryId;
        Revalidate();
    }

    private void ChooseRating(int rating) => _model.Rating = rating;

    private void EditDetails() => _step = AddBookStep.Form;

    private void ShowFound()
    {
        _step = AddBookStep.Found;
        _errors = FormErrors.None;
        _submitted = false;
    }

    // Once the form was submitted, its errors follow what is typed.
    private void Revalidate()
    {
        if (_submitted)
        {
            _errors = FormErrors.From(_validator.Validate(_model));
        }
    }

    private async Task SaveAsync()
    {
        if (_isSaving)
        {
            return;
        }

        _submitted = true;
        _errors = FormErrors.From(await _validator.ValidateAsync(_model, _disposalCts.Token));
        if (_errors.Any)
        {
            // The book found misses something: the form shows what.
            _step = AddBookStep.Form;
            return;
        }

        _isSaving = true;
        try
        {
            await CreateAsync();
        }
        finally
        {
            _isSaving = false;
        }
    }

    private async Task CreateAsync()
    {
        var request = new CreateBookRequest(
            _model.Serie,
            _model.Title,
            _model.ISBN ?? string.Empty,
            _model.LibraryId,
            _model.VolumeNumber,
            _model.ImageLink,
            _model.Rating,
            _model.Authors,
            _model.Publishers,
            _model.PublishDate,
            _model.NumberOfPages);

        var result = await BooksService.Create(request, _disposalCts.Token);
        if (result.IsSuccess)
        {
            Snackbar.Add($"{_model.Title} was added to {LibraryName}", Severity.Success);
            NavigationManager.NavigateTo(AddBookLinks.Exit(_model.LibraryId));
            return;
        }

        Logger.LogError("Failed to add the book {Isbn}: {Description}", _model.ISBN, result.Error?.Description);
        Snackbar.Add(result.Error == BooksError.Duplicate ? "This book is already in your libraries" : "Unable to add the book", Severity.Error);
    }

    public void Dispose()
    {
        _disposalCts.Cancel();
        _disposalCts.Dispose();
    }
}
