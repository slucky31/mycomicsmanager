using MudBlazor;
using Web.Components.Pages.Dialogs;

namespace Web.Extensions;

public static class DialogServiceExtensions
{
    public static async Task<bool> ShowConfirmationAsync(
        this IDialogService dialogService,
        string title,
        string message,
        string actionText = "Confirm",
        Color color = Color.Error)
    {
        var parameters = new DialogParameters<ConfirmationDialog>
        {
            { x => x.ConfirmationMessage, message },
            { x => x.ActionText, actionText },
            { x => x.ColorConfirmButton, color }
        };

        var options = new DialogOptions
        {
            CloseOnEscapeKey = true,
            MaxWidth = MaxWidth.ExtraSmall,
            CloseButton = false
        };

        var dialog = await dialogService.ShowAsync<ConfirmationDialog>(title, parameters, options);
        var result = await dialog.Result;

        return result is not null && result.Data is not null && !result.Canceled;
    }
}
