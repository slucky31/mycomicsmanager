using Domain.Primitives;

namespace Domain.Books;

public static class BooksError
{
    public static readonly TError BadRequest = new("BOK400", "Verify the request parameters.");
    public static readonly TError NotFound = new("BOK404", "Book not found");
    public static readonly TError Duplicate = new("BOK409", "A book is already created with this ISBN");
    public static readonly TError ValidationError = new("BOK504", "The parameters were not validated");
    public static readonly TError InvalidISBN = new("BOK505", "The ISBN format is invalid");
    public static readonly TError InvalidRating = new("BOK506", "The rating must be between 1 and 5");
    public static readonly TError DialogError = new("BOK701", "Dialog initialisation error");
    public static readonly TError DialogCanceled = new("BOK702", "Dialog canceled by user");
    public static readonly TError ScanError = new("BOK801", "ISBN scanning failed");
    public static readonly TError CameraError = new("BOK802", "Camera access denied or unavailable");
    public static readonly TError AlreadyInLibrary = new("BOK410", "The book is already in this library");
    public static readonly TError FileNotFound = new("BOK411", "The book file was not found");
    public static readonly TError FileAlreadyExists = new("BOK412", "A file with the same name already exists in the target library");
    public static readonly TError NotDigital = new("BOK413", "Only digital books can be read");
    public static readonly TError PageNotFound = new("BOK414", "The requested page does not exist");
}
