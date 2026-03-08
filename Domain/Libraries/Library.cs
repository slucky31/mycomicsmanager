using Domain.Books;
using Domain.Extensions;
using Domain.Primitives;

namespace Domain.Libraries;

public class Library : Entity<Guid>
{
    public string Name { get; protected set; } = string.Empty;

    public string Color { get; protected set; } = string.Empty;

    public string Icon { get; protected set; } = string.Empty;

    public LibraryBookType BookType { get; protected set; }

    public Guid UserId { get; protected set; }

    public BookSortOrder DefaultBookSortOrder { get; protected set; } = BookSortOrder.IdDesc;

    public string RelativePath => Name.RemoveDiacritics().ToUpperInvariant();

    private readonly List<Book> _books = [];
    public IReadOnlyList<Book> Books => _books.AsReadOnly();

    protected Library() { }

    public static Result<Library> Create(
        string name,
        string color,
        string icon,
        LibraryBookType bookType,
        Guid userId)
    {

        if (!Enum.IsDefined(bookType))
        {
            return LibrariesError.BadRequest;
        }

        if (string.IsNullOrWhiteSpace(name) || name.Length > LibraryConstants.MaxNameLength)
        {
            return LibrariesError.BadRequest;
        }

        if (string.IsNullOrWhiteSpace(color) || color.Length > LibraryConstants.MaxColorLength)
        {
            return LibrariesError.BadRequest;
        }

        if (string.IsNullOrWhiteSpace(icon) || icon.Length > LibraryConstants.MaxIconLength)
        {
            return LibrariesError.BadRequest;
        }

        if (userId == Guid.Empty)
        {
            return LibrariesError.BadRequest;
        }

        var library = new Library
        {
            Id = Guid.CreateVersion7(),
            Name = name,
            Color = color,
            Icon = icon,
            BookType = bookType,
            UserId = userId
        };

        return library;
    }

    public Result Update(string color, string icon)
    {
        if (string.IsNullOrWhiteSpace(color) || color.Length > LibraryConstants.MaxColorLength)
        {
            return LibrariesError.BadRequest;
        }

        if (string.IsNullOrWhiteSpace(icon) || icon.Length > LibraryConstants.MaxIconLength)
        {
            return LibrariesError.BadRequest;
        }

        Color = color;
        Icon = icon;

        return Result.Success();
    }

    public Result UpdateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Length > LibraryConstants.MaxNameLength)
        {
            return LibrariesError.BadRequest;
        }

        Name = name;

        return Result.Success();
    }

    public Result CanContain(Book book)
    {
        if (BookType == LibraryBookType.Physical && book is PhysicalBook)
        {
            return Result.Success();
        }

        if (BookType == LibraryBookType.Digital && book is DigitalBook)
        {
            return Result.Success();
        }

        return LibrariesError.BookTypeMismatch;
    }
}
