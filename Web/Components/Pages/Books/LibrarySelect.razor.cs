using Microsoft.AspNetCore.Components;
using Web.Models;
using Web.Services;
using Web.Validators;

namespace Web.Components.Pages.Books;

public partial class LibrarySelect
{
    [Inject] private PopupAnchorLocator PopupAnchorLocator { get; set; } = default!;

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

    // A first option for every library at once (Guid.Empty), e.g. "All libraries".
    [Parameter]
    public string? AllLabel { get; set; }

    // A pill in the bar of a page instead of a field of a form.
    [Parameter]
    public bool Compact { get; set; }

    private bool _open;
    private PopupAnchor? _anchor;

    private LibraryUiDto? Selected => Libraries.FirstOrDefault(library => library.Id == Value);

    private bool HasChoice => Selected is not null || AllLabel is not null;

    private string ValueText => Selected?.Name ?? AllLabel ?? "Choose a library";

    private string AriaLabel => $"Library: {ValueText}";

    private string CssClass
    {
        get
        {
            var css = Compact ? "library-select library-select-compact" : "library-select";
            return string.IsNullOrEmpty(Error) ? css : $"{css} library-select-invalid";
        }
    }

    private async Task OpenAsync()
    {
        var anchor = await PopupAnchorLocator.AnchorOfAsync(Id);
        _anchor = anchor;
        _open = true;
    }

    private void Close() => _open = false;

    private Task ChooseAsync(Guid libraryId)
    {
        Close();
        return ValueChanged.InvokeAsync(libraryId);
    }
}
