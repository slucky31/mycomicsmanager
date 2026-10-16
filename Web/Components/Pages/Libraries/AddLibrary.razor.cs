using Microsoft.AspNetCore.Components;
using MudBlazor;
using Web.Models;
using Web.Services;
using Web.Validators;

namespace Web.Components.Pages.Libraries;

// The form to create a library (E3, E4).
public sealed partial class AddLibrary : IDisposable
{
    private const string Msg_CreateFailed = "Unable to create the library";

    [Inject] private ILibrariesService LibrariesService { get; set; } = default!;
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;
    [Inject] private ILogger<AddLibrary> Logger { get; set; } = default!;

    private readonly LibraryUiDto _model = new();
    private readonly LibraryValidator _validator = new();
    private readonly CancellationTokenSource _disposalCts = new();
    private FormErrors _errors = FormErrors.None;
    private bool _submitted;
    private bool _isSaving;

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
            return;
        }

        _isSaving = true;
        try
        {
            var result = await LibrariesService.Create(new CreateLibraryRequest(_model.Name, _model.Color, _model.Icon, _model.BookType), _disposalCts.Token);
            if (result.IsSuccess)
            {
                Snackbar.Add($"{_model.Name} was created", Severity.Success);
                NavigationManager.NavigateTo("/libraries/list");
                return;
            }

            Logger.LogError("Unable to create the library {Name}: {Error}", _model.Name, result.Error?.Description);
            Snackbar.Add(LibraryDisplay.SaveFailed(result.Error, Msg_CreateFailed), Severity.Error);
        }
        catch (Exception ex) when (ex is OperationCanceledException or InvalidOperationException)
        {
            Logger.LogError(ex, "Unexpected error while creating the library {Name}", _model.Name);
            Snackbar.Add(Msg_CreateFailed, Severity.Error);
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
