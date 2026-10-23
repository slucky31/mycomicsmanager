using System.Globalization;
using Application.Helpers;
using Application.Interfaces;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using Web.Models;
using Web.Services;
using Web.Validators;

namespace Web.Components.Pages.Books;

public partial class FindBookDetails
{
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;
    [Inject] private IBooksService BooksService { get; set; } = default!;
    [Inject] private IBnfCatalogueService BnfCatalogueService { get; set; } = default!;
    [Inject] private IBedethequeService BedethequeService { get; set; } = default!;
    [Inject] private IBubbleService BubbleService { get; set; } = default!;
    [Inject] private IOpenLibraryService OpenLibraryService { get; set; } = default!;
    [Inject] private IGoogleBooksService GoogleBooksService { get; set; } = default!;
    [Inject] private IComicSearchService ComicSearchService { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;
    [Inject] private ILogger<FindBookDetails> Logger { get; set; } = default!;

    [Parameter]
    public string BookId { get; set; } = string.Empty;

    // An ISBN read on a page of the book (reader): the search uses it, and saving stores it on the book.
    [SupplyParameterFromQuery(Name = "isbn")]
    public string? Isbn { get; set; }

    private BookUiDto? _currentBook;
    private string? _isbn;
    private bool _isbnFromPage;
    private BnfBookResult? _bnfResult;
    private BedethequeBookResult? _bedethequeResult;
    private BubbleBookResult? _bubbleResult;
    private OpenLibraryBookResult? _olResult;
    private GoogleBooksBookResult? _googleResult;
    private ParsedTitleInfo? _bnfParsed;
    private ParsedTitleInfo? _olParsed;
    private ParsedTitleInfo? _googleParsed;

    // The state of each web search, in the order the sources are shown.
    private readonly SortedDictionary<BookSource, ProviderSearchState> _searchStates = new();

    private bool _isLoading = true;
    private bool _loadError;
    private bool _isSaving;

    // The source picked for each field; a field left out keeps its current value.
    private readonly Dictionary<string, BookSource> _selected = new(StringComparer.Ordinal);
    private BookDetailsComparison _comparison = new([]);
    private int _changeCount;

    private sealed record ParsedTitleInfo(string Title, string Serie, int VolumeNumber);

    // The book and ISBN the page last searched for: each search costs SerpApi and Google quota.
    private (string BookId, string? Isbn)? _searchedFor;

    // Runs after the first initialization too, so the page is loaded once per book.
    protected override async Task OnParametersSetAsync()
    {
        if (_searchedFor == (BookId, Isbn))
        {
            return;
        }

        _searchedFor = (BookId, Isbn);
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
            ResolveIsbn(_currentBook.ISBN);
            RefreshComparison();
            _isLoading = false;
            StateHasChanged();
            await FetchWebServicesAsync();
        }
        catch (Exception ex) when (ex is OperationCanceledException or InvalidOperationException)
        {
            _loadError = true;
            Logger.LogError(ex, "Unexpected error loading book for import {BookId}", BookId);
        }
        finally
        {
            _isLoading = false;
        }
    }

    private void ResolveIsbn(string? currentIsbn)
    {
        var isbnFromPage = !string.IsNullOrWhiteSpace(Isbn) && IsbnHelper.IsValidISBN(Isbn)
            ? IsbnHelper.NormalizeIsbn(Isbn)
            : null;

        _isbn = isbnFromPage ?? currentIsbn;
        _isbnFromPage = isbnFromPage is not null && !string.Equals(isbnFromPage, currentIsbn, StringComparison.Ordinal);
    }

    private async Task FetchWebServicesAsync()
    {
        if (_currentBook is null || string.IsNullOrWhiteSpace(_isbn))
        {
            return;
        }

        // Start all requests concurrently, then apply each result as soon as it comes, so that
        // a slow or failing provider neither delays nor prevents the others.
        var isbn = _isbn;
        _searchStates.Clear();
        foreach (var source in WebSources())
        {
            _searchStates[source] = ProviderSearchState.Searching;
        }

        List<Task> searches = [.. WebSources().Select(source => SearchSourceAsync(source, isbn))];

        StateHasChanged();
        await Task.WhenAll(searches);
    }

    // Starts the search of one source; its result is applied as soon as it comes.
    private Task SearchSourceAsync(BookSource source, string isbn) => source switch
    {
        BookSource.Bnf => SearchProviderAsync(source, "the BnF catalogue", BnfCatalogueService.SearchByIsbnAsync(isbn), result =>
        {
            _bnfResult = result;
            _bnfParsed = ParseTitle(result);
            return ProviderSearchStates.Of(result);
        }),
        BookSource.OpenLibrary => SearchProviderAsync(source, "OpenLibrary", OpenLibraryService.SearchByIsbnAsync(isbn), result =>
        {
            _olResult = result;
            _olParsed = ParseTitle(result);
            return ProviderSearchStates.Of(result);
        }),
        BookSource.Google => SearchProviderAsync(source, "Google Books", GoogleBooksService.SearchByIsbnAsync(isbn), result =>
        {
            _googleResult = result;
            _googleParsed = ParseTitle(result);
            return ProviderSearchStates.Of(result);
        }),
        BookSource.Bedetheque => SearchProviderAsync(source, "Bedetheque", BedethequeService.SearchByIsbnAsync(isbn), result =>
        {
            _bedethequeResult = result;
            return ProviderSearchStates.Of(result);
        }),
        BookSource.Bubble => SearchProviderAsync(source, "Bubble", BubbleService.SearchByIsbnAsync(isbn), result =>
        {
            _bubbleResult = result;
            return ProviderSearchStates.Of(result);
        }),
        _ => Task.CompletedTask,
    };

    private async Task SearchProviderAsync<T>(BookSource source, string provider, Task<T> search, Func<T?, ProviderSearchState> apply)
        where T : class
    {
        var searchedFor = _searchedFor;

        var result = await AwaitProviderAsync(search, provider);

        // Another book or ISBN was opened meanwhile: this result is not for the page shown.
        if (_searchedFor != searchedFor)
        {
            return;
        }

        _searchStates[source] = apply(result);
        RefreshComparison();
        StateHasChanged();
    }

    // Each source can be turned off in Settings → Features.
    private IEnumerable<BookSource> WebSources() =>
        new[] { BookSource.Bnf, BookSource.Bedetheque, BookSource.Bubble, BookSource.OpenLibrary, BookSource.Google }
            .Where(source => source switch
            {
                BookSource.Bnf => BnfCatalogueService.IsEnabled,
                BookSource.Bedetheque => BedethequeService.IsEnabled,
                BookSource.Bubble => BubbleService.IsEnabled,
                BookSource.OpenLibrary => OpenLibraryService.IsEnabled,
                BookSource.Google => GoogleBooksService.IsEnabled,
                _ => false,
            });

    private bool IsSearching => _searchStates.ContainsValue(ProviderSearchState.Searching);

    private int SearchesDone => _searchStates.Values.Count(state => state != ProviderSearchState.Searching);

    // Every source answered and none of them knows the book.
    private bool NoSourceFound => _searchStates.Count > 0 && !IsSearching && !_searchStates.ContainsValue(ProviderSearchState.Found);

    private void RefreshComparison()
    {
        var fields = BookFieldDefinition.For(_currentBook is { IsPhysical: false }, !string.IsNullOrWhiteSpace(_currentBook?.ImageLink));
        _comparison = new BookDetailsComparison(fields
            .Select(field => BookFieldComparison.Compare(
                field,
                Comparable(field.Key, GetCurrentValue(field.Key)),
                WebSources().Select(source => (source, Comparable(field.Key, GetSourceValue(source, field.Key))))))
            .ToList());
        CountChanges();
    }

    // Volume 0 means the volume is not known.
    private static string? Comparable(string field, string? value) =>
        field == BookFieldKeys.VolumeNumber && value == "0" ? null : value;

    private void Select(string field, BookSource source)
    {
        _selected[field] = source;
        CountChanges();
    }

    // The ISBN read on the page is saved too: it counts as a change.
    private void CountChanges() => _changeCount = _comparison.CountChanges(_selected) + (_isbnFromPage ? 1 : 0);

    private bool IsSelected(string field, BookFieldOption option) =>
        option.Sources.Contains(_selected.GetValueOrDefault(field, BookSource.Current));

    private async Task<T?> AwaitProviderAsync<T>(Task<T> task, string provider) where T : class
    {
        try
        {
            return await task;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or OperationCanceledException)
        {
            Logger.LogError(ex, "Error fetching {Provider} for ISBN {ISBN}", provider, _isbn);
            Snackbar.Add($"Could not fetch data from {provider}. You can still save with current values.", Severity.Warning);
            return null;
        }
    }

    // The title of these sources mixes the series, the volume and the title of the book.
    private ParsedTitleInfo? ParseTitle(IBookSearchResult? result)
    {
        if (result is not { Found: true })
        {
            return null;
        }

        var (title, serie, volumeNumber) = ComicSearchService.ParseTitleInfo(result.Title, result.Subtitle);
        return new ParsedTitleInfo(title, serie, volumeNumber);
    }

    // Bedetheque and Bubble give the series and the volume apart.
    private static string? GetStructuredResultValue(IStructuredBookResult? result, string field)
    {
        if (result is not { Found: true } r)
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

    private static string? GetBookResultValue(IBookSearchResult? result, ParsedTitleInfo? parsed, string field)
    {
        if (result is not { Found: true })
        {
            return null;
        }

        return field switch
        {
            BookFieldKeys.Title => parsed?.Title,
            BookFieldKeys.Serie => parsed?.Serie,
            BookFieldKeys.VolumeNumber => parsed?.VolumeNumber.ToString(CultureInfo.InvariantCulture),
            BookFieldKeys.Authors => NullIfEmpty(string.Join(", ", result.Authors)),
            BookFieldKeys.Publishers => NullIfEmpty(string.Join(", ", result.Publishers)),
            BookFieldKeys.PublishDate => result.PublishDate?.ToString(PublishDateHelper.DisplayFormat, CultureInfo.InvariantCulture),
            BookFieldKeys.NumberOfPages => result.NumberOfPages?.ToString(CultureInfo.InvariantCulture),
            BookFieldKeys.Cover => result.CoverUrl?.ToString(),
            _ => null
        };
    }

    private string? GetBnfValue(string field) => GetBookResultValue(_bnfResult, _bnfParsed, field);

    private string? GetOlValue(string field) => GetBookResultValue(_olResult, _olParsed, field);

    private string? GetGoogleValue(string field) => GetBookResultValue(_googleResult, _googleParsed, field);

    private string? GetResolvedValue(string field) =>
        GetSourceValue(_selected.GetValueOrDefault(field, BookSource.Current), field);

    private string? GetSourceValue(BookSource source, string field) => source switch
    {
        BookSource.Bnf => GetBnfValue(field),
        BookSource.Bedetheque => GetStructuredResultValue(_bedethequeResult, field),
        BookSource.Bubble => GetStructuredResultValue(_bubbleResult, field),
        BookSource.OpenLibrary => GetOlValue(field),
        BookSource.Google => GetGoogleValue(field),
        _ => GetCurrentValue(field)
    };

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
                cover = await ComicSearchService.UploadCoverAsync(coverUri, _isbn ?? string.Empty);
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or OperationCanceledException)
            {
                Logger.LogError(ex, "Failed to upload cover to Cloudinary for book {BookId}, keeping original URL", BookId);
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
            _isbn ?? string.Empty,
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
