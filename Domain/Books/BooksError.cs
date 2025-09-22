using Domain.Primitives;

namespace Domain.Books;

public static class BooksError
{
    public static readonly TError BadRequest = new("BOK400", "Verify the request parameters.");
    public static readonly TError NotFound = new("BOK404", "Book not found");
    public static readonly TError Duplicate = new("BOK409", "A book is already created with this ISBN");
    public static readonly TError ValidationError = new("BOK504", "The parameters were not validated");
    public static readonly TError InvalidISBN = new("BOK505", "The ISBN format is invalid");
    public static readonly TError DialogError = new("BOK701", "Dialog initialisation error");
    public static readonly TError DialogCanceled = new("BOK702", "Dialog canceled by user");
    public static readonly TError ScanError = new("BOK801", "ISBN scanning failed");
    public static readonly TError CameraError = new("BOK802", "Camera access denied or unavailable");
}