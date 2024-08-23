using Domain.Extensions;
using Domain.Primitives;
using MongoDB.Bson;

namespace Domain.Libraries;

public class Library : Entity<ObjectId>
{

    public string Name { get; protected set; } = String.Empty;

    public string RelativePath => Name.RemoveDiacritics().ToUpperInvariant();

    public static Library Create(string name)
    {
        var library = new Library
        {
            Id = ObjectId.GenerateNewId(),
            Name = name,
        };
        return library;
    }

    public void Update(string name)
    {
        Name = name;
    }
}
