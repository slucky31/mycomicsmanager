using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Web.Components.Pages.Dialogs;

public partial class ConfirmationDialog
{
    [CascadingParameter]
    public required IMudDialogInstance MudDialog { get; set; }

    [Parameter]
    public string? ConfirmationMessage { get; set; }

    [Parameter]
    public string ActionText { get; set; } = "Confirm";

    [Parameter]
    public Color ColorConfirmButton { get; set; } = Color.Primary;

    private void Cancel()
    {
        MudDialog.Cancel();
    }

    private void Confirm()
    {
        MudDialog.Close(DialogResult.Ok(true));
    }

}
