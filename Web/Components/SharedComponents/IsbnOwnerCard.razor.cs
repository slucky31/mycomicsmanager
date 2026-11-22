using Microsoft.AspNetCore.Components;
using Web.Models;

namespace Web.Components.SharedComponents;

public partial class IsbnOwnerCard
{
    [Parameter, EditorRequired]
    public IsbnOwnerViewModel Owner { get; set; } = default!;

    // Where "Open that book" leads.
    [Parameter, EditorRequired]
    public string Href { get; set; } = string.Empty;

    // The ISBN, shown with "Already used" when several ISBNs are listed; left out when the field above shows it.
    [Parameter]
    public string? Isbn { get; set; }
}
