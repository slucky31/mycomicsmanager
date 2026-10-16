using Microsoft.AspNetCore.Components;

namespace Web.Components.SharedComponents;

public partial class LibraryIcon
{
    // The Material name of the icon, e.g. "CollectionsBookmark".
    [Parameter, EditorRequired]
    public string Icon { get; set; } = string.Empty;

    [Parameter]
    public string? Color { get; set; }

    // 40px instead of 36px, with a bigger glyph.
    [Parameter]
    public bool Large { get; set; }
}
