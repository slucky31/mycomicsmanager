using Domain.Books;
using Microsoft.AspNetCore.Components;

namespace Web.Components.SharedComponents;

public partial class IsbnScanStateIcon
{
    [Parameter]
    public IsbnScanState State { get; set; }

    // Drawn over a cover: the neutral states follow the light text color.
    [Parameter]
    public bool OnDarkBackground { get; set; }

    [Parameter]
    public string? Class { get; set; }
}
