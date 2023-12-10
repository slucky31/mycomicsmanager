using Domain.Primitives;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Domain.Libraries;

public class Library : Entity<string> {

    public string Name { get; private set; } = String.Empty;

    public static Library Create(string name)
    {
        var library = new Library
        {
            Id = Guid.NewGuid().ToString(),
            Name = name
        };
        return library;
    }

    public void Update(string name)
    {
        Name = name;
    }
}
