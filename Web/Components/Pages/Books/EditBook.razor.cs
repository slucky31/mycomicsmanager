using Domain.Books;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using Web.Extensions;
using Web.Models;
using Web.Services;
using Web.Validators;

namespace Web.Components.Pages.Books;

// The form to edit a book and its readings (E1, E2, T11).
public sealed partial class EditBook : IDisposable
{
    private const string Msg_LoadFailed = "Unable to load the book";
    private const string Msg_SaveFailed = "Unable to save the changes";
    private const string Msg_DeleteReadingFailed = "Unable to delete the reading";

    [Inject] private NavigationManager NavigationManager { get; set; } = default!;
    [Inject] private IBooksService BooksService { get; set; } = default!;
    [Inject] private ILibrariesService LibrariesService { get; set; } = default!;
    [Inject] private IDialogService DialogService { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;
    [Inject] private ILogger<EditBook> Logger { get; set; } = default!;

    [Parameter]
    public string BookId { get; set; } = string.Empty;

    // The screen the book was opened from (#1261), kept for the book page this form goes back to.
    [SupplyParameterFromQuery(Name = ReturnLinks.QueryName)]
    public string? From { get; set; }

    private readonly BookValidator _validator = new();
    private readonly CancellationTokenSource _disposalCts = new();

    private EditBookViewModel? _view;
    private BookUiDto? _model;
    private string? _libraryName;
    private FormErrors _errors = FormErrors.None;
    private bool _submitted;
    private bool _isLoading = true;
    private bool _isSaving;
    private Guid? _deletingReadingId;

    private string BookHref => ReturnLinks.With($"/books/{BookId}", From);

    protected override async Task OnInitializedAsync()
    {
        try
        {
            var book = await LoadBookAsync();
            if (book is not null)
            {
                _model = BookUiDto.Convert(book);
                await LoadLibraryNameAsync(book.LibraryId);
            }
        }
        finally
        {
            _isLoading = false;
        }
    }

    // The book, or null after showing why it could not be loaded; what is shown only changes on a success.
    private async Task<Book?> LoadBookAsync()
    {
        try
        {
            var result = await BooksService.GetById(BookId);
            if (result.IsSuccess && result.Value is not null)
            {
                _view = EditBookViewModel.From(result.Value);
                return result.Value;
            }

            Logger.LogError("Unable to load the book {BookId}: {Error}", BookId, result.Error?.Description);
        }
        catch (Exception ex) when (ex is OperationCanceledException or InvalidOperationException)
        {
            Logger.LogError(ex, "Unexpected error while loading book {BookId}", BookId);
        }

        Snackbar.Add(Msg_LoadFailed, Severity.Error);
        return null;
    }

    // "Library" is shown when the name cannot be loaded.
    private async Task LoadLibraryNameAsync(Guid libraryId)
    {
        var result = await LibrariesService.GetById(libraryId.ToString());
        if (result.IsSuccess && result.Value is not null)
        {
            _libraryName = result.Value.Name;
        }
        else
        {
            Logger.LogWarning("Unable to load the library {LibraryId} of book {BookId}: {Error}", libraryId, BookId, result.Error?.Description);
        }
    }

    // Once the form was submitted, its errors follow what is typed.
    private void Revalidate()
    {
        if (_submitted && _model is not null)
        {
            _errors = FormErrors.From(_validator.Validate(_model));
        }
    }

    private async Task SaveAsync()
    {
        if (_isSaving || _model is null)
        {
            return;
        }

        _submitted = true;
        _errors = FormErrors.From(await _validator.ValidateAsync(_model, _disposalCts.Token));
        if (_errors.Any)
        {
            return;
        }

        _isSaving = true;
        try
        {
            await UpdateAsync(_model);
        }
        catch (Exception ex) when (ex is OperationCanceledException or InvalidOperationException)
        {
            Logger.LogError(ex, "Unexpected error while updating book {BookId}", BookId);
            Snackbar.Add(Msg_SaveFailed, Severity.Error);
        }
        finally
        {
            _isSaving = false;
        }
    }

    private async Task UpdateAsync(BookUiDto model)
    {
        var request = new UpdateBookRequest(
            model.Id.ToString(),
            model.Serie,
            model.Title,
            model.ISBN ?? string.Empty,
            model.VolumeNumber,
            model.ImageLink,
            model.Authors,
            model.Publishers,
            model.PublishDate,
            model.NumberOfPages);

        var result = await BooksService.Update(request, _disposalCts.Token);
        if (result.IsSuccess)
        {
            Snackbar.Add($"{model.Title} was saved", Severity.Success);
            NavigationManager.NavigateTo(BookHref);
            return;
        }

        Logger.LogError("Unable to update the book {BookId}: {Error}", BookId, result.Error?.Description);
        Snackbar.Add(result.Error == BooksError.Duplicate ? "Another book already has this ISBN" : Msg_SaveFailed, Severity.Error);
    }

    // After a confirmation; the fields being edited are kept, only the readings are loaded again.
    private async Task DeleteReadingAsync(EditBookReading reading)
    {
        if (_deletingReadingId is not null)
        {
            return;
        }

        _deletingReadingId = reading.Id;
        try
        {
            var confirmed = await DialogService.ShowConfirmationAsync("Delete this reading?", reading.DeleteMessage, "Delete reading");
            if (!confirmed)
            {
                return;
            }

            var result = await BooksService.DeleteReadingDate(BookId, reading.Id.ToString(), _disposalCts.Token);
            if (result.IsSuccess)
            {
                await LoadBookAsync();
                return;
            }

            Logger.LogError("Unable to delete the reading {ReadingId} of book {BookId}: {Error}", reading.Id, BookId, result.Error?.Description);
            Snackbar.Add(Msg_DeleteReadingFailed, Severity.Error);
        }
        catch (Exception ex) when (ex is OperationCanceledException or InvalidOperationException)
        {
            Logger.LogError(ex, "Unexpected error while deleting the reading {ReadingId} of book {BookId}", reading.Id, BookId);
            Snackbar.Add(Msg_DeleteReadingFailed, Severity.Error);
        }
        finally
        {
            _deletingReadingId = null;
        }
    }

    public void Dispose()
    {
        _disposalCts.Cancel();
        _disposalCts.Dispose();
    }
}
