using Domain.Primitives;
using MongoDB.Bson;

namespace Domain.Libraries;

public class Library : Entity<LibraryId> {

    public string Name { get; protected set; } = String.Empty;

    public static Library Create(string name, LibraryId? id = null)
    {
        var library = new Library
        {
            Id = id ?? new LibraryId(new ObjectId()),
            Name = name
        };
        return library;
    }

}

public record LibraryId(ObjectId Id) : StronglyObjectIdTypedId(Id);
