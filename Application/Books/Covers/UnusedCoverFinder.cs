using Application.Interfaces;
using Domain.Books;
using Domain.Primitives;

namespace Application.Books.Covers;

/// <summary>
/// The images of the cover folders that no book uses (#1225): the folder of the settings, and the old digital-covers.
/// Nothing is found while a book still uses a shared image: moving the covers comes first.
/// </summary>
public sealed class UnusedCoverFinder(IBookRepository bookRepository, ICloudinaryService cloudinary, BookCoverStore coverStore)
{
    public async Task<Result<IReadOnlyList<CloudinaryImage>>> FindAsync(CancellationToken ct)
    {
        var books = await bookRepository.ListAsync(ct);
        if (books.Exists(coverStore.IsShared))
        {
            return BooksError.CoversNotMoved;
        }

        var used = books.Select(b => BookCovers.PublicIdOf(b.ImageLink)).OfType<string>().ToHashSet(StringComparer.Ordinal);
        var unused = new List<CloudinaryImage>();
        foreach (var folder in coverStore.Folders)
        {
            var listResult = await cloudinary.ListImagesAsync(folder, ct);
            if (listResult.IsFailure)
            {
                return listResult.Error!;
            }

            unused.AddRange(listResult.Value!.Where(image => !used.Contains(image.PublicId)));
        }

        return unused;
    }
}
