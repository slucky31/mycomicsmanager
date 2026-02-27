using Microsoft.AspNetCore.Components;
using MudBlazor;
using Web.Validators;

namespace Web.Components.Pages.Libraries;

public partial class LibraryDialog
{
    [CascadingParameter]
    private IMudDialogInstance? MudDialog { get; set; }

    [Parameter]
    public LibraryUiDto Library { get; set; } = new();

    private MudForm? _form;

    private readonly LibraryValidator _libraryValidator = new();

    private static readonly string[] ColorPalette =
    [
        "#5C6BC0", // Indigo (default)
        "#7B5EA7", // Purple
        "#26A69A", // Teal
        "#42A5F5", // Blue
        "#66BB6A", // Green
        "#FFCA28", // Amber
        "#FF7043", // Deep Orange
        "#EC407A", // Pink
        "#78909C", // Blue Grey
        "#8D6E63", // Brown
        "#26C6DA", // Cyan
        "#D4E157", // Lime
    ];

    private async Task SubmitAsync()
    {
        if (_form is null)
        {
            return;
        }

        await _form.Validate();

        if (_form.IsValid && MudDialog is not null)
        {
            MudDialog.Close(DialogResult.Ok(Library));
        }
    }

    private void Cancel()
    {
        MudDialog?.Cancel();
    }

    private void SelectColor(string hex)
    {
        Library.Color = hex;
    }
}
