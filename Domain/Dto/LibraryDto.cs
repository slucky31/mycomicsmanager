using Ardalis.GuardClauses;
using Domain.Libraries;
using MongoDB.Bson;
using Library = Domain.Libraries.Library;

namespace Domain.Dto;
public class LibraryDto : EntityDto
{
    public string Name { get; protected set; } = String.Empty;

    public static LibraryDto Create(Library library)
    {
        Guard.Against.Null(library);    
        
        return new LibraryDto
        {
            Id = new ObjectId(library.Id?.Id.ToString()),
            Name = library.Name,
            CreatedOnUtc = library.CreatedOnUtc,
            ModifiedOnUtc = library.ModifiedOnUtc
        };
    }

    public Library ToLibrary()
    {
        var library = Library.Create(Name, new LibraryId(Id));        
        library.CloneAuditable(this);
        return library;                
    }

    public void Update(string name)
    {
        Name = name;
    }
}
