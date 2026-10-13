using System.Globalization;

namespace Web.Models;

// Where a menu opens from, in CSS pixels of the viewport: the bounds of a button, or a point (zero size) for a long press.
public sealed record PopupAnchor(double Left, double Top, double Right, double Bottom, double ViewportWidth, double ViewportHeight)
{
    public static PopupAnchor At(double x, double y, double viewportWidth, double viewportHeight) =>
        new(x, y, x, y, viewportWidth, viewportHeight);
}

// Places a menu next to its anchor on a tablet or a large screen (a phone shows a bottom sheet instead, in CSS):
// below the anchor, or above it when there is more room, and always inside the viewport.
public static class PopupPlacement
{
    public const double Width = 300;
    private const double Margin = 8;
    private const double Gap = 4;

    // The CSS variables read by the sheet; empty without an anchor, the menu is then centered.
    public static string Style(PopupAnchor? anchor, bool alignEnd)
    {
        if (anchor is null)
        {
            return string.Empty;
        }

        var left = alignEnd ? anchor.Right - Width : anchor.Left;
        left = Math.Clamp(left, Margin, Math.Max(Margin, anchor.ViewportWidth - Width - Margin));

        var roomBelow = anchor.ViewportHeight - anchor.Bottom;
        if (roomBelow >= anchor.Top)
        {
            var top = anchor.Bottom + Gap;
            return Css(left, "--popup-top", top, anchor.ViewportHeight - top - Margin);
        }

        var bottom = anchor.ViewportHeight - anchor.Top + Gap;
        return Css(left, "--popup-bottom", bottom, anchor.Top - Gap - Margin);
    }

    private static string Css(double left, string verticalVariable, double vertical, double maxHeight) =>
        string.Create(CultureInfo.InvariantCulture,
            $"--popup-left: {Math.Round(left)}px; {verticalVariable}: {Math.Round(vertical)}px; --popup-max-height: {Math.Round(Math.Max(maxHeight, 0))}px;");
}
