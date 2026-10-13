using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Web.Models;

namespace Web.Components.SharedComponents;

// A bottom sheet on a phone, a menu next to its anchor on a tablet or a large screen (ADR-0021).
public partial class ActionSheet
{
    [Parameter]
    public bool Open { get; set; }

    // Accessible name of the sheet.
    [Parameter, EditorRequired]
    public string AriaLabel { get; set; } = string.Empty;

    // Heading with a close button, shown on a phone only: a menu needs none.
    [Parameter]
    public string? Title { get; set; }

    // Where the menu opens from on a tablet or a large screen; centered without one.
    [Parameter]
    public PopupAnchor? Anchor { get; set; }

    // A button anchor: the menu lines up with its right edge. A long press: the menu starts at the finger.
    [Parameter]
    public bool AlignEnd { get; set; }

    [Parameter]
    public RenderFragment? Header { get; set; }

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    [Parameter]
    public EventCallback OnClose { get; set; }

    private ElementReference _panel;
    private bool _wasOpen;
    private bool _focusPending;

    protected override void OnParametersSet()
    {
        // Focus moves into the sheet when it opens, so that Escape and the keyboard reach it.
        _focusPending = Open && !_wasOpen;
        _wasOpen = Open;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (_focusPending)
        {
            await _panel.FocusAsync();
            _focusPending = false;
        }
    }

    private Task CloseAsync() => OnClose.InvokeAsync();

    private Task OnKeyDownAsync(KeyboardEventArgs args) =>
        args.Key == "Escape" ? CloseAsync() : Task.CompletedTask;
}
