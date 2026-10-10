using Domain.Books;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Web.Components.SharedComponents;

public partial class IsbnScanStateIcon
{
    [Parameter]
    public IsbnScanState State { get; set; }

    [Parameter]
    public int CandidateCount { get; set; }

    // Shown in the tooltip when the ISBN is known.
    [Parameter]
    public string? Isbn { get; set; }

    // On a cover, a known ISBN needs no mark: only a missing one is shown.
    [Parameter]
    public bool HideWhenKnown { get; set; }

    // Drawn over a cover: the QR code follows the light text color.
    [Parameter]
    public bool OnDarkBackground { get; set; }

    [Parameter]
    public Size Size { get; set; } = Size.Small;

    [Parameter]
    public string? Class { get; set; }
}
