using Application.Books;
using Application.ComicInfoSearch;
using Application.Interfaces;
using Domain.Books;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Application.UnitTests.Books;

public sealed class BookCoverStoreTests
{
    private readonly ICloudinaryService _cloudinary = Substitute.For<ICloudinaryService>();
    private readonly BookCoverStore _store;

    public BookCoverStoreTests() => _store = TestCoverStores.Create(_cloudinary);

    private static PhysicalBook Book(string imageLink) =>
        PhysicalBook.Create(new BookMetadata("Blacksad", "Arctic Nation", "9782205050196", ImageLink: imageLink), Guid.CreateVersion7()).Value!;

    private static string OwnImage(Book book) => $"https://res.cloudinary.com/mcm/image/upload/v1/covers/{book.Id}.webp";

    [Theory]
    [InlineData("https://books.google.com/cover.jpg", true)]
    [InlineData("http://books.google.com/cover.jpg", true)]
    [InlineData("https://res.cloudinary.com/mcm/image/upload/v1/covers/9782205050196.jpg", true)]
    [InlineData("", false)]
    [InlineData("/covers/local.jpg", false)]
    [InlineData("ftp://example.com/cover.jpg", false)]
    public void NeedsCopy_Should_TellWhetherTheCoverIsAWebImageThatIsNotTheBooksOwn(string imageLink, bool expected) =>
        _store.NeedsCopy(Book(imageLink)).Should().Be(expected);

    [Fact]
    public void NeedsCopy_Should_ReturnFalse_WhenTheCoverIsAlreadyTheImageOfTheBook()
    {
        var book = Book(string.Empty);
        book.UseStoredCover(OwnImage(book));

        _store.NeedsCopy(book).Should().BeFalse();
        _store.IsShared(book).Should().BeFalse();
    }

    [Theory]
    [InlineData("https://res.cloudinary.com/mcm/image/upload/v1/covers/9782205050196.jpg", true)]
    [InlineData("https://res.cloudinary.com/mcm/image/upload/v1/digital-covers/digital-1.webp", true)]
    [InlineData("https://books.google.com/cover.jpg", false)]
    [InlineData("", false)]
    public void IsShared_Should_TellWhetherTheCoverIsACloudinaryImageOfAnotherName(string imageLink, bool expected) =>
        _store.IsShared(Book(imageLink)).Should().Be(expected);

    [Theory]
    [InlineData("covers", new[] { "covers", "digital-covers" })]
    [InlineData("digital-covers", new[] { "digital-covers" })]
    public void Folders_Should_AddTheOldDigitalCoversFolder_ToTheFolderOfTheSettings(string folder, string[] expected)
    {
        var store = new BookCoverStore(_cloudinary, Options.Create(new CloudinarySettings { Folder = folder }), NullLogger<BookCoverStore>.Instance);

        store.Folders.Should().Equal(expected);
    }

    [Fact]
    public async Task StoreAsync_Should_CopyTheCoverToTheImageOfTheBook()
    {
        var book = Book("https://books.google.com/cover.jpg");
        var stored = new Uri(OwnImage(book));
        _cloudinary.UploadImageFromUrlAsync(new Uri("https://books.google.com/cover.jpg"), TestCoverStores.Folder, book.Id.ToString(), Arg.Any<CancellationToken>())
            .Returns(new CloudinaryUploadResult(stored, $"covers/{book.Id}", true, null));

        var changed = await _store.StoreAsync(book, TestContext.Current.CancellationToken);

        changed.Should().BeTrue();
        book.ImageLink.Should().Be(stored.ToString());
    }

    [Fact]
    public async Task StoreAsync_Should_KeepTheCover_WhenItCannotBeCopied()
    {
        var book = Book("https://books.google.com/cover.jpg");
        _cloudinary.UploadImageFromUrlAsync(Arg.Any<Uri>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new CloudinaryUploadResult(new Uri("https://res.cloudinary.com/mcm/partial.jpg"), null, false, "upload error"));

        var changed = await _store.StoreAsync(book, TestContext.Current.CancellationToken);

        changed.Should().BeFalse();
        book.ImageLink.Should().Be("https://books.google.com/cover.jpg");
    }

    [Fact]
    public async Task StoreAsync_Should_UploadNothing_WhenTheBookHasNoWebCover()
    {
        var changed = await _store.StoreAsync(Book(string.Empty), TestContext.Current.CancellationToken);

        changed.Should().BeFalse();
        await _cloudinary.DidNotReceiveWithAnyArgs().UploadImageFromUrlAsync(default!, default!, default!, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task UploadFileAsync_Should_ReturnTheLinkOfTheImage_NamedAfterTheBook()
    {
        var bookId = Guid.CreateVersion7();
        var stored = new Uri($"https://res.cloudinary.com/mcm/image/upload/v1/covers/{bookId}.webp");
        _cloudinary.UploadImageFromFileAsync("/tmp/page-001.webp", TestCoverStores.Folder, bookId.ToString(), Arg.Any<CancellationToken>())
            .Returns(new CloudinaryUploadResult(stored, $"covers/{bookId}", true, null));

        var link = await _store.UploadFileAsync(bookId, "/tmp/page-001.webp", TestContext.Current.CancellationToken);

        link.Should().Be(stored.ToString());
    }

    [Fact]
    public async Task DeleteAsync_Should_DeleteOnlyTheOwnImagesOfTheBooks()
    {
        var own = Book(string.Empty);
        own.UseStoredCover(OwnImage(own));
        var shared = Book("https://res.cloudinary.com/mcm/image/upload/v1/covers/9782205050196.jpg");
        var web = Book("https://books.google.com/cover.jpg");
        _cloudinary.DeleteImagesAsync(Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>()).Returns(1);

        await _store.DeleteAsync([own, shared, web, Book(string.Empty)], TestContext.Current.CancellationToken);

        await _cloudinary.Received(1).DeleteImagesAsync(
            Arg.Is<IReadOnlyList<string>>(ids => ids.Count == 1 && ids[0] == $"covers/{own.Id}"), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteAsync_Should_CallNothing_WhenNoBookHasItsOwnImage()
    {
        await _store.DeleteAsync([Book("https://books.google.com/cover.jpg")], TestContext.Current.CancellationToken);

        await _cloudinary.DidNotReceive().DeleteImagesAsync(Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteAsync_Should_NotThrow_WhenTheImagesCannotBeDeleted()
    {
        var own = Book(string.Empty);
        own.UseStoredCover(OwnImage(own));
        _cloudinary.DeleteImagesAsync(Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>()).Returns(BooksError.CoversNotDeleted);

        var act = () => _store.DeleteAsync([own], TestContext.Current.CancellationToken);

        await act.Should().NotThrowAsync();
    }
}
