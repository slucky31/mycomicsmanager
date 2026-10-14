using Microsoft.AspNetCore.Components;
using Web.Validators;

namespace Web.Components.Pages.Books;

public partial class LibrarySelect
{
    [Parameter]
    public string Id { get; set; } = "book-library";

    [Parameter]
    public IReadOnlyList<LibraryUiDto> Libraries { get; set; } = [];

    [Parameter]
    public Guid Value { get; set; }

    [Parameter]
    public EventCallback<Guid> ValueChanged { get; set; }

    [Parameter]
    public string? Error { get; set; }

    private LibraryUiDto? Selected => Libraries.FirstOrDefault(library => library.Id == Value);

    private Task OnChangeAsync(ChangeEventArgs args) =>
        Guid.TryParse(args.Value?.ToString(), out var id) ? ValueChanged.InvokeAsync(id) : Task.CompletedTask;
}
