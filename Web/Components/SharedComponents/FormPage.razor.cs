using Microsoft.AspNetCore.Components;

namespace Web.Components.SharedComponents;

public partial class FormPage
{
    [Parameter]
    public string? Class { get; set; }

    [Parameter]
    public RenderFragment? ChildContent { get; set; }
}
