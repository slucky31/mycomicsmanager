using System.Globalization;
using Application.Interfaces;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using Serilog;
using Web.Services;
using Web.Validators;

namespace Web.Components.Pages.Books;

public partial class ImportBook
{
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;
    [Inject] private IBooksService BooksService { get; set; } = default!;
    [Inject] private IOpenLibraryService OpenLibraryService { get; set; } = default!;
    [Inject] private IGoogleBooksService GoogleBooksService { get; set; } = default!;
    [Inject] private IComicSearchService ComicSearchService { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;

    [Parameter]
    public string BookId { get; set; } = string.Empty;

    private BookUiDto? _currentBook;
    private OpenLibraryBookResult? _olResult;
    private GoogleBooksBookResult? _googleResult;
    private ParsedTitleInfo? _olParsed;
    private ParsedTitleInfo? _googleParsed;

    private bool _isLoading = true;
    private bool _loadError;
    private bool _isSaving;

    // Per-field selected source ("current" | "openlibrary" | "google")
    private string _selectedTitle = "current";
    private string _selectedSerie = "current";
    private string _selectedVolumeNumber = "current";
    private string _selectedAuthors = "current";
    private string _selectedPublishers = "current";
    private string _selectedPublishDate = "current";
    private string _selectedNumberOfPages = "current";
    private string _selectedCover = "current";

    private record ParsedTitleInfo(string Title, string Serie, int VolumeNumber);

    protected override async Task OnInitializedAsync()
    {
        await LoadBookThenFetchAsync();
    }

    private async Task LoadBookThenFetchAsync()
    {
        _isLoading = true;
        _loadError = false;

        try
        {
            var result = await BooksService.GetById(BookId);

            if (!result.IsSuccess || result.Value is null)
            {
                _loadError = true;
                return;
            }

            _currentBook = BookUiDto.Convert(result.Value);
            await FetchWebServicesAsync();
        }
        catch (Exception ex) when (ex is OperationCanceledException or InvalidOperationException)
        {
            _loadError = true;
            Log.Error(ex, "Unexpected error loading book for import {BookId}", BookId);
        }      
        finally
        {
            _isLoading = false;
        }
    }

    private async Task FetchWebServicesAsync()
    {
        if (_currentBook is null || string.IsNullOrWhiteSpace(_currentBook.ISBN))
        {
            return;
        }

        try
        {
            var olTask = OpenLibraryService.SearchByIsbnAsync(_currentBook.ISBN);
            var googleTask = GoogleBooksService.SearchByIsbnAsync(_currentBook.ISBN);
            await Task.WhenAll(olTask, googleTask);

            _olResult = await olTask;
            _googleResult = await googleTask;

            if (_olResult.Found)
            {
                var (title, serie, volumeNumber) = ComicSearchService.ParseTitleInfo(_olResult.Title, _olResult.Subtitle);
                _olParsed = new ParsedTitleInfo(title, serie, volumeNumber);
            }

            if (_googleResult.Found)
            {
                var (title, serie, volumeNumber) = ComicSearchService.ParseTitleInfo(_googleResult.Title, _googleResult.Subtitle);
                _googleParsed = new ParsedTitleInfo(title, serie, volumeNumber);
            }
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or OperationCanceledException)
        {
            Log.Error(ex, "Error fetching web services for ISBN {ISBN}", _currentBook.ISBN);
        }
    }

    private string? GetOlValue(string field) => field switch
    {
        "title"         => _olParsed?.Title,
        "serie"         => _olParsed?.Serie,
        "volumeNumber"  => _olParsed?.VolumeNumber.ToString(CultureInfo.InvariantCulture),
        "authors"       => _olResult?.Found == true ? string.Join(", ", _olResult.Authors) : null,
        "publishers"    => _olResult?.Found == true ? string.Join(", ", _olResult.Publishers) : null,
        "publishDate"   => _olResult?.Found == true ? _olResult.PublishDate : null,
        "numberOfPages" => _olResult?.Found == true ? _olResult.NumberOfPages?.ToString(CultureInfo.InvariantCulture) : null,
        "cover"         => _olResult?.CoverUrl?.ToString(),
        _ => null
    };

    private string? GetGoogleValue(string field) => field switch
    {
        "title"         => _googleParsed?.Title,
        "serie"         => _googleParsed?.Serie,
        "volumeNumber"  => _googleParsed?.VolumeNumber.ToString(CultureInfo.InvariantCulture),
        "authors"       => _googleResult?.Found == true ? string.Join(", ", _googleResult.Authors) : null,
        "publishers"    => _googleResult?.Found == true ? string.Join(", ", _googleResult.Publishers) : null,
        "publishDate"   => _googleResult?.Found == true ? _googleResult.PublishDate : null,
        "numberOfPages" => _googleResult?.Found == true ? _googleResult.NumberOfPages?.ToString(CultureInfo.InvariantCulture) : null,
        "cover"         => _googleResult?.CoverUrl?.ToString(),
        _ => null
    };

    private string? GetResolvedValue(string field)
    {
        var source = field switch
        {
            "title"         => _selectedTitle,
            "serie"         => _selectedSerie,
            "volumeNumber"  => _selectedVolumeNumber,
            "authors"       => _selectedAuthors,
            "publishers"    => _selectedPublishers,
            "publishDate"   => _selectedPublishDate,
            "numberOfPages" => _selectedNumberOfPages,
            "cover"         => _selectedCover,
            _ => "current"
        };

        return source switch
        {
            "openlibrary" => GetOlValue(field),
            "google"      => GetGoogleValue(field),
            _             => GetCurrentValue(field)
        };
    }

    private string? GetCurrentValue(string field) => field switch
    {
        "title"         => _currentBook?.Title,
        "serie"         => _currentBook?.Serie,
        "volumeNumber"  => _currentBook?.VolumeNumber.ToString(CultureInfo.InvariantCulture),
        "authors"       => _currentBook?.Authors,
        "publishers"    => _currentBook?.Publishers,
        "publishDate"   => _currentBook?.PublishDate?.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture),
        "numberOfPages" => _currentBook?.NumberOfPages?.ToString(CultureInfo.InvariantCulture),
        "cover"         => _currentBook?.ImageLink,
        _ => null
    };

    private static string? NullIfEmpty(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value;

    private async Task ApplyAndSaveAsync()
    {
        if (_currentBook is null)
        {
            return;
        }

        _isSaving = true;
        StateHasChanged();

        var title = GetResolvedValue("title") ?? _currentBook.Title;
        var serie = GetResolvedValue("serie") ?? _currentBook.Serie;
        var authors = GetResolvedValue("authors") ?? _currentBook.Authors;
        var publishers = GetResolvedValue("publishers") ?? _currentBook.Publishers;
        var cover = GetResolvedValue("cover") ?? _currentBook.ImageLink;

        var volumeNumber = int.TryParse(GetResolvedValue("volumeNumber"), out var vol)
            ? vol
            : _currentBook.VolumeNumber;

       var publishDate = _currentBook.PublishDate;
        var publishDateStr = GetResolvedValue("publishDate");
        if (!string.IsNullOrEmpty(publishDateStr) &&
            DateOnly.TryParse(publishDateStr, out var pd))
        {
            publishDate = pd;
        }

        var numberOfPages = _currentBook.NumberOfPages;
        var pagesStr = GetResolvedValue("numberOfPages");
        if (!string.IsNullOrEmpty(pagesStr) && int.TryParse(pagesStr, out var pages))
        {
            numberOfPages = pages;
        }

        var request = new UpdateBookRequest(
            _currentBook.Id.ToString(),
            serie,
            title,
            _currentBook.ISBN,
            volumeNumber,
            cover,
            authors,
            publishers,
            publishDate,
            numberOfPages
        );

        var result = await BooksService.Update(request);

        if (result.IsSuccess)
        {
            NavigationManager.NavigateTo($"/books/{BookId}");
        }
        else
        {
            _isSaving = false;
            Snackbar.Add($"Failed to update book: {result.Error?.Description}", Severity.Error);
            StateHasChanged();
        }
    }

    private void Cancel() => NavigationManager.NavigateTo($"/books/{BookId}");
}
