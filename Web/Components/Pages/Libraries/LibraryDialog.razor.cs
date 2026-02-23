using Microsoft.AspNetCore.Components;
using MudBlazor;
using Web.Validators;

namespace Web.Components.Pages.Libraries;

public partial class LibraryDialog
{
    [CascadingParameter]
    private IMudDialogInstance? MudDialog { get; set; }

    [Parameter]
    public LibraryUiDto library { get; set; } = new LibraryUiDto();

    private MudForm? _form;

    private readonly LibraryValidator _libraryValidator = new();

    private async Task SubmitAsync()
    {
        if (_form is null)
        {
            return;
        }

        await _form.Validate();

        if (_form.IsValid && MudDialog is not null)
        {
            MudDialog.Close(DialogResult.Ok(library));
        }
    }

    private void Cancel()
    {
        MudDialog?.Cancel();
    }
}
