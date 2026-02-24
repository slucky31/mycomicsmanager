using Microsoft.AspNetCore.Components;
using MudBlazor;
using Web.Validators;

namespace Web.Components.Pages.Books;

public partial class BookForm
{
    [Parameter]
    public BookUiDto Model { get; set; } = new();

    [Parameter]
    public bool IsbnReadOnly { get; set; }

    [Parameter]
    public bool ShowRating { get; set; } = true;

    private readonly BookValidator _bookValidator = new();
    private MudForm _form = default!;

    // Conversion property for MudDatePicker (DateTime?) to DateOnly?
    private DateTime? PublishDateTime
    {
        get => Model.PublishDate?.ToDateTime(TimeOnly.MinValue);
        set => Model.PublishDate = value.HasValue ? DateOnly.FromDateTime(value.Value) : null;
    }

    public async Task<bool> ValidateAsync()
    {
        await _form.Validate();
        return _form.IsValid;
    }
}
