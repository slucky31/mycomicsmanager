using Ardalis.GuardClauses;
using Domain.Libraries;
using Domain.Primitives;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using Web.Services;
using Web.Validators;

namespace Web.Components.Pages.Libraries;

#pragma warning disable CA1515 // Consider making public types internal (bug roselyn analyser : https://github.com/dotnet/roslyn-analyzers/issues/7473)
public partial class LibrairiesList
#pragma warning restore CA1515 // Consider making public types internal
{
    [Inject] private ILibrariesService LibrariesService { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;
    [Inject] private NavigationManager MyNavigationManager { get; set; } = default!;
    [Inject] private IDialogService DialogService { get; set; } = default!;

    private enum FormMode
    {
        Create,
        Edit
    }

    private List<LibraryUiDto> Libraries { get; } = new();

    private string searchString = "";

    public static readonly string Msg_NoRecordsFound = "No matching records found";
    public static readonly string Msg_LibCorrectlyDeleted = "The library was correctly deleted";
    public static readonly string Msg_LibCorrectlyCreated = "The library was correctly created";
    public static readonly string Msg_LibCorrectlyUpdated = "The library was correctly updated";

    protected override async Task OnInitializedAsync()
    {
        await ReloadData();
    }

    private async Task ReloadData()
    {
        var sortColumn = LibrariesColumn.Name;
        var sortOrder = SortOrder.Descending;

        var result = await LibrariesService.FilterBy(searchString, sortColumn, sortOrder, 1, 10);

        Libraries.Clear();
        if (result is not null && result.IsSuccess && result.Value is not null && result.Value.Items is not null)
        {
            var items = result.Value.Items?
                .Select(library => LibraryUiDto.convert(library))
                .ToList();

            if (items is not null)
            {
                Libraries.AddRange(items);
            }
        }
    }

    private async Task OnClickDelete(Guid id)
    {
        var result = await LibrariesService.Delete(id.ToString());
        Guard.Against.Null(result);
        if (result.IsFailure)
        {
            Guard.Against.Null(result.Error);
            Guard.Against.Null(result.Error.Description);
            Snackbar.Add(result.Error.Description, Severity.Error);
        }
        else
        {
            Snackbar.Add(Msg_LibCorrectlyDeleted, Severity.Success);
        }
        await ReloadData();
    }


    private async Task CreateOrEdit(FormMode mode, LibraryUiDto? editLibrary)
    {
        DialogParameters<LibraryDialog> parameters;
        switch (mode)
        {
            case FormMode.Create:
                parameters = new DialogParameters<LibraryDialog>
                {
                    { x => x.library, new LibraryUiDto() }
                };
                break;
            case FormMode.Edit:
                Guard.Against.Null(editLibrary);
                parameters = new DialogParameters<LibraryDialog>
                {
                    { x => x.library, editLibrary }
                };
                break;
            default:
                return;
        }

        var options = new DialogOptions() { CloseButton = true, MaxWidth = MaxWidth.ExtraSmall };
        var dialog = await DialogService.ShowAsync<LibraryDialog>("Create", parameters, options);
        var result = await dialog.Result;

        if (result is null)
        {
            return;
        }

        if (result.Canceled || result.Data is null)
        {
            return;
        }

        var library = (LibraryUiDto)result.Data;
        Result<Library> resultCreateOrEdit;
        string SuccesMessage;

        switch (mode)
        {
            case FormMode.Create:
                resultCreateOrEdit = await LibrariesService.Create(library.Name);
                SuccesMessage = Msg_LibCorrectlyCreated;
                break;
            case FormMode.Edit:
                resultCreateOrEdit = await LibrariesService.Update(library.Id.ToString(), library.Name);
                SuccesMessage = Msg_LibCorrectlyUpdated;
                break;
            default:
                return;
        }

        if (resultCreateOrEdit.IsSuccess)
        {
            Snackbar.Add(SuccesMessage, Severity.Success);
        }
        else
        {
            Guard.Against.Null(resultCreateOrEdit.Error);
            Guard.Against.Null(resultCreateOrEdit.Error.Description);
            Snackbar.Add(resultCreateOrEdit.Error.Description, Severity.Error);
        }

        await ReloadData();
    }

    private async void OnSearch(string text)
    {
        searchString = text;
        await ReloadData();
    }
}

