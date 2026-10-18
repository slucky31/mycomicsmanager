using Domain.Primitives;

namespace Domain.Errors;

public static class LegacyMigrationError
{
    public static readonly TError BadRequest = new("LegacyMigration.BadRequest", "The number of books to move is invalid.");
    public static readonly TError SourceNotFound = new("LegacyMigration.SourceNotFound", "The old library folder (\"legacy\" in the temp folder) does not exist.");
    public static readonly TError ImportedLibraryNotFound = new("LegacyMigration.ImportedLibraryNotFound", "The digital library \"Imported\" does not exist.");
}
