using System.Globalization;
using Application.Helpers;
using Application.Interfaces;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using Serilog;
using Web.Services;
using Web.Validators;

namespace Web.Components.Pages.Books;

public partial class ImportBookMetaFromWeb
{
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;
    [Inject] private IBooksService BooksService { get; set; } = default!;
    [Inject] private IBedethequeService BedethequeService { get; set; } = default!;
    [Inject] private IOpenLibraryService OpenLibraryService { get; set; } = default!;
    [Inject] private IGoogleBooksService GoogleBooksService { get; set; } = default!;
    [Inject] private IComicSearchService ComicSearchService { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;

    [Parameter]
    public string BookId { get; set; } = string.Empty;

    private BookUiDto? _currentBook;
    private BedethequeBookResult? _bedethequeResult;
    private OpenLibraryBookResult? _olResult;
    private GoogleBooksBookResult? _googleResult;
    private ParsedTitleInfo? _olParsed;
    private ParsedTitleInfo? _googleParsed;

    private bool _isLoading = true;
    private bool _loadError;
    private bool _isSaving;

    // Per-field selected source
    private BookSource _selectedTitle = BookSource.Current;
    private BookSource _selectedSerie = BookSource.Current;
    private BookSource _selectedVolumeNumber = BookSource.Current;
    private BookSource _selectedAuthors = BookSource.Current;
    private BookSource _selectedPublishers = BookSource.Current;
    private BookSource _selectedPublishDate = BookSource.Current;
    private BookSource _selectedNumberOfPages = BookSource.Current;
    private BookSource _selectedCover = BookSource.Current;

    private sealed record ParsedTitleInfo(string Title, string Serie, int VolumeNumber);

    protected override async Task OnInitializedAsync()
    {
        await LoadBookThenFetchAsync();
    }

    protected override async Task OnParametersSetAsync()
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
            _isLoading = false;
            StateHasChanged();
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

        // Start all requests concurrently, then await each independently so that
        // a failure in one provider does not prevent the other's result from being used.
        var bedethequeTask = BedethequeService.SearchByIsbnAsync(_currentBook.ISBN);
        var olTask = OpenLibraryService.SearchByIsbnAsync(_currentBook.ISBN);
        var googleTask = GoogleBooksService.SearchByIsbnAsync(_currentBook.ISBN);

        try
        {
            _bedethequeResult = await bedethequeTask;
            StateHasChanged();
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or OperationCanceledException)
        {
            Log.Error(ex, "Error fetching Bedetheque for ISBN {ISBN}", _currentBook.ISBN);
            Snackbar.Add("Could not fetch data from Bedetheque. You can still save with current values.", Severity.Warning);
        }

        try
        {
            _olResult = await olTask;
            if (_olResult.Found)
            {
                var (title, serie, volumeNumber) = ComicSearchService.ParseTitleInfo(_olResult.Title, _olResult.Subtitle);
                _olParsed = new ParsedTitleInfo(title, serie, volumeNumber);
            }
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or OperationCanceledException)
        {
            Log.Error(ex, "Error fetching OpenLibrary for ISBN {ISBN}", _currentBook.ISBN);
            Snackbar.Add("Could not fetch data from OpenLibrary. You can still save with current values.", Severity.Warning);
        }
        finally
        {
            StateHasChanged();
        }

        try
        {
            _googleResult = await googleTask;
            if (_googleResult.Found)
            {
                var (title, serie, volumeNumber) = ComicSearchService.ParseTitleInfo(_googleResult.Title, _googleResult.Subtitle);
                _googleParsed = new ParsedTitleInfo(title, serie, volumeNumber);
            }
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or OperationCanceledException)
        {
            Log.Error(ex, "Error fetching Google Books for ISBN {ISBN}", _currentBook.ISBN);
            Snackbar.Add("Could not fetch data from Google Books. You can still save with current values.", Severity.Warning);
        }
        finally
        {
            StateHasChanged();
        }
    }

    private string? GetBedethequeValue(string field)
    {
        if (_bedethequeResult is not { Found: true } r)
        {
            return null;
        }

        return field switch
        {
            BookFieldKeys.Title => NullIfEmpty(r.Title),
            BookFieldKeys.Serie => NullIfEmpty(r.Serie),
            BookFieldKeys.VolumeNumber => r.VolumeNumber.ToString(CultureInfo.InvariantCulture),
            BookFieldKeys.Authors => NullIfEmpty(string.Join(", ", r.Authors)),
            BookFieldKeys.Publishers => NullIfEmpty(string.Join(", ", r.Publishers)),
            BookFieldKeys.PublishDate => r.PublishDate?.ToString(PublishDateHelper.DisplayFormat, CultureInfo.InvariantCulture),
            BookFieldKeys.NumberOfPages => r.NumberOfPages?.ToString(CultureInfo.InvariantCulture),
            BookFieldKeys.Cover => r.CoverUrl?.ToString(),
            _ => null
        };
    }

    private string? GetOlValue(string field) => field switch
    {
        BookFieldKeys.Title => _olParsed?.Title,
        BookFieldKeys.Serie => _olParsed?.Serie,
        BookFieldKeys.VolumeNumber => _olParsed?.VolumeNumber.ToString(CultureInfo.InvariantCulture),
        BookFieldKeys.Authors => _olResult?.Found == true ? NullIfEmpty(string.Join(", ", _olResult.Authors)) : null,
        BookFieldKeys.Publishers => _olResult?.Found == true ? NullIfEmpty(string.Join(", ", _olResult.Publishers)) : null,
        BookFieldKeys.PublishDate => _olResult?.Found == true ? _olResult.PublishDate?.ToString(PublishDateHelper.DisplayFormat, CultureInfo.InvariantCulture) : null,
        BookFieldKeys.NumberOfPages => _olResult?.Found == true ? _olResult.NumberOfPages?.ToString(CultureInfo.InvariantCulture) : null,
        BookFieldKeys.Cover => _olResult?.CoverUrl?.ToString(),
        _ => null
    };

    private string? GetGoogleValue(string field) => field switch
    {
        BookFieldKeys.Title => _googleParsed?.Title,
        BookFieldKeys.Serie => _googleParsed?.Serie,
        BookFieldKeys.VolumeNumber => _googleParsed?.VolumeNumber.ToString(CultureInfo.InvariantCulture),
        BookFieldKeys.Authors => _googleResult?.Found == true ? NullIfEmpty(string.Join(", ", _googleResult.Authors)) : null,
        BookFieldKeys.Publishers => _googleResult?.Found == true ? NullIfEmpty(string.Join(", ", _googleResult.Publishers)) : null,
        BookFieldKeys.PublishDate => _googleResult?.Found == true ? _googleResult.PublishDate?.ToString(PublishDateHelper.DisplayFormat, CultureInfo.InvariantCulture) : null,
        BookFieldKeys.NumberOfPages => _googleResult?.Found == true ? _googleResult.NumberOfPages?.ToString(CultureInfo.InvariantCulture) : null,
        BookFieldKeys.Cover => _googleResult?.CoverUrl?.ToString(),
        _ => null
    };

    private string? GetResolvedValue(string field)
    {
        var source = field switch
        {
            BookFieldKeys.Title => _selectedTitle,
            BookFieldKeys.Serie => _selectedSerie,
            BookFieldKeys.VolumeNumber => _selectedVolumeNumber,
            BookFieldKeys.Authors => _selectedAuthors,
            BookFieldKeys.Publishers => _selectedPublishers,
            BookFieldKeys.PublishDate => _selectedPublishDate,
            BookFieldKeys.NumberOfPages => _selectedNumberOfPages,
            BookFieldKeys.Cover => _selectedCover,
            _ => BookSource.Current
        };

        return source switch
        {
            BookSource.Bedetheque => GetBedethequeValue(field),
            BookSource.OpenLibrary => GetOlValue(field),
            BookSource.Google => GetGoogleValue(field),
            _ => GetCurrentValue(field)
        };
    }

    private string? GetCurrentValue(string field) => field switch
    {
        BookFieldKeys.Title => _currentBook?.Title,
        BookFieldKeys.Serie => _currentBook?.Serie,
        BookFieldKeys.VolumeNumber => _currentBook?.VolumeNumber.ToString(CultureInfo.InvariantCulture),
        BookFieldKeys.Authors => _currentBook?.Authors,
        BookFieldKeys.Publishers => _currentBook?.Publishers,
        BookFieldKeys.PublishDate => _currentBook?.PublishDate?.ToString(PublishDateHelper.DisplayFormat, CultureInfo.InvariantCulture),
        BookFieldKeys.NumberOfPages => _currentBook?.NumberOfPages?.ToString(CultureInfo.InvariantCulture),
        BookFieldKeys.Cover => _currentBook?.ImageLink,
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

        var title = GetResolvedValue(BookFieldKeys.Title) ?? _currentBook.Title;
        var serie = GetResolvedValue(BookFieldKeys.Serie) ?? _currentBook.Serie;
        var authors = GetResolvedValue(BookFieldKeys.Authors) ?? _currentBook.Authors;
        var publishers = GetResolvedValue(BookFieldKeys.Publishers) ?? _currentBook.Publishers;
        var cover = GetResolvedValue(BookFieldKeys.Cover) ?? _currentBook.ImageLink;

        if (!string.IsNullOrEmpty(cover) &&
            !cover.Contains("res.cloudinary.com", StringComparison.OrdinalIgnoreCase) &&
            Uri.TryCreate(cover, UriKind.Absolute, out var coverUri) &&
            (coverUri.Scheme == Uri.UriSchemeHttp || coverUri.Scheme == Uri.UriSchemeHttps))
        {
            try
            {
                cover = await ComicSearchService.UploadCoverAsync(coverUri, _currentBook.ISBN);
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or OperationCanceledException)
            {
                Log.Error(ex, "Failed to upload cover to Cloudinary for book {BookId}, keeping original URL", BookId);
                Snackbar.Add("Cover could not be uploaded to Cloudinary. The original URL will be saved.", Severity.Warning);
            }
        }

        var volumeNumber = int.TryParse(GetResolvedValue(BookFieldKeys.VolumeNumber), out var vol)
            ? vol
            : _currentBook.VolumeNumber;

        var publishDate = _currentBook.PublishDate;
        var publishDateStr = GetResolvedValue(BookFieldKeys.PublishDate);
        if (!string.IsNullOrEmpty(publishDateStr) &&
            DateOnly.TryParseExact(publishDateStr, PublishDateHelper.DisplayFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var pd))
        {
            publishDate = pd;
        }

        var numberOfPages = _currentBook.NumberOfPages;
        var pagesStr = GetResolvedValue(BookFieldKeys.NumberOfPages);
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

public enum BookSource
{
    Current,
    Bedetheque,
    OpenLibrary,
    Google
}
