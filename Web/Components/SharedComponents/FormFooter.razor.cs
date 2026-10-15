using Microsoft.AspNetCore.Components;

namespace Web.Components.SharedComponents;

public partial class FormFooter
{
    [Parameter]
    public RenderFragment? ChildContent { get; set; }
}
