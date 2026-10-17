using Microsoft.AspNetCore.Components;

namespace Web.Components.SharedComponents;

// One line of an ActionSheet: a link, an action, or an option that can be selected.
public partial class ActionSheetItem
{
    [Parameter, EditorRequired]
    public string Icon { get; set; } = string.Empty;

    [Parameter, EditorRequired]
    public string Text { get; set; } = string.Empty;

    // The color of a library; the icon takes the color of the text without one.
    [Parameter]
    public string? IconColor { get; set; }

    // Under the text: what an option or a link is for.
    [Parameter]
    public string? Description { get; set; }

    // A link; otherwise a button calling OnClick.
    [Parameter]
    public string? Href { get; set; }

    [Parameter]
    public EventCallback OnClick { get; set; }

    // Null for an action; true or false for an option, the selected one being checked and in bold.
    [Parameter]
    public bool? Selected { get; set; }

    // Delete: in red.
    [Parameter]
    public bool Danger { get; set; }

    [Parameter]
    public bool Disabled { get; set; }

    private string? IconStyle => IconColor is null ? null : $"color: {IconColor};";

    private string? AriaPressed => Selected switch
    {
        true => "true",
        false => "false",
        null => null,
    };

    private string CssClass => (Selected, Danger) switch
    {
        (true, _) => "action-sheet-item action-sheet-item-selected",
        (_, true) => "action-sheet-item action-sheet-item-danger",
        _ => "action-sheet-item",
    };
}
