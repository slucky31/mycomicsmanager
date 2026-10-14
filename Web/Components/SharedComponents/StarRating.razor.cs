using Microsoft.AspNetCore.Components;

namespace Web.Components.SharedComponents;

public partial class StarRating
{
    // 0 when no star is chosen.
    [Parameter]
    public int Value { get; set; }

    [Parameter]
    public EventCallback<int> ValueChanged { get; set; }

    // The id of the text that names the stars.
    [Parameter]
    public string? LabelledBy { get; set; }

    private Task ChooseAsync(int value) => ValueChanged.InvokeAsync(value == Value ? 0 : value);
}
