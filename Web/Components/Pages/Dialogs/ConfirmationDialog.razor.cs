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

    // Color.Error: the action destroys something, its button is red.
    [Parameter]
    public Color ColorConfirmButton { get; set; } = Color.Primary;

    private readonly string _id = Guid.NewGuid().ToString("N");

    private string TitleId => $"confirm-title-{_id}";

    private string TextId => $"confirm-text-{_id}";

    private bool IsDanger => ColorConfirmButton == Color.Error;

    private void Cancel()
    {
        MudDialog.Cancel();
    }

    private void Confirm()
    {
        MudDialog.Close(DialogResult.Ok(true));
    }
}
