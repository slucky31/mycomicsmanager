namespace Domain.Libraries;

public static class LibraryConstants
{
    public const int MaxNameLength = 200;
    public const int MaxColorLength = 7;
    public const int MaxIconLength = 100;

    // The digital library the feed imports deposit their files in; it cannot be deleted or renamed.
    public const string ImportedName = "Imported";
}
