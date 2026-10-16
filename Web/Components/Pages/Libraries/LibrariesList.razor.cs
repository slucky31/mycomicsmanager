using Microsoft.AspNetCore.Components;
using MudBlazor;
using Web.Extensions;
using Web.Models;
using Web.Services;

namespace Web.Components.Pages.Libraries;

public sealed partial class LibrariesList : IDisposable
{
    [Inject] private ILibrariesService LibrariesService { get; set; } = default!;
    [Inject] private IDialogService DialogService { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;
    [Inject] private PopupAnchorLocator PopupAnchorLocator { get; set; } = default!;
    [Inject] private ILogger<LibrariesList> Logger { get; set; } = default!;

    public static readonly string Msg_LibCorrectlyDeleted = "The library was correctly deleted";
    public static readonly string Msg_LoadFailed = "Failed to load the libraries";
    public static readonly string Msg_DeleteFailed = "Failed to delete library";

    private readonly CancellationTokenSource _disposalCts = new();
    private List<LibraryCardViewModel> _libraries = [];
    private bool _isLoading = true;
    private LibraryCardViewModel? _actionsLibrary;
    private PopupAnchor? _sheetAnchor;

    protected override async Task OnInitializedAsync()
    {
        await LoadLibrariesAsync();
        _isLoading = false;
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
        var anchor = await PopupAnchorLocator.AnchorOfAsync(library.ActionsButtonId, _disposalCts.Token);
        _sheetAnchor = anchor;
        _actionsLibrary = library;
    }

    private void CloseActions() => _actionsLibrary = null;

    private async Task DeleteAsync(LibraryCardViewModel library)
    {
        CloseActions();

        var confirmed = await DialogService.ShowConfirmationAsync(
            "Delete library",
            $"The library “{library.Name}” will be deleted with its {LibraryDisplay.BookCount(library.BookCount)}. This cannot be undone.",
            "Delete library");
        if (!confirmed)
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

    public void Dispose()
    {
        _disposalCts.Cancel();
        _disposalCts.Dispose();
    }
}
