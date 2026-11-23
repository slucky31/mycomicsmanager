using Application.Books;
using Application.ComicInfoSearch;
using Application.Interfaces;
using Microsoft.Extensions.Options;

namespace Application.UnitTests.Books;

// The cover store of the handlers, on a substitute of Cloudinary, with the covers in the "covers" folder.
internal static class TestCoverStores
{
    public const string Folder = "covers";

    public static BookCoverStore Create(ICloudinaryService cloudinary) =>
        new(cloudinary, Options.Create(new CloudinarySettings { Folder = Folder }), NullLogger<BookCoverStore>.Instance);
}
