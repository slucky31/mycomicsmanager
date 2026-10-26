using MudBlazor;

namespace Web.Infrastructure;

// The MudBlazor theme shared by every layout. The colours of the components' own styles are in wwwroot/css/theme.css.
internal static class AppTheme
{
    public static MudTheme Theme { get; } = new()
    {
        Typography = new Typography
        {
            // MudBlazor writes its buttons in capitals, the app's own buttons are in sentence case.
            Button = new ButtonTypography { TextTransform = "none" },
        },
    };
}
