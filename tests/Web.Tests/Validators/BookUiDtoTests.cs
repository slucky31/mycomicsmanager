using AwesomeAssertions;
using Domain.Books;
using Web.Validators;
using Xunit;

namespace Web.Tests.Validators;

public sealed class BookUiDtoTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_ShouldInitializeWithDefaultValues()
    {
        // Act
        var dto = new BookUiDto();

        // Assert
        dto.Serie.Should().Be(string.Empty);
        dto.Title.Should().Be(string.Empty);
        dto.ISBN.Should().Be(string.Empty);
        dto.VolumeNumber.Should().Be(1);
        dto.ImageLink.Should().Be(string.Empty);
        dto.Id.Should().Be(default(Guid));
        dto.CreatedOnUtc.Should().Be(default(DateTime));
        dto.ModifiedOnUtc.Should().BeNull();
    }

    #endregion

    #region Property Tests

    [Fact]
    public void Serie_ShouldAllowSettingValue()
    {
        // Arrange
        var dto = new BookUiDto();
        const string serie = "The Amazing Spider-Man";

        // Act
        dto.Serie = serie;

        // Assert
        dto.Serie.Should().Be(serie);
    }

    [Fact]
    public void Title_ShouldAllowSettingValue()
    {
        // Arrange
        var dto = new BookUiDto();
        const string title = "Into the Spider-Verse";

        // Act
        dto.Title = title;

        // Assert
        dto.Title.Should().Be(title);
    }

    [Fact]
    public void ISBN_ShouldAllowSettingValue()
    {
        // Arrange
        var dto = new BookUiDto();
        const string isbn = "978-0-123456-78-9";

        // Act
        dto.ISBN = isbn;

        // Assert
        dto.ISBN.Should().Be(isbn);
    }

    [Fact]
    public void VolumeNumber_ShouldAllowSettingValue()
    {
        // Arrange
        var dto = new BookUiDto();
        const int volumeNumber = 42;

        // Act
        dto.VolumeNumber = volumeNumber;

        // Assert
        dto.VolumeNumber.Should().Be(volumeNumber);
    }

    [Fact]
    public void ImageLink_ShouldAllowSettingValue()
    {
        // Arrange
        var dto = new BookUiDto();
        const string imageLink = "https://example.com/cover.jpg";

        // Act
        dto.ImageLink = imageLink;

        // Assert
        dto.ImageLink.Should().Be(imageLink);
    }

    #endregion

    #region Convert Method Tests

    [Fact]
    public void Convert_ShouldMapAllProperties_WhenBookIsProvided()
    {
        // Arrange
        const string serie = "Batman";
        const string title = "The Dark Knight Returns";
        const string isbn = "978-1-401263-119-5";
        const int volumeNumber = 1;
        const string imageLink = "https://example.com/batman.jpg";

        var book = Book.Create(serie, title, isbn, volumeNumber, imageLink);
        book.CreatedOnUtc = new DateTime(2024, 1, 15, 10, 30, 0, DateTimeKind.Utc);
        book.ModifiedOnUtc = new DateTime(2024, 1, 20, 14, 45, 0, DateTimeKind.Utc);

        // Act
        var dto = BookUiDto.Convert(book);

        // Assert
        dto.Id.Should().Be(book.Id);
        dto.Serie.Should().Be(serie);
        dto.Title.Should().Be(title);
        dto.ISBN.Should().Be(isbn);
        dto.VolumeNumber.Should().Be(volumeNumber);
        dto.ImageLink.Should().Be(imageLink);
        dto.CreatedOnUtc.Should().Be(book.CreatedOnUtc);
        dto.ModifiedOnUtc.Should().Be(book.ModifiedOnUtc);
    }

    [Fact]
    public void Convert_ShouldHandleEmptyStrings()
    {
        // Arrange
        var book = Book.Create("", "", "");

        // Act
        var dto = BookUiDto.Convert(book);

        // Assert
        dto.Serie.Should().Be(string.Empty);
        dto.Title.Should().Be(string.Empty);
        dto.ISBN.Should().Be(string.Empty);
        dto.ImageLink.Should().Be(string.Empty);
    }

    [Fact]
    public void Convert_ShouldHandleDefaultVolumeNumber()
    {
        // Arrange
        var book = Book.Create("Test Serie", "Test Title", "123-456");

        // Act
        var dto = BookUiDto.Convert(book);

        // Assert
        dto.VolumeNumber.Should().Be(1);
    }

    [Fact]
    public void Convert_ShouldHandleEmptyImageLink()
    {
        // Arrange
        var book = Book.Create("Serie", "Title", "ISBN", 5);

        // Act
        var dto = BookUiDto.Convert(book);

        // Assert
        dto.ImageLink.Should().Be(string.Empty);
    }

    [Fact]
    public void Convert_ShouldPreserveGuidId()
    {
        // Arrange
        var book = Book.Create("X-Men", "Days of Future Past", "978-0-785-19-4");

        // Act
        var dto = BookUiDto.Convert(book);

        // Assert
        dto.Id.Should().NotBe(default(Guid));
        dto.Id.Should().Be(book.Id);
    }

    [Fact]
    public void Convert_ShouldHandleNullModifiedOnUtc()
    {
        // Arrange
        var book = Book.Create("Series", "Title", "ISBN");

        // Act
        var dto = BookUiDto.Convert(book);

        // Assert
        dto.ModifiedOnUtc.Should().BeNull();
    }

    [Fact]
    public void Convert_ShouldHandleCompleteBook_WithAllFieldsPopulated()
    {
        // Arrange
        const string serie = "The Walking Dead";
        const string title = "Days Gone Bye";
        const string isbn = "978-1-582406-72-1";
        const int volumeNumber = 1;
        const string imageLink = "https://cdn.example.com/walking-dead-vol1.jpg";

        var book = Book.Create(serie, title, isbn, volumeNumber, imageLink);
        var createdDate = new DateTime(2023, 6, 1, 8, 0, 0, DateTimeKind.Utc);
        var modifiedDate = new DateTime(2023, 6, 15, 16, 30, 0, DateTimeKind.Utc);
        book.CreatedOnUtc = createdDate;
        book.ModifiedOnUtc = modifiedDate;

        // Act
        var dto = BookUiDto.Convert(book);

        // Assert
        dto.Id.Should().Be(book.Id);
        dto.Serie.Should().Be(serie);
        dto.Title.Should().Be(title);
        dto.ISBN.Should().Be(isbn);
        dto.VolumeNumber.Should().Be(volumeNumber);
        dto.ImageLink.Should().Be(imageLink);
        dto.CreatedOnUtc.Should().Be(createdDate);
        dto.ModifiedOnUtc.Should().Be(modifiedDate);
    }

    [Fact]
    public void Convert_ShouldCreateNewInstance_EachTime()
    {
        // Arrange
        var book = Book.Create("Series", "Title", "ISBN");

        // Act
        var dto1 = BookUiDto.Convert(book);
        var dto2 = BookUiDto.Convert(book);

        // Assert
        dto1.Should().NotBeSameAs(dto2);
        dto1.Id.Should().Be(dto2.Id);
    }

    [Fact]
    public void Convert_ShouldHandleLargeVolumeNumbers()
    {
        // Arrange
        const int volumeNumber = 999;
        var book = Book.Create("One Piece", "Chapter 999", "ISBN", volumeNumber);

        // Act
        var dto = BookUiDto.Convert(book);

        // Assert
        dto.VolumeNumber.Should().Be(volumeNumber);
    }

    [Fact]
    public void Convert_ShouldHandleLongStrings()
    {
        // Arrange
        var longSerie = new string('A', 500);
        var longTitle = new string('B', 500);
        var longIsbn = new string('1', 100);
        var longImageLink = new string('/', 1000);

        var book = Book.Create(longSerie, longTitle, longIsbn, 1, longImageLink);

        // Act
        var dto = BookUiDto.Convert(book);

        // Assert
        dto.Serie.Should().Be(longSerie);
        dto.Title.Should().Be(longTitle);
        dto.ISBN.Should().Be(longIsbn);
        dto.ImageLink.Should().Be(longImageLink);
    }

    [Fact]
    public void Convert_ShouldHandleSpecialCharacters()
    {
        // Arrange
        const string serie = "Série Ã©crite en français";
        const string title = "Ñoño's Ädventure!";
        const string isbn = "978-ä-öü-ß";

        var book = Book.Create(serie, title, isbn);

        // Act
        var dto = BookUiDto.Convert(book);

        // Assert
        dto.Serie.Should().Be(serie);
        dto.Title.Should().Be(title);
        dto.ISBN.Should().Be(isbn);
    }

    #endregion
}
