using Domain.Libraries;
using Microsoft.AspNetCore.Components;
using Web.Models;
using Web.Validators;

namespace Web.Components.Pages.Libraries;

public partial class LibraryForm
{
    // The id of the form, for the buttons of the header to submit it.
    [Parameter, EditorRequired]
    public string FormId { get; set; } = string.Empty;

    [Parameter, EditorRequired]
    public LibraryUiDto Model { get; set; } = new();

    // A new library: its type can be chosen.
    [Parameter]
    public bool IsNew { get; set; }

    // The Imported library keeps its name: the feed imports deposit their files there.
    [Parameter]
    public bool NameLocked { get; set; }

    private string? NameHelp => NameLocked ? "The feed imports add their books here: this library keeps its name." : null;

    [Parameter]
    public FormErrors Errors { get; set; } = FormErrors.None;

    // Called after each change, for the page to validate the form again.
    [Parameter]
    public EventCallback OnChanged { get; set; }

    [Parameter]
    public EventCallback OnSubmit { get; set; }

    // What the preview shows of the books of the library.
    [Parameter]
    public int BookCount { get; set; }

    [Parameter]
    public IReadOnlyList<string> Covers { get; set; } = [];

    // The actions kept at the bottom of the screen on a phone.
    [Parameter]
    public RenderFragment? Footer { get; set; }

    private bool _iconsOpen;

    private LibraryCardViewModel Preview =>
        LibraryCardViewModel.Preview(Model.Id, Model.Name, Model.Color, Model.Icon, Model.BookType, BookCount, Covers);

    private string IconLabel => LibraryIcons.LabelOf(Model.Icon);

    private string? Describe(string id, string property) => FormFieldIds.DescribedBy(id, Errors.For(property), null);

    private Task OnNameInputAsync(ChangeEventArgs args)
    {
        Model.Name = args.Value?.ToString() ?? string.Empty;
        return OnChanged.InvokeAsync();
    }

    private Task ChooseTypeAsync(LibraryBookType type)
    {
        Model.BookType = type;
        return OnChanged.InvokeAsync();
    }

    private Task ChooseColorAsync(LibraryColor color)
    {
        Model.Color = color.Hex;
        return OnChanged.InvokeAsync();
    }

    private Task ChooseIconAsync(string icon)
    {
        Model.Icon = icon;
        return OnChanged.InvokeAsync();
    }

    private void OpenIcons() => _iconsOpen = true;

    private void CloseIcons() => _iconsOpen = false;

    private Task ChooseIconFromSheetAsync(string icon)
    {
        _iconsOpen = false;
        return ChooseIconAsync(icon);
    }
}
