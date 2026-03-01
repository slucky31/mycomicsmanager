using Microsoft.AspNetCore.Components;
using MudBlazor;
using Serilog;
using Web.Services;
using Web.Validators;

namespace Web.Components.Pages.Libraries;

public partial class EditLibrary
{
    [Inject] private ILibrariesService LibrariesService { get; set; } = default!;
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;

    [Parameter] public string? LibraryId { get; set; }

    private LibraryUiDto? _model;
    private MudForm? _form;
    private readonly LibraryValidator _validator = new();
    private bool _isLoading = true;
    private bool _isSaving;

    protected override async Task OnInitializedAsync()
    {
        var result = await LibrariesService.GetById(LibraryId);

        if (result.IsSuccess)
        {
            _model = LibraryUiDto.Convert(result.Value!);
        }
        else
        {
            Snackbar.Add("Library not found", Severity.Error);
        }

        _isLoading = false;
    }

    private async Task SaveAsync()
    {
        if (_form is null || _model is null)
        {
            return;
        }

        await _form.Validate();

        if (!_form.IsValid)
        {
            return;
        }

        _isSaving = true;

        try
        {
            var result = await LibrariesService.Update(
                new UpdateLibraryRequest(_model.Id.ToString(), _model.Name, _model.Color, _model.Icon));

            if (result.IsSuccess)
            {
                NavigationManager.NavigateTo($"/libraries/{LibraryId}");
                return;
            }

            Snackbar.Add("Failed to update library", Severity.Error);
            Log.Error("Failed to update library: {Error}", result.Error);
        }
        catch (InvalidOperationException ex)
        {
            Snackbar.Add("Failed to update library", Severity.Error);
            Log.Error(ex, "Failed to update library");
        }
        finally
        {
            _isSaving = false;
        }
    }

    private void GoBack() => NavigationManager.NavigateTo($"/libraries/{LibraryId}");

    private void SelectColor(string hex)
    {
        if (_model is null)
        {
            return;
        }

        _model.Color = hex;
    }
}
