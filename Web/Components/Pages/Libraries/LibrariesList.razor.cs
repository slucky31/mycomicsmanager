using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;
using Web.Components.Pages.Dialogs;
using Web.Infrastructure;
using Web.Models;
using Web.Services;

namespace Web.Components.Pages.Libraries;

public partial class LibrariesList : IAsyncDisposable
{
    [Inject] private ILibrariesService LibrariesService { get; set; } = default!;
    [Inject] private IDialogService DialogService { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;
    [Inject] private IJSRuntime JS { get; set; } = default!;
    [Inject] private ILogger<LibrariesList> Logger { get; set; } = default!;

    public static readonly string Msg_LibCorrectlyDeleted = "The library was correctly deleted";
    public static readonly string Msg_LoadFailed = "Failed to load the libraries";
    public static readonly string Msg_DeleteFailed = "Failed to delete library";

    private readonly CancellationTokenSource _disposalCts = new();
    private List<LibraryCardViewModel> _libraries = [];
    private bool _isLoading = true;
    private LibraryCardViewModel? _actionsLibrary;
    private PopupAnchor? _sheetAnchor;
    private IJSObjectReference? _module;

    protected override async Task OnInitializedAsync()
    {
        await LoadLibrariesAsync();
        _isLoading = false;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender)
        {
            return;
        }

        try
        {
            _module = await JS.InvokeAsync<IJSObjectReference?>("import", _disposalCts.Token, AppVersion.Versioned("./js/popupAnchor.js"));
        }
        catch (JSException ex)
        {
            // Without the script the menus are centered on a tablet.
            Logger.LogWarning(ex, "Unable to load the popup anchor script");
        }
    }

    private async Task LoadLibrariesAsync()
    {
        var result = await LibrariesService.GetSummaries(_disposalCts.Token);
        if (result.IsSuccess && result.Value is not null)
        {
            _libraries = [.. result.Value.Select(LibraryCardViewModel.From)];
        }
        else if (result.IsFailure)
        {
            Logger.LogError("Failed to load the libraries: {Error}", result.Error?.Description);
            Snackbar.Add(Msg_LoadFailed, Severity.Error);
        }
    }

    private async Task OpenActionsAsync(LibraryCardViewModel library)
    {
        var anchor = await AnchorOfAsync(library.ActionsButtonId);
        _sheetAnchor = anchor;
        _actionsLibrary = library;
    }

    private void CloseActions() => _actionsLibrary = null;

    private async Task<PopupAnchor?> AnchorOfAsync(string elementId)
    {
        if (_module is null)
        {
            return null;
        }

        try
        {
            return await _module.InvokeAsync<PopupAnchor?>("anchorOf", _disposalCts.Token, elementId);
        }
        catch (JSException ex)
        {
            Logger.LogWarning(ex, "Unable to find the position of {ElementId}", elementId);
            return null;
        }
    }

    private async Task DeleteAsync(LibraryCardViewModel library)
    {
        CloseActions();

        var parameters = new DialogParameters<ConfirmationDialog>
        {
            { x => x.ConfirmationMessage, $"Do you really want to delete the library “{library.Name}” and its {LibraryDisplay.BookCount(library.BookCount)}? This process cannot be undone." },
            { x => x.ActionText, "Delete" },
            { x => x.ColorConfirmButton, Color.Error }
        };
        var options = new DialogOptions
        {
            CloseOnEscapeKey = true,
            MaxWidth = MaxWidth.ExtraSmall,
            CloseButton = false
        };

        var dialog = await DialogService.ShowAsync<ConfirmationDialog>("Delete library", parameters, options);
        var confirmation = await dialog.Result;
        if (confirmation is null || confirmation.Canceled)
        {
            return;
        }

        var result = await LibrariesService.Delete(library.Id.ToString(), _disposalCts.Token);
        if (result.IsFailure)
        {
            Logger.LogError("Failed to delete the library {LibraryId}: {Error}", library.Id, result.Error?.Description);
            Snackbar.Add(Msg_DeleteFailed, Severity.Error);
            return;
        }

        Snackbar.Add(Msg_LibCorrectlyDeleted, Severity.Success);
        await LoadLibrariesAsync();
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
                await _module.DisposeAsync();
            }
        }
        catch (Exception ex) when (ex is JSException or JSDisconnectedException or InvalidOperationException)
        {
            // JS interop can fail when the Blazor circuit is disconnected; intentionally ignored.
        }
    }
}
