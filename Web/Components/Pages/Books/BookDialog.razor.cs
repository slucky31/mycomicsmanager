using Domain.Books;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using Web.Enums;
using Web.Validators;

namespace Web.Components.Pages.Books;

public partial class BookDialog
{

    [CascadingParameter]
    public IMudDialogInstance MudDialog { get; set; } = default!;

    [Inject]
    private IDialogService DialogService { get; set; } = default!;

    [Parameter]
    public FormMode FormMode { get; set; }

    [Parameter]
    public Book? Book { get; set; }

    private readonly BookValidator _bookValidator = new();

    private MudForm _form = default!;
    private BookUiDto _bookModel = new();

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        if (Book is not null)
        {
            _bookModel = BookUiDto.Convert(Book);
        }
    }

    private void Cancel()
    {
        MudDialog.Cancel();
    }

    private async Task Submit()
    {
        await _form.Validate();

        if (_form.IsValid)
        {
            MudDialog.Close(DialogResult.Ok(_bookModel));
        }

    }

    private async Task ScanISBN()
    {
        
        var options = new DialogOptions
        {
            CloseOnEscapeKey = true,
            MaxWidth = MaxWidth.Medium,
            CloseButton = false
        };

        var dialog = await DialogService.ShowAsync<ScanIsbnDialog>("", options);
        var result = await dialog.Result;

        if (result is not null && result.Data is not null && !result.Canceled)
        {
            string isbn = result.Data.ToString() ?? string.Empty;
            _bookModel.ISBN = isbn;
        }
    }

}
