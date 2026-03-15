using System.Globalization;
using Microsoft.AspNetCore.Components;

namespace Web.Components.SharedComponents;

public partial class PageHeader
{
    [Parameter, EditorRequired] public string Title { get; set; } = "";
    [Parameter] public string? Subtitle { get; set; }
    [Parameter] public RenderFragment? SubtitleContent { get; set; }
    [Parameter] public EventCallback OnBack { get; set; }
    [Parameter] public string? LibraryColor { get; set; }
    [Parameter] public RenderFragment? Actions { get; set; }

    private string HeaderStyle => LibraryColor != null
        ? $"background: linear-gradient(135deg, {LibColorRgba(0.95)} 0%, {LibColorRgba(0.1)} 100%); border-bottom: 2px solid {LibColorRgba(0.9)};"
        : "";

    private string LibColorRgba(double alpha)
    {
        var hex = LibraryColor ?? "#5C6BC0";
        if (hex.StartsWith('#') && hex.Length == 7
            && int.TryParse(hex[1..3], NumberStyles.HexNumber, null, out var r)
            && int.TryParse(hex[3..5], NumberStyles.HexNumber, null, out var g)
            && int.TryParse(hex[5..7], NumberStyles.HexNumber, null, out var b))
        {
            return FormattableString.Invariant($"rgba({r},{g},{b},{alpha})");
        }
        return FormattableString.Invariant($"rgba(92,107,192,{alpha})");
    }
}
