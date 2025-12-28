using Ardalis.GuardClauses;
using Domain.Extensions;
using Domain.Libraries;
using Domain.Primitives;
using MudBlazor;

namespace Web.Validators;

public class LibraryUiDto : Entity<Guid>
{
    [Label("Name")]
    public string Name { get; set; } = string.Empty;

    [Label("RelativePath")]
    public string RelativePath => Name.RemoveDiacritics().ToUpperInvariant();

    public static LibraryUiDto convert(Library library)
    {
        Guard.Against.Null(library);

        return new LibraryUiDto
        {
            Name = library.Name,
            Id = library.Id,
            CreatedOnUtc = library.CreatedOnUtc,
            ModifiedOnUtc = library.ModifiedOnUtc
        };
    }
}
