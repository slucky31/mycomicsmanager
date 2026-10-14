using Microsoft.AspNetCore.Components;

namespace Web.Components.SharedComponents;

public partial class FormField
{
    // The id of the input, select or textarea of the field.
    [Parameter, EditorRequired]
    public string Id { get; set; } = string.Empty;

    [Parameter, EditorRequired]
    public string Label { get; set; } = string.Empty;

    [Parameter]
    public bool Required { get; set; }

    [Parameter]
    public string? Error { get; set; }

    [Parameter]
    public string? Help { get; set; }

    [Parameter]
    public RenderFragment? ChildContent { get; set; }
}
