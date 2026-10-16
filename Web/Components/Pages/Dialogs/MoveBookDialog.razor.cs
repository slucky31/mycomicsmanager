using Microsoft.AspNetCore.Components;
using MudBlazor;
using Web.Models;

namespace Web.Components.Pages.Dialogs;

public partial class MoveBookDialog
{
    [CascadingParameter]
    public required IMudDialogInstance MudDialog { get; set; }

    [Parameter]
    public BookMoveChoices Choices { get; set; } = new([], null);

    private BookMoveTargetViewModel? _selected;

    private string TitleId { get; } = $"move-title-{Guid.NewGuid():N}";

    // The suggested library is chosen when the dialog opens.
    protected override void OnParametersSet()
    {
        if (_selected is null && Choices.Preselected is { } preselected)
        {
            Choose(Choices.Libraries.First(l => l.Id == preselected));
        }
    }

    private void Choose(BookMoveTargetViewModel library)
    {
        if (library.IsCurrent)
        {
            return;
        }

        _selected = library;
    }

    private void Cancel() => MudDialog.Cancel();

    private void Confirm()
    {
        if (_selected is not null)
        {
            MudDialog.Close(DialogResult.Ok(_selected.Id));
        }
    }
}
