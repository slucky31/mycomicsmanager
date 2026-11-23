using Application.Books;

namespace Application.UnitTests.Books;

public sealed class BookCoversTests
{
    private static readonly Guid s_bookId = new(0x0193a6c4, 0x1c2b, 0x7d3e, 0x8f, 0x40, 0x5a, 0x6b, 0x7c, 0x8d, 0x9e, 0x0f);

    [Theory]
    [InlineData("https://res.cloudinary.com/mcm/image/upload/v1712345678/covers/9782205050196.jpg", "covers/9782205050196")]
    [InlineData("https://res.cloudinary.com/mcm/image/upload/c_fill,ar_2:3,w_216,f_auto,q_auto/digital-covers/9782756015996.webp", "digital-covers/9782756015996")]
    [InlineData("https://res.cloudinary.com/mcm/image/upload/covers-dev/digital-abc", "covers-dev/digital-abc")]
    [InlineData("https://res.cloudinary.com/mcm/image/upload/covers/L%27%C3%A9t%C3%A9.jpg", "covers/L'été")]
    public void PublicIdOf_Should_ReadThePublicIdOfTheImage_WithoutTransformationVersionOrExtension(string link, string publicId) =>
        BookCovers.PublicIdOf(link).Should().Be(publicId);

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("/covers/local.jpg")]
    [InlineData("https://covers.openlibrary.org/b/id/1-L.jpg")]
    [InlineData("https://res.cloudinary.com/mcm/video/upload/covers/1.mp4")]
    [InlineData("https://res.cloudinary.com/mcm/image/upload/v1712345678/")]
    public void PublicIdOf_Should_ReturnNull_WhenTheLinkIsNotACloudinaryImage(string? link)
    {
        BookCovers.PublicIdOf(link).Should().BeNull();
        BookCovers.IsCloudinary(link).Should().BeFalse();
    }

    [Theory]
    [InlineData("https://res.cloudinary.com/mcm/image/upload/v1/covers/0193a6c4-1c2b-7d3e-8f40-5a6b7c8d9e0f.webp", "covers", true)]
    [InlineData("https://res.cloudinary.com/mcm/image/upload/v1/covers/0193a6c4-1c2b-7d3e-8f40-5a6b7c8d9e0f.webp", "covers-dev", false)]
    [InlineData("https://res.cloudinary.com/mcm/image/upload/v1/covers/9782205050196.jpg", "covers", false)]
    [InlineData("https://books.google.com/cover.jpg", "covers", false)]
    public void IsStoredFor_Should_TellWhetherTheLinkIsTheImageOfTheBook(string link, string folder, bool expected) =>
        BookCovers.IsStoredFor(link, folder, s_bookId).Should().Be(expected);
}
