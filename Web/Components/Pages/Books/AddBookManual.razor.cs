using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Web.Models;
using Web.Services;

namespace Web.Components.Pages.Books;

// The ISBN typed by hand, when the barcode won't scan (A4).
public partial class AddBookManual
{
    private const string InputId = "isbn";

    [Inject] private NavigationManager NavigationManager { get; set; } = default!;
    [Inject] private ILibrariesService LibrariesService { get; set; } = default!;
    [Inject] private ILogger<AddBookManual> Logger { get; set; } = default!;

    [SupplyParameterFromQuery]
    public string? LibraryId { get; set; }

    private ElementReference _input;
    private Guid? _libraryId;
    private string? _libraryName;
    private string _isbn = string.Empty;
    private string? _error;

    protected override async Task OnInitializedAsync()
    {
        _libraryId = AddBookLinks.ParseLibraryId(LibraryId);
        _libraryName = await LibrariesService.NameOfAsync(_libraryId);
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            try
            {
                await _input.FocusAsync();
            }
            catch (JSException ex)
            {
                Logger.LogDebug(ex, "Unable to focus the ISBN field");
            }
        }
    }

    private void ClearError() => _error = null;

    private void LookUp()
    {
        var (isbn, error) = IsbnEntry.Check(_isbn);
        _error = error;
        if (isbn is not null)
        {
            NavigationManager.NavigateTo(AddBookLinks.LookUp(isbn, _libraryId));
        }
    }
}
