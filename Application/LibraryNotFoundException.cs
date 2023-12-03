using Domain.Libraries;

namespace Application;

public class LibraryNotFoundException : Exception
{
    public LibraryNotFoundException()
    {
    }

    public LibraryNotFoundException(LibraryId libraryId) : base($"The library with the ID = {libraryId.Value} was not found")
    {
    }

    public LibraryNotFoundException(string message) : base(message)
    {
    }

    public LibraryNotFoundException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
