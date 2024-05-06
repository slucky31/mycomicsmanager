using Ardalis.GuardClauses;
using Domain.Libraries;
using Domain.Primitives;
using Microsoft.AspNetCore.Components;

namespace Web.Components.Pages.Libraries;

public partial class LibraryForm
{
    [Parameter]
    public string? LibraryId { get; set; }

    public string? LibraryName { get; set; }

    private List<string> Errors { get; set; } = [];

    protected override async Task OnInitializedAsync()
    {
        if (string.IsNullOrWhiteSpace(LibraryId))
        {
            LibraryName = "";
        }
        else
        {
            var result = await LibrariesService.GetById(LibraryId);
            if (result.IsSuccess)
            {                
                Guard.Against.Null(result.Value);
                LibraryName = result.Value.Name;
            }
        }
    }   

    private async Task CreateOrUpdateLibrary()
    {
        Errors.Clear();
        var result = string.IsNullOrWhiteSpace(LibraryId) ? await LibrariesService.Create(LibraryName) : await LibrariesService.Update(LibraryId, LibraryName);
        if (result.IsFailure)
        {            
            Guard.Against.Null(result.Error);
            Guard.Against.Null(result.Error.Description);
            Errors.Add(result.Error.Description);
        }
        else
        {
            MyNavigationManager.NavigateTo("/libraries/list");
        }
    }
}
