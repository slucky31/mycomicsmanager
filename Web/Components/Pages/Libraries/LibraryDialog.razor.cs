using Application.Libraries;
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

    private MudForm? form;

    private LibraryValidator libraryValidator = new LibraryValidator();

    private async Task Submit()
    {
        if (form is null)
            return;

        await form.Validate();

        if (form.IsValid && MudDialog is not null)
        {
            MudDialog.Close(DialogResult.Ok(library));
        }
    }

    private void Cancel()
    {
        if (MudDialog is not null)
        {
            MudDialog.Cancel();
        }
    }
}
