using Microsoft.AspNetCore.Components;
using MudBlazor;
using Web.Services;
using Web.Validators;

namespace Web.Components.Pages.Libraries;

public partial class AddLibrary
{
    [Inject] private ILibrariesService LibrariesService { get; set; } = default!;
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;

    private readonly LibraryUiDto _model = new();
    private MudForm? _form;
    private readonly LibraryValidator _validator = new();
    private bool _isSaving;

    internal static readonly string[] ColorPalette =
    [
        "#5C6BC0",
        "#7B5EA7",
        "#26A69A",
        "#42A5F5",
        "#66BB6A",
        "#FFCA28",
        "#FF7043",
        "#EC407A",
        "#78909C",
        "#8D6E63",
        "#26C6DA",
        "#D4E157",
    ];

    private async Task SaveAsync()
    {
        if (_form is null)
        {
            return;
        }

        await _form.Validate();

        if (!_form.IsValid)
        {
            return;
        }

        _isSaving = true;

        var result = await LibrariesService.Create(
            new CreateLibraryRequest(_model.Name, _model.Color, _model.Icon, _model.BookType));

        if (result.IsSuccess)
        {
            Snackbar.Add("Library created successfully!", Severity.Success);
            NavigationManager.NavigateTo("/libraries/list");
        }
        else
        {
            Snackbar.Add(result.Error?.Description ?? "Failed to create library", Severity.Error);
            _isSaving = false;
        }
    }

    private void GoBack() => NavigationManager.NavigateTo("/libraries/list");
}
