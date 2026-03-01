using Ardalis.GuardClauses;
using Domain.Libraries;
using Domain.Primitives;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using Web.Services;
using Web.Validators;

namespace Web.Components.Pages.Libraries;

public partial class LibrariesList
{
    [Inject] private ILibrariesService LibrariesService { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;


    private List<LibraryUiDto> Libraries { get; } = [];

    public static readonly string Msg_NoRecordsFound = "No matching records found";
    public static readonly string Msg_LibCorrectlyDeleted = "The library was correctly deleted";

    protected override async Task OnInitializedAsync()
    {
        await ReloadDataAsync();
    }

    private async Task ReloadDataAsync()
    {
        var sortColumn = LibrariesColumn.Name;
        var sortOrder = SortOrder.Descending;

        var result = await LibrariesService.FilterBy("", sortColumn, sortOrder, 1, 10);

        if (result is not null && result.IsSuccess && result.Value is not null && result.Value.Items is not null)
        {
            Libraries.Clear();

            Libraries.AddRange(result.Value.Items.Select(LibraryUiDto.Convert));
        }
    }

    private async Task OnClickDeleteAsync(Guid id)
    {
        var result = await LibrariesService.Delete(id.ToString());
        if (result.IsFailure)
        {
            Guard.Against.Null(result.Error);
            Guard.Against.Null(result.Error.Description);
            Snackbar.Add("Failed to delete library", Severity.Error);

        }
        else
        {
            await ReloadDataAsync();
        }
    }
}

