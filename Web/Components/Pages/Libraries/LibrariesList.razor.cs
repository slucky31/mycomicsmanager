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
    [Inject] private IDialogService DialogService { get; set; } = default!;

    private enum FormMode
    {
        Create,
        Edit
    }

    private List<LibraryUiDto> Libraries { get; } = [];

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

        var result = await LibrariesService.FilterBy("", sortColumn, sortOrder, 1, 10);

        if (result is not null && result.IsSuccess && result.Value is not null && result.Value.Items is not null)
        {
            Libraries.Clear();

            Libraries.AddRange(result.Value.Items.Select(LibraryUiDto.convert));
        }
    }

    private async Task OnClickDelete(Guid id)
    {
        var result = await LibrariesService.Delete(id.ToString());
        if (result.IsFailure)
        {
            Guard.Against.Null(result.Error);
            Guard.Against.Null(result.Error.Description);
            Snackbar.Add(result.Error.Description, Severity.Error);
        }
        else
        {
            Snackbar.Add(Msg_LibCorrectlyDeleted, Severity.Success);
            await ReloadData();
        }

    }

    private async Task<Result<LibraryUiDto>> OpenLibraryDialog(FormMode mode, LibraryUiDto? editLibrary)
    {
        DialogParameters<LibraryDialog> parameters;
        LibraryUiDto library;

        switch (mode)
        {
            case FormMode.Create:
                library = new LibraryUiDto();
                break;
            case FormMode.Edit:
                if (editLibrary is null)
                {
                    return LibrariesError.DialogError;
                }
                library = editLibrary;
                break;
            default:
                return LibrariesError.DialogError;
        }
        parameters = new DialogParameters<LibraryDialog>
        {
            { x => x.library, library }
        };

        var options = new DialogOptions { CloseButton = true, MaxWidth = MaxWidth.ExtraSmall };
        var dialogTitle = mode == FormMode.Create ? "Create" : "Edit";
        dialogTitle += " Library";

        var result = await DialogService.ShowAsync<LibraryDialog>(dialogTitle, parameters, options);
        var dialog = await result.Result;

        if (dialog is null || dialog.Data is null || dialog.Data is not LibraryUiDto)
        {
            return LibrariesError.DialogError;
        }
        if (dialog.Canceled)
        {
            return LibrariesError.DialogCanceled;
        }
        return (LibraryUiDto)dialog.Data;
    }


    private async Task CreateOrEdit(FormMode mode, LibraryUiDto? editLibrary)
    {
        if (mode == FormMode.Edit && editLibrary is null)
        {
            return;
        }

        var result = await OpenLibraryDialog(mode, editLibrary);
        if (result.IsFailure || result.Value is null)
        {
            return;
        }
        var libraryFromDialog = result.Value;

        Result<Domain.Libraries.Library> resultCreateOrEdit;
        string successMessage;

        switch (mode)
        {
            case FormMode.Create:
                resultCreateOrEdit = await LibrariesService.Create(libraryFromDialog.Name);
                successMessage = Msg_LibCorrectlyCreated;
                break;
            case FormMode.Edit:
                resultCreateOrEdit = await LibrariesService.Update(libraryFromDialog.Id.ToString(), libraryFromDialog.Name);
                successMessage = Msg_LibCorrectlyUpdated;
                break;
            default:
                return;
        }

        if (resultCreateOrEdit.IsSuccess)
        {
            Snackbar.Add(successMessage, Severity.Success);
        }
        else
        {
            Guard.Against.Null(resultCreateOrEdit.Error);
            Guard.Against.Null(resultCreateOrEdit.Error.Description);
            Snackbar.Add(resultCreateOrEdit.Error.Description, Severity.Error);
        }

        await ReloadData();

    }

}

