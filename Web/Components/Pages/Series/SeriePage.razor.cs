using Domain.Series;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;
using Web.Components.Pages.Libraries.Views;
using Web.Infrastructure;
using Web.Models;
using Web.Services;

namespace Web.Components.Pages.Series;

// A series of a library: how complete it is, and its volumes with the missing ones in their place (#1205),
// each book with the actions it has among the covers of its library (#1220).
public partial class SeriePage : IAsyncDisposable
{
    [Inject] private ISeriesService SeriesService { get; set; } = default!;
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;
    [Inject] private ILogger<SeriePage> Logger { get; set; } = default!;
    [Inject] private IJSRuntime JS { get; set; } = default!;
    [Inject] private PopupAnchorLocator PopupAnchorLocator { get; set; } = default!;

    public const string Msg_LoadFailed = "Failed to load the series";

    [Parameter] public Guid LibraryId { get; set; }

    [SupplyParameterFromQuery(Name = "name")]
    public string? Name { get; set; }

    private readonly CancellationTokenSource _disposalCts = new();
    private (Guid LibraryId, string? Name) _shown;
    private SeriePageViewModel? _serie;
    private bool _isLoading = true;
    // The series could not be loaded, for another reason than it being gone: an error with Retry (#1241).
    private bool _loadFailed;
    private bool _isEditing;
    private bool _isSaving;
    private readonly SerieTotalForm _form = new();

    private BookActionsTarget? _actionsBook;
    private bool _isActionsOpen;
    private PopupAnchor? _actionsAnchor;
    private bool _actionsAlignEnd;

    private ElementReference _content;
    private IJSObjectReference? _module;
    private DotNetObjectReference<SeriePage>? _dotNetRef;

    protected override async Task OnParametersSetAsync()
    {
        if (_shown == (LibraryId, Name))
        {
            return;
        }

        _shown = (LibraryId, Name);
        _isEditing = false;
        _serie = null;
        await RetryAsync();
    }

    private async Task RetryAsync()
    {
        _isLoading = true;
        try
        {
            await LoadAsync();
        }
        finally
        {
            _isLoading = false;
        }
    }

    private async Task LoadAsync(bool bookGone = false)
    {
        var shown = _shown;
        if (string.IsNullOrWhiteSpace(shown.Name))
        {
            NavigationManager.NavigateTo(SerieDisplay.SeriesHref(shown.LibraryId));
            return;
        }

        try
        {
            var result = await SeriesService.GetSerie(shown.LibraryId, shown.Name, _disposalCts.Token);
            if (shown != _shown)
            {
                return;
            }

            if (result.IsSuccess)
            {
                _serie = SeriePageViewModel.From(result.Value!);
                _loadFailed = false;
            }
            else if (result.IsFailure && result.Error == SeriesError.NotFound)
            {
                ShowGone(shown.LibraryId, bookGone);
            }
            else if (result.IsFailure)
            {
                // A database or network error: the series may well still exist, so the page stays to retry.
                _loadFailed = true;
                Snackbar.Add(Msg_LoadFailed, Severity.Error);
                Logger.LogError("Failed to load a series of library {LibraryId}: {ErrorDescription}", shown.LibraryId, result.Error!.Description);
            }
        }
        catch (OperationCanceledException)
        {
            // Page left while loading.
        }
    }

    private void ShowGone(Guid libraryId, bool bookGone)
    {
        // When its last book was moved or deleted, the series is gone too: no need to say so.
        if (!bookGone)
        {
            Snackbar.Add(SeriesError.NotFound.Description!, Severity.Error);
        }

        NavigationManager.NavigateTo(SerieDisplay.SeriesHref(libraryId));
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender)
        {
            return;
        }

        try
        {
            _dotNetRef = DotNetObjectReference.Create(this);
            var module = await JS.InvokeAsync<IJSObjectReference?>("import", _disposalCts.Token, AppVersion.Versioned("./js/libraryPage.js"));
            if (module is not null)
            {
                await module.InvokeVoidAsync("attach", _disposalCts.Token, _content, _dotNetRef);
                _module = module;
            }
        }
        catch (OperationCanceledException)
        {
            // Page left while rendering.
        }
        catch (JSException ex)
        {
            Logger.LogWarning(ex, "Unable to set up the long press on the books of a series of library {LibraryId}", LibraryId);
        }
    }

    // ── Actions of a book (#1220) ──────────────────────────────────────

    internal async Task OpenBookActionsAsync(BookActionsTarget book)
    {
        var anchor = await PopupAnchorLocator.AnchorOfAsync(BookActionsIds.Of(book.Id), _disposalCts.Token);
        ShowBookActions(book, anchor, alignEnd: true);
    }

    // Long press or right click on a book (libraryPage.js): the menu opens at the finger.
    [JSInvokable]
    public void OpenBookActionsAt(string bookId, PopupAnchor anchor)
    {
        var book = Guid.TryParse(bookId, out var id) ? _serie?.Slots.Select(s => s.Book).FirstOrDefault(b => b?.Id == id) : null;
        if (book is null)
        {
            return;
        }

        ShowBookActions(book, anchor, alignEnd: false);
        StateHasChanged();
    }

    private void ShowBookActions(BookActionsTarget book, PopupAnchor? anchor, bool alignEnd)
    {
        _actionsBook = book;
        _actionsAnchor = anchor;
        _actionsAlignEnd = alignEnd;
        _isActionsOpen = true;
    }

    private void CloseActions() => _isActionsOpen = false;

    // A book moved or deleted leaves its place to a missing volume, or the series when it was the last one.
    private Task ReloadAfterBookGoneAsync() => LoadAsync(bookGone: true);

    // ── Number of volumes published ────────────────────────────────────

    private void StartEdit()
    {
        _form.TotalVolumes = _serie?.TotalVolumes;
        _form.IsFinished = _serie?.IsFinished ?? false;
        _isEditing = true;
    }

    private void CancelEdit() => _isEditing = false;

    private async Task SaveAsync()
    {
        if (_serie is null)
        {
            return;
        }

        _isSaving = true;
        try
        {
            var result = await SeriesService.SetTotal(LibraryId, _serie.Name, _form.TotalVolumes, _form.IsFinished, _disposalCts.Token);
            if (result.IsSuccess)
            {
                _isEditing = false;
                await LoadAsync();
            }
            else if (result.IsFailure)
            {
                Snackbar.Add(result.Error!.Description ?? "Failed to save the volumes", Severity.Error);
                Logger.LogError("Failed to save the volumes of a series of library {LibraryId}: {ErrorDescription}", LibraryId, result.Error.Description);
            }
        }
        catch (OperationCanceledException)
        {
            // Page left while saving.
        }
        finally
        {
            _isSaving = false;
        }
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA1816", Justification = "No finalizer; S3971 prohibits GC.SuppressFinalize in DisposeAsync.")]
    public async ValueTask DisposeAsync()
    {
        await _disposalCts.CancelAsync();
        _disposalCts.Dispose();
        try
        {
            if (_module is not null)
            {
                await _module.InvokeVoidAsync("detach", CancellationToken.None, _content);
                await _module.DisposeAsync();
            }
        }
        catch (Exception ex) when (ex is JSException or JSDisconnectedException or InvalidOperationException)
        {
            // JS interop can fail when the Blazor circuit is disconnected; intentionally ignored.
        }
        finally
        {
            _dotNetRef?.Dispose();
        }
    }

    private sealed class SerieTotalForm
    {
        public int? TotalVolumes { get; set; }

        public bool IsFinished { get; set; }
    }
}
