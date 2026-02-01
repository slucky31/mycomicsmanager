using Domain.Extensions;
using Domain.Primitives;

namespace Domain.Libraries;

public class Library : Entity<Guid>
{

    public string Name { get; protected set; } = string.Empty;

    public string RelativePath => Name.RemoveDiacritics().ToUpperInvariant();

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
