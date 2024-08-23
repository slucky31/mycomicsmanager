using Ardalis.GuardClauses;
using Domain.Libraries;
using Domain.Primitives;
using Microsoft.AspNetCore.Components;
using MongoDB.Bson;
using MudBlazor;
using Web.Services;

namespace Web.Components.Pages.Libraries;

public partial class LibrairiesList
{
    [Inject] private ILibrariesService LibrariesService { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;

    private MudTable<Library>? table;
    private string searchString = "";

    public static readonly string Msg_NoRecordsFound = "No matching records found";
    public static readonly string Msg_LibCorrectlyDeleted = "The library was correctly deleted";

    private async Task<TableData<Library>> OnReloadData(TableState state, CancellationToken token)
    {
        var sortColumn = LibrariesColumn.Id;
        if (state.SortLabel == "name_field")
        {
            sortColumn = LibrariesColumn.Name;
        }

        var sortOrder = SortOrder.Ascending;
        if (state.SortDirection == SortDirection.Descending)
        {
            sortOrder = SortOrder.Descending;
        }

        var result = await LibrariesService.FilterBy(searchString, sortColumn, sortOrder, state.Page + 1, state.PageSize);
        if (result is not null && result.Items is not null)
        {
            return new TableData<Library> { TotalItems = result.TotalCount, Items = result.Items.ToList() };
        }

        return new TableData<Library> { TotalItems = 0, Items = new List<Library>() };
    }

    private async Task OnClickDelete(ObjectId id)
    {
        var result = await LibrariesService.Delete(id.ToString());
        Guard.Against.Null(result);
        if (result.IsFailure)
        {
            Guard.Against.Null(result.Error);
            Snackbar.Add(result.Error.Description, Severity.Error);
        }
        else
        {
            Snackbar.Add(Msg_LibCorrectlyDeleted, Severity.Success);
            if (table is not null)
            {
                await table.ReloadServerData();
            }
        }
    }

    private void OnClickEdit(ObjectId id)
    {
        MyNavigationManager.NavigateTo($"/libraries/update/{id}");
    }

    private void OnSearch(string text)
    {
        searchString = text;
        Guard.Against.Null(table);
        table.ReloadServerData();
    }
}
