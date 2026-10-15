using Microsoft.AspNetCore.Components;
using Web.Models;
using Web.Validators;

namespace Web.Components.Pages.Books;

public partial class BookFields
{
    [Parameter, EditorRequired]
    public BookUiDto Model { get; set; } = new();

    // The errors of the last validation, shown under the fields.
    [Parameter]
    public FormErrors Errors { get; set; } = FormErrors.None;

    // Called after each change of a field, for the form to validate it again.
    [Parameter]
    public EventCallback OnChanged { get; set; }

    private Task OnInputAsync(Action<string> setValue, ChangeEventArgs args)
    {
        setValue(args.Value?.ToString() ?? string.Empty);
        return NotifyChangedAsync();
    }

    private Task NotifyChangedAsync() => OnChanged.InvokeAsync();

    private string? Describe(string id, string property) => FormFieldIds.DescribedBy(id, Errors.For(property), null);
}
