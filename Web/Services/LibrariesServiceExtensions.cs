namespace Web.Services;

public static class LibrariesServiceExtensions
{
    // The name of the library a book is added to ("Adding to Read Books"), null when it cannot be read.
    public static async Task<string?> NameOfAsync(this ILibrariesService librariesService, Guid? libraryId)
    {
        ArgumentNullException.ThrowIfNull(librariesService);

        if (libraryId is not { } id)
        {
            return null;
        }

        var result = await librariesService.GetById(id.ToString());
        return result.IsSuccess ? result.Value?.Name : null;
    }
}
