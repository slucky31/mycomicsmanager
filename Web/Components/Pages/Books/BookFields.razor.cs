using Application.Helpers;
using Microsoft.AspNetCore.Components;
using Web.Models;
using Web.Services;
using Web.Validators;

namespace Web.Components.Pages.Books;

public sealed partial class BookFields : IDisposable
{
    internal const string Msg_IsbnUsed = "This ISBN is already used by another book";

    [Inject] private IBooksService BooksService { get; set; } = default!;
    [Inject] private ILogger<BookFields> Logger { get; set; } = default!;

    [Parameter, EditorRequired]
    public BookUiDto Model { get; set; } = new();

    // The errors of the last validation, shown under the fields.
    [Parameter]
    public FormErrors Errors { get; set; } = FormErrors.None;

    // Called after each change of a field, for the form to validate it again.
    [Parameter]
    public EventCallback OnChanged { get; set; }

    // Told when the ISBN becomes held by another book, or free again (#1343): the form cannot be saved meanwhile.
    [Parameter]
    public EventCallback<bool> OnIsbnTakenChanged { get; set; }

    private IsbnOwnerViewModel? _isbnOwner;
    private CancellationTokenSource? _isbnCheckCts;
    private Guid? _checkedLibraryId;

    // The error of the validation first; otherwise, the other book holding the ISBN.
    private string? IsbnError => Errors.For(nameof(BookUiDto.ISBN)) ?? (_isbnOwner is null ? null : Msg_IsbnUsed);

    // On the first render and when another library is chosen, the ISBN is checked against that library's books.
    protected override Task OnParametersSetAsync()
    {
        if (_checkedLibraryId == Model.LibraryId)
        {
            return Task.CompletedTask;
        }

        _checkedLibraryId = Model.LibraryId;
        return CheckIsbnAsync();
    }

    private Task OnInputAsync(Action<string> setValue, ChangeEventArgs args)
    {
        setValue(args.Value?.ToString() ?? string.Empty);
        return NotifyChangedAsync();
    }

    // While the ISBN is typed, the other book is hidden: it is checked again once the field is left.
    private async Task OnIsbnInputAsync(ChangeEventArgs args)
    {
        Model.ISBN = args.Value?.ToString() ?? string.Empty;
        await SetIsbnOwnerAsync(null);
        await NotifyChangedAsync();
    }

    private Task OnOneShotChangedAsync(ChangeEventArgs args)
    {
        Model.SetOneShot(args.Value is true);
        return NotifyChangedAsync();
    }

    private Task NotifyChangedAsync() => OnChanged.InvokeAsync();

    private string? Describe(string id, string property) => FormFieldIds.DescribedBy(id, Errors.For(property), null);

    // Whether another book of the same type already holds the ISBN; a late answer for an older ISBN is dropped.
    private async Task CheckIsbnAsync()
    {
        var isbn = Model.ISBN ?? string.Empty;
        var libraryId = Model.LibraryId;
        if (libraryId == Guid.Empty || !IsbnHelper.IsValidISBN(isbn))
        {
            await SetIsbnOwnerAsync(null);
            return;
        }

        if (_isbnCheckCts is not null)
        {
            await _isbnCheckCts.CancelAsync();
            _isbnCheckCts.Dispose();
        }

        _isbnCheckCts = new CancellationTokenSource();
        var token = _isbnCheckCts.Token;

        try
        {
            var result = await BooksService.GetIsbnOwner(libraryId, Model.Id, isbn, token);
            if (token.IsCancellationRequested || !string.Equals(isbn, Model.ISBN, StringComparison.Ordinal) || libraryId != Model.LibraryId)
            {
                return;
            }

            if (result.IsSuccess)
            {
                await SetIsbnOwnerAsync(result.Value is [var owner, ..] ? IsbnOwnerViewModel.From(owner) : null);
            }
            else
            {
                // The save still refuses a duplicate: the form only loses the early warning.
                Logger.LogWarning("Unable to check the ISBN {Isbn}: {Error}", isbn, result.Error?.Description);
            }
        }
        catch (OperationCanceledException)
        {
            // A newer ISBN is being checked, or the form was left.
        }
    }

    private async Task SetIsbnOwnerAsync(IsbnOwnerViewModel? owner)
    {
        var wasTaken = _isbnOwner is not null;
        _isbnOwner = owner;
        if (wasTaken != (owner is not null))
        {
            await OnIsbnTakenChanged.InvokeAsync(owner is not null);
        }
    }

    public void Dispose()
    {
        _isbnCheckCts?.Cancel();
        _isbnCheckCts?.Dispose();
    }
}
