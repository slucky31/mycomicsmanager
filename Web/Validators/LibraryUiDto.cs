using Domain.Extensions;
using Domain.Libraries;
using Domain.Primitives;
using MudBlazor;

namespace Web.Validators;

public class LibraryUiDto : Entity<Guid>
{
    [Label("Name")]
    public string Name { get; set; } = string.Empty;

    [Label("Color")]
    public string Color { get; set; } = "#5C6BC0";

    [Label("Icon")]
    public string Icon { get; set; } = "CollectionsBookmark";

    [Label("Type")]
    public LibraryBookType BookType { get; set; } = LibraryBookType.Physical;

    [Label("RelativePath")]
    public string RelativePath => Name.RemoveDiacritics().ToUpperInvariant();

    public static LibraryUiDto Convert(Library library) => new()
    {
        Id = library.Id,
        Name = library.Name,
        Color = library.Color,
        Icon = library.Icon,
        BookType = library.BookType,
        CreatedOnUtc = library.CreatedOnUtc,
        ModifiedOnUtc = library.ModifiedOnUtc
    };
}
