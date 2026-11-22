using System.Globalization;

namespace Web.Models;

/// <summary>
/// What the page sees of the screen (window.mcmDisplay in App.razor): explains the space above the bars
/// of the app added to the iPhone Home Screen (#1250).
/// </summary>
public sealed record DisplayDiagnostics(
    bool Standalone,
    string DisplayMode,
    bool FillsScreen,
    bool AppMode,
    double StatusBar,
    double TopInset,
    int WindowHeight,
    int ScreenHeight);

public sealed record DisplayDiagnosticRow(string Name, string Value);

public static class DisplayDiagnosticsView
{
    public const string ScriptFunction = "mcmDisplay.diagnostics";

    public static string Summary(DisplayDiagnostics diagnostics)
    {
        ArgumentNullException.ThrowIfNull(diagnostics);
        return diagnostics.AppMode ? "Home Screen app" : "Browser";
    }

    public static IReadOnlyList<DisplayDiagnosticRow> Rows(DisplayDiagnostics diagnostics)
    {
        ArgumentNullException.ThrowIfNull(diagnostics);
        return
        [
            new("Home Screen app", YesNo(diagnostics.AppMode)),
            new("navigator.standalone", YesNo(diagnostics.Standalone)),
            new("Display mode", diagnostics.DisplayMode),
            new("Window fills the screen", $"{YesNo(diagnostics.FillsScreen)} ({diagnostics.WindowHeight} / {diagnostics.ScreenHeight} px)"),
            new("Status bar", Pixels(diagnostics.StatusBar)),
            new("Space above the bars", Pixels(diagnostics.TopInset)),
        ];
    }

    private static string YesNo(bool value) => value ? "Yes" : "No";

    private static string Pixels(double value) => $"{Math.Round(value, MidpointRounding.AwayFromZero).ToString(CultureInfo.InvariantCulture)} px";
}
