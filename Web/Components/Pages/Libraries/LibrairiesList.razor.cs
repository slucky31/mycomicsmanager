using Application.Libraries.Delete;
using Application.Libraries.List;
using Ardalis.GuardClauses;
using Domain.Libraries;
using Domain.Primitives;
using MongoDB.Bson;
using MudBlazor;

namespace Web.Components.Pages.Libraries;

public partial class LibrairiesList
{
    private MudTable<Library>? table;
    private string searchString = "";

    private async Task<TableData<Library>> ServerReload(TableState state)
    {
        LibrariesColumn sortColumn = LibrariesColumn.Id;
        if (state.SortLabel == "name_field")
        {
            sortColumn = LibrariesColumn.Name;
        }

        SortOrder sortOrder = SortOrder.Ascending;
        if (state.SortDirection == SortDirection.Descending)
        {
            sortOrder = SortOrder.Descending;
        }

        var query = new GetLibrariesQuery(searchString, sortColumn, sortOrder, state.Page + 1, state.PageSize);

        var result = await Mediator.Send(query);
        if (result is not null && result.Items is not null)
        {
            return new TableData<Library> { TotalItems = result.TotalCount, Items = result.Items.ToList() };
        }

        return new TableData<Library> { TotalItems = 0, Items = new List<Library>() };
    }

    private async Task OnClickDelete(ObjectId id)
    {
        var query = new DeleteLibraryCommand(id);

        var result = await Mediator.Send(query);

        Guard.Against.Null(result);
        if (result.IsFailure)
        {            
            Guard.Against.Null(result.Error);            
            Snackbar.Add(result.Error.Description, Severity.Error);
        }
        else
        {
            Snackbar.Add("The library was correctly deleted", Severity.Success);
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
