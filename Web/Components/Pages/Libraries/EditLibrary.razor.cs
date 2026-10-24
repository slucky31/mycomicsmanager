using Microsoft.AspNetCore.Components;
using MudBlazor;
using Web.Models;
using Web.Services;
using Web.Validators;

namespace Web.Components.Pages.Libraries;

// The form to edit a library (T12): its name, color and icon; its type cannot be changed.
public sealed partial class EditLibrary : IDisposable
{
    private const string Msg_LoadFailed = "Unable to load the library";
    private const string Msg_SaveFailed = "Unable to save the changes";

    [Inject] private ILibrariesService LibrariesService { get; set; } = default!;
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;
    [Inject] private ILogger<EditLibrary> Logger { get; set; } = default!;

    [Parameter] public string? LibraryId { get; set; }

    private readonly LibraryValidator _validator = new();
    private readonly CancellationTokenSource _disposalCts = new();
    private LibraryUiDto? _model;
    private string? _name;
    private bool _nameLocked;
    private int _bookCount;
    private IReadOnlyList<string> _covers = [];
    private FormErrors _errors = FormErrors.None;
    private bool _submitted;
    private bool _isLoading = true;
    private bool _isSaving;

    private string LibraryHref => $"/libraries/{LibraryId}";

    protected override async Task OnInitializedAsync()
    {
        try
        {
            await LoadLibraryAsync();
            if (_model is not null)
            {
                await LoadBooksAsync(_model.Id);
            }
        }
        finally
        {
            _isLoading = false;
        }
    }

    private async Task LoadLibraryAsync()
    {
        try
        {
            var result = await LibrariesService.GetById(LibraryId);
            if (result.IsSuccess && result.Value is not null)
            {
                _model = LibraryUiDto.Convert(result.Value);
                _name = result.Value.Name;
                _nameLocked = result.Value.IsImported;
                return;
            }

            Logger.LogError("Unable to load the library {LibraryId}: {Error}", LibraryId, result.Error?.Description);
        }
        catch (Exception ex) when (ex is OperationCanceledException or InvalidOperationException)
        {
            Logger.LogError(ex, "Unexpected error while loading the library {LibraryId}", LibraryId);
        }

        Snackbar.Add(Msg_LoadFailed, Severity.Error);
    }

    // The count of books and latest covers of the preview; the card shows none when they cannot be loaded.
    private async Task LoadBooksAsync(Guid libraryId)
    {
        // The library edited may be hidden.
        var result = await LibrariesService.GetSummaries(includeHidden: true, _disposalCts.Token);
        var summary = result.IsSuccess ? result.Value?.FirstOrDefault(s => s.Id == libraryId) : null;
        if (summary is null)
        {
            Logger.LogWarning("Unable to load the books of the library {LibraryId}: {Error}", libraryId, result.Error?.Description);
            return;
        }

        _bookCount = summary.BookCount;
        _covers = summary.Covers;
    }

    // Once the form was submitted, its errors follow what is typed.
    private void Revalidate()
    {
        if (_submitted && _model is not null)
        {
            _errors = FormErrors.From(_validator.Validate(_model));
        }
    }

    private async Task SaveAsync()
    {
        if (_isSaving || _model is null)
        {
            return;
        }

        _submitted = true;
        _errors = FormErrors.From(await _validator.ValidateAsync(_model, _disposalCts.Token));
        if (_errors.Any)
        {
            return;
        }

        _isSaving = true;
        try
        {
            var result = await LibrariesService.Update(new UpdateLibraryRequest(_model.Id.ToString(), _model.Name, _model.Color, _model.Icon, IsHidden: _model.IsHidden), _disposalCts.Token);
            if (result.IsSuccess)
            {
                Snackbar.Add($"{_model.Name} was saved", Severity.Success);
                NavigationManager.NavigateTo(LibraryHref);
                return;
            }

            Logger.LogError("Unable to update the library {LibraryId}: {Error}", LibraryId, result.Error?.Description);
            Snackbar.Add(LibraryDisplay.SaveFailed(result.Error, Msg_SaveFailed), Severity.Error);
        }
        catch (Exception ex) when (ex is OperationCanceledException or InvalidOperationException)
        {
            Logger.LogError(ex, "Unexpected error while updating the library {LibraryId}", LibraryId);
            Snackbar.Add(Msg_SaveFailed, Severity.Error);
        }
        finally
        {
            _isSaving = false;
        }
    }

    public void Dispose()
    {
        _disposalCts.Cancel();
        _disposalCts.Dispose();
    }
}
