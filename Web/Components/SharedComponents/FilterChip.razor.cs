using Microsoft.AspNetCore.Components;

namespace Web.Components.SharedComponents;

public partial class FilterChip
{
    [Parameter, EditorRequired]
    public string Text { get; set; } = string.Empty;

    [Parameter]
    public bool Selected { get; set; }

    [Parameter]
    public EventCallback OnClick { get; set; }
}
