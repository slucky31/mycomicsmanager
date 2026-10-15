using Microsoft.AspNetCore.Components;

namespace Web.Components.SharedComponents;

public partial class FormSection
{
    // The id of the section's title, which labels the section.
    [Parameter, EditorRequired]
    public string Id { get; set; } = string.Empty;

    [Parameter, EditorRequired]
    public string Title { get; set; } = string.Empty;

    [Parameter]
    public RenderFragment? ChildContent { get; set; }
}
