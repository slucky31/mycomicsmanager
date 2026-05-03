using Microsoft.AspNetCore.Components;
using MudBlazor;
using Web.Models;

namespace Web.Components.Pages.Libraries.Views;

public partial class BooksListView
{
    [Parameter, EditorRequired]
    public Func<TableState, CancellationToken, Task<TableData<BookListItemViewModel>>> ServerData { get; set; } = default!;

    [Parameter, EditorRequired]
    public EventCallback<Guid> OnDelete { get; set; }

    [Parameter]
    public bool ShowDownload { get; set; }

    [Parameter]
    public EventCallback<Guid> OnDownload { get; set; }

    private MudTable<BookListItemViewModel>? _table;

    public Task ReloadAsync() => _table?.ReloadServerData() ?? Task.CompletedTask;
}
