using Domain.Extensions;
using Domain.Primitives;

namespace Domain.Libraries;

public class Library : Entity<Guid>
{

    public string Name { get; protected set; } = String.Empty;

#pragma warning disable CA1308 // Normalize strings to uppercase
    public string RelativePath => Name.RemoveDiacritics().ToLowerInvariant();
#pragma warning restore CA1308 // Normalize strings to uppercase

    public static Library Create(string name)
    {
        var library = new Library
        {
            Id = Guid.CreateVersion7(),
            Name = name,
        };
        return library;
    }

    public void Update(string name)
    {
        Name = name;
    }
}
