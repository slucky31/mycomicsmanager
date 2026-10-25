using Application.Interfaces;
using MudBlazor;
using Web.Models;
using Web.Validators;

namespace Web.Components.Pages.Books;

// A book without ISBN (#1237): the sources are searched by its title, its series and its volume, the user
// picks the matching edition, then its ISBN is searched on every source like the ISBN of the book.
public partial class FindBookDetails
{
    private string? _queryTitle;
    private string? _querySerie;
    private int? _queryVolume;

    // The state of the text search of each source, and the editions each one found.
    private readonly SortedDictionary<BookSource, ProviderSearchState> _textSearchStates = new();
    private readonly Dictionary<BookSource, IReadOnlyList<BookCandidate>> _candidates = [];

    // The text search shown: the results of an older one are dropped.
    private int _textSearchId;

    // The edition selected in the results, then the one compared with the book.
    private (BookSource Source, BookCandidate Candidate)? _chosen;
    private (BookSource Source, BookCandidate Candidate)? _edition;
    private bool _isCheckingIsbn;

    private bool IsTextSearch => _edition is null && string.IsNullOrWhiteSpace(_isbn);

    private bool IsTextSearching => _textSearchStates.ContainsValue(ProviderSearchState.Searching);

    private int TextSearchesDone => _textSearchStates.Values.Count(state => state != ProviderSearchState.Searching);

    private BookTextQuery TextQuery => new(_queryTitle, _querySerie, _queryVolume);

    // The ISBN saved with the book differs from its own: read on a page, or the one of the edition picked.
    private bool IsbnIsNew => IsbnFromPage || _edition is not null;

    // Another book is opened: nothing of the previous search stays.
    private void ResetTextSearch()
    {
        _textSearchId++;
        _chosen = null;
        _edition = null;
        _candidates.Clear();
        _textSearchStates.Clear();
    }

    private void InitTextQuery(BookUiDto book)
    {
        _queryTitle = book.Title;
        _querySerie = book.Serie;
        _queryVolume = book.VolumeNumber > 0 ? book.VolumeNumber : null;
    }

    private async Task SearchByTextAsync()
    {
        var query = TextQuery;
        if (query.IsEmpty)
        {
            return;
        }

        var searchId = ++_textSearchId;
        _chosen = null;
        _candidates.Clear();
        _textSearchStates.Clear();
        foreach (var source in WebSources())
        {
            _textSearchStates[source] = ProviderSearchState.Searching;
        }

        List<Task> searches = [.. WebSources().Select(source => SearchSourceByTextAsync(source, query, searchId))];

        StateHasChanged();
        await Task.WhenAll(searches);
    }

    private async Task SearchSourceByTextAsync(BookSource source, BookTextQuery query, int searchId)
    {
        var result = await AwaitProviderAsync(SearchTextAsync(source, query), BookSources.Name(source));

        // Another search was started meanwhile: this result is not for the query shown.
        if (searchId != _textSearchId)
        {
            return;
        }

        _textSearchStates[source] = ProviderSearchStates.Of(result);
        _candidates[source] = result?.Candidates ?? [];
        StateHasChanged();
    }

    private Task<BookCandidatesResult> SearchTextAsync(BookSource source, BookTextQuery query) => source switch
    {
        BookSource.Bnf => BnfCatalogueService.SearchByTextAsync(query),
        BookSource.Bedetheque => BedethequeService.SearchByTextAsync(query),
        BookSource.Bubble => BubbleService.SearchByTextAsync(query),
        BookSource.OpenLibrary => OpenLibraryService.SearchByTextAsync(query),
        BookSource.Google => GoogleBooksService.SearchByTextAsync(query),
        _ => Task.FromResult(BookCandidatesResult.None),
    };

    private IReadOnlyList<BookCandidate> CandidatesOf(BookSource source) => _candidates.GetValueOrDefault(source, []);

    // Only an edition with an ISBN can be searched on every source.
    private void Choose(BookSource source, BookCandidate candidate)
    {
        if (candidate.Isbn is not null)
        {
            _chosen = (source, candidate);
        }
    }

    private bool IsChosen(BookSource source, BookCandidate candidate) => _chosen == (source, candidate);

    // An ISBN held by another book of the same type cannot be given to this one (#1217): the edition is refused.
    private async Task CompareWithEditionAsync()
    {
        if (_chosen is not { Candidate.Isbn: { } isbn } chosen || _isCheckingIsbn)
        {
            return;
        }

        _isCheckingIsbn = true;
        try
        {
            var availability = await BooksService.IsIsbnAvailable(BookId, isbn);
            if (availability.IsFailure)
            {
                Logger.LogError("Could not check ISBN {Isbn} for book {BookId}: {Error}", isbn, BookId, availability.Error?.Description);
                Snackbar.Add($"Could not check the ISBN of this edition: {availability.Error?.Description}", Severity.Error);
                return;
            }

            if (!availability.Value)
            {
                Snackbar.Add($"ISBN {isbn} is already used by another of your books: pick another edition.", Severity.Error);
                return;
            }

            _edition = chosen;
            _isbn = isbn;
            _selected.Clear();
            RefreshComparison();
        }
        finally
        {
            _isCheckingIsbn = false;
        }

        StateHasChanged();
        await FetchWebServicesAsync();
    }

    // Back to the editions found: the comparison of the edition is dropped, the search is kept.
    private void ChangeEdition()
    {
        _edition = null;
        _isbn = null;
        _searchStates.Clear();
        ClearSourceResults();
        _selected.Clear();
        RefreshComparison();
    }
}
