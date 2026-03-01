using System.Reflection;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Web.Components.SharedComponents;

public partial class IconPicker : ComponentBase
{
    private static readonly Dictionary<string, string> s_iconMap =
        typeof(Icons.Material.Filled)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(f => f.FieldType == typeof(string))
            .ToDictionary(f => f.Name, f => (string)f.GetValue(null)!);

    [Parameter]
    public string Value { get; set; } = string.Empty;

    [Parameter]
    public EventCallback<string> ValueChanged { get; set; }

    [Parameter]
    public string Label { get; set; } = "Icon";

    private string _selectedName = string.Empty;

    /// <summary>Resolves an icon name to its MudBlazor SVG path constant.</summary>
    public static string Resolve(string? name)
        => !string.IsNullOrEmpty(name) && s_iconMap.TryGetValue(name, out var svg)
            ? svg
            : Icons.Material.Filled.Star;

    protected override void OnParametersSet()
    {
        _selectedName = Value;
    }

    private static Task<IEnumerable<string>> SearchIconNamesAsync(string value, CancellationToken _)
    {
        var results = string.IsNullOrEmpty(value)
            ? s_iconMap.Keys.Take(50)
            : s_iconMap.Keys.Where(n => n.Contains(value, StringComparison.OrdinalIgnoreCase)).Take(50);
        return Task.FromResult<IEnumerable<string>>(results);
    }

    private async Task OnNameChangedAsync(string name)
    {
        _selectedName = name;
        await ValueChanged.InvokeAsync(name);
    }
}
