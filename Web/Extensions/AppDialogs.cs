using MudBlazor;

namespace Web.Extensions;

// The dialogs of the redesign (ADR-0021): a sheet at the bottom of a phone, centered from the tablet on (MainLayout.razor.css).
public static class AppDialogs
{
    // Their own title; Escape and a tap outside close them; the focus starts on the first button, which never destroys anything.
    public static DialogOptions Options => new()
    {
        NoHeader = true,
        CloseButton = false,
        CloseOnEscapeKey = true,
        BackdropClick = true,
        FullWidth = true,
        MaxWidth = MaxWidth.ExtraSmall,
        DefaultFocus = DefaultFocus.FirstChild,
    };
}
