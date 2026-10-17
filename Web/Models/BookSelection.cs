using System.Collections.Immutable;

namespace Web.Models;

// The books chosen in a library to move them at once (#1150); None while no book is being chosen.
public sealed class BookSelection
{
    private readonly ImmutableHashSet<Guid> _ids;

    private BookSelection(bool isActive, ImmutableHashSet<Guid> ids)
    {
        IsActive = isActive;
        _ids = ids;
    }

    public static BookSelection None { get; } = new(false, []);

    // Chosen from the actions of a book: that book is the first one checked.
    public static BookSelection Start(Guid? firstBookId = null) =>
        new(true, firstBookId is { } id ? [id] : []);

    public bool IsActive { get; }

    public int Count => _ids.Count;

    public IReadOnlyList<Guid> BookIds => [.. _ids];

    public string Label => Count switch
    {
        0 => "Choose books",
        1 => "1 book selected",
        _ => $"{Count} books selected",
    };

    public bool IsChecked(BookListItemViewModel book) => book is not null && _ids.Contains(book.Id);

    public string AriaChecked(BookListItemViewModel book) => IsChecked(book) ? "true" : "false";

    public BookSelection Toggle(Guid bookId)
    {
        if (!IsActive)
        {
            return this;
        }

        var withoutBook = _ids.Remove(bookId);
        return new(true, withoutBook.Count < _ids.Count ? withoutBook : _ids.Add(bookId));
    }

    // Every book on screen is checked, those already checked stay so.
    public BookSelection With(IEnumerable<Guid> bookIds) => !IsActive ? this : new(true, _ids.Union(bookIds));
}
