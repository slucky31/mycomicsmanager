using Microsoft.AspNetCore.Components;
using Web.Models;

namespace Web.Components.Pages.Libraries;

public partial class IconChooser
{
    // The Material name of the icon chosen.
    [Parameter]
    public string Value { get; set; } = string.Empty;

    [Parameter]
    public EventCallback<string> ValueChanged { get; set; }

    private string _query = string.Empty;
    private IReadOnlyList<LibraryIconChoice> _choices = LibraryIcons.Suggested;
    private string _caption = LibraryIcons.ResultsCaption(string.Empty, 0);

    // The suggestions follow the icon chosen (shown first when it is not one of them).
    protected override void OnParametersSet()
    {
        if (string.IsNullOrWhiteSpace(_query))
        {
            Show(string.Empty);
        }
    }

    private void Show(string query)
    {
        _query = query;
        _choices = LibraryIcons.Search(query, Value);
        _caption = LibraryIcons.ResultsCaption(query, _choices.Count);
    }

    private Task SearchAsync(ChangeEventArgs args)
    {
        Show(args.Value?.ToString() ?? string.Empty);
        return Task.CompletedTask;
    }

    private Task ChooseAsync(LibraryIconChoice choice) => ValueChanged.InvokeAsync(choice.Name);
}
