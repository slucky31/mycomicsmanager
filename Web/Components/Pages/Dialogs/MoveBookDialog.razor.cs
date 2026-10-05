using Microsoft.AspNetCore.Components;
using MudBlazor;
using Web.Models;

namespace Web.Components.Pages.Dialogs;

public partial class MoveBookDialog
{
    [CascadingParameter]
    public required IMudDialogInstance MudDialog { get; set; }

    [Parameter]
    public IReadOnlyList<BookMoveTargetViewModel> Targets { get; set; } = [];

    private Guid _selectedLibraryId;

    protected override void OnParametersSet()
    {
        if (_selectedLibraryId == Guid.Empty)
        {
            _selectedLibraryId = Targets.FirstOrDefault(t => t.IsSuggested)?.Id ?? Guid.Empty;
        }
    }

    private void Cancel() => MudDialog.Cancel();

    private void Confirm() => MudDialog.Close(DialogResult.Ok(_selectedLibraryId));
}
