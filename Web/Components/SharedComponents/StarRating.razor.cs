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

    // The stars only show the rating, as an image named "Rated 4 out of 5".
    [Parameter]
    public bool ReadOnly { get; set; }

    private string RatingLabel => $"Rated {Value} out of 5";

    private Task ChooseAsync(int value) => ValueChanged.InvokeAsync(value == Value ? 0 : value);
}
