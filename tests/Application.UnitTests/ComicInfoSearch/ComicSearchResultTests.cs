using Application.Interfaces;


namespace Application.UnitTests.ComicInfoSearch;

public sealed class ComicSearchResultTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_ShouldInitializeWithAllProperties()
    {
        // Arrange
        const string title = "The Amazing Spider-Man";
        const string serie = "Spider-Man";
        const string isbn = "9781234567890";
        const int volumeNumber = 1;
        const string imageUrl = "https://example.com/image.jpg";
        const string authors = "Stan Lee, Steve Ditko";
        const string publishers = "Marvel Comics";
        var publishDate = new DateOnly(2024, 1, 15);
        const int numberOfPages = 120;
        const bool found = true;

        // Act
        var result = new ComicSearchResult(
            title,
            serie,
            isbn,
            volumeNumber,
            imageUrl,
            authors,
            publishers,
            publishDate,
            numberOfPages,
            found);

        // Assert
        result.Title.Should().Be(title);
        result.Serie.Should().Be(serie);
        result.Isbn.Should().Be(isbn);
        result.VolumeNumber.Should().Be(volumeNumber);
        result.ImageUrl.Should().Be(imageUrl);
        result.Authors.Should().Be(authors);
        result.Publishers.Should().Be(publishers);
        result.PublishDate.Should().Be(publishDate);
        result.NumberOfPages.Should().Be(numberOfPages);
        result.Found.Should().Be(found);
    }

    [Fact]
    public void Constructor_ShouldAcceptNullableProperties_WhenNull()
    {
        // Arrange
        const string title = "Unknown Comic";
        const string serie = "Unknown Series";
        const string isbn = "0000000000000";
        const int volumeNumber = 0;
        const string imageUrl = "";
        const string authors = "";
        const string publishers = "";
        const bool found = false;

        // Act
        var result = new ComicSearchResult(
            title,
            serie,
            isbn,
            volumeNumber,
            imageUrl,
            authors,
            publishers,
            null,
            null,
            found);

        // Assert
        result.Title.Should().Be(title);
        result.Serie.Should().Be(serie);
        result.Isbn.Should().Be(isbn);
        result.VolumeNumber.Should().Be(volumeNumber);
        result.ImageUrl.Should().Be(imageUrl);
        result.Authors.Should().Be(authors);
        result.Publishers.Should().Be(publishers);
        result.PublishDate.Should().BeNull();
        result.NumberOfPages.Should().BeNull();
        result.Found.Should().Be(found);
    }

    #endregion

    #region Equality Tests

    [Fact]
    public void Equals_ShouldReturnTrue_WhenAllPropertiesMatch()
    {
        // Arrange
        var publishDate = new DateOnly(2024, 1, 15);
        var result1 = new ComicSearchResult(
            "Title",
            "Serie",
            "1234567890",
            1,
            "https://example.com/image.jpg",
            "Author",
            "Publisher",
            publishDate,
            100,
            true);

        var result2 = new ComicSearchResult(
            "Title",
            "Serie",
            "1234567890",
            1,
            "https://example.com/image.jpg",
            "Author",
            "Publisher",
            publishDate,
            100,
            true);

        // Act & Assert
        result1.Should().Be(result2);
        (result1 == result2).Should().BeTrue();
    }

    [Fact]
    public void Equals_ShouldReturnFalse_WhenTitleDiffers()
    {
        // Arrange
        var result1 = new ComicSearchResult("Title1", "Serie", "1234", 1, "", "", "", null, null, true);
        var result2 = new ComicSearchResult("Title2", "Serie", "1234", 1, "", "", "", null, null, true);

        // Act & Assert
        result1.Should().NotBe(result2);
        (result1 != result2).Should().BeTrue();
    }

    [Fact]
    public void Equals_ShouldReturnFalse_WhenIsbnDiffers()
    {
        // Arrange
        var result1 = new ComicSearchResult("Title", "Serie", "1234567890", 1, "", "", "", null, null, true);
        var result2 = new ComicSearchResult("Title", "Serie", "0987654321", 1, "", "", "", null, null, true);

        // Act & Assert
        result1.Should().NotBe(result2);
    }

    [Fact]
    public void Equals_ShouldReturnFalse_WhenFoundDiffers()
    {
        // Arrange
        var result1 = new ComicSearchResult("Title", "Serie", "1234", 1, "", "", "", null, null, true);
        var result2 = new ComicSearchResult("Title", "Serie", "1234", 1, "", "", "", null, null, false);

        // Act & Assert
        result1.Should().NotBe(result2);
    }

    [Fact]
    public void GetHashCode_ShouldBeSame_WhenObjectsAreEqual()
    {
        // Arrange
        var publishDate = new DateOnly(2024, 1, 15);
        var result1 = new ComicSearchResult("Title", "Serie", "1234", 1, "url", "Author", "Publisher", publishDate, 100, true);
        var result2 = new ComicSearchResult("Title", "Serie", "1234", 1, "url", "Author", "Publisher", publishDate, 100, true);

        // Act & Assert
        result1.GetHashCode().Should().Be(result2.GetHashCode());
    }

    #endregion

    #region With Expression Tests

    [Fact]
    public void With_ShouldCreateNewInstance_WhenModifyingTitle()
    {
        // Arrange
        var original = new ComicSearchResult("Original Title", "Serie", "1234", 1, "", "", "", null, null, true);

        // Act
        var modified = original with { Title = "New Title" };

        // Assert
        modified.Title.Should().Be("New Title");
        modified.Serie.Should().Be(original.Serie);
        modified.Isbn.Should().Be(original.Isbn);
        original.Title.Should().Be("Original Title");
    }

    [Fact]
    public void With_ShouldCreateNewInstance_WhenModifyingFound()
    {
        // Arrange
        var original = new ComicSearchResult("Title", "Serie", "1234", 1, "", "", "", null, null, false);

        // Act
        var modified = original with { Found = true };

        // Assert
        modified.Found.Should().BeTrue();
        original.Found.Should().BeFalse();
    }

    [Fact]
    public void With_ShouldCreateNewInstance_WhenModifyingNullableProperty()
    {
        // Arrange
        var original = new ComicSearchResult("Title", "Serie", "1234", 1, "", "", "", null, null, true);
        var newDate = new DateOnly(2024, 12, 25);

        // Act
        var modified = original with { PublishDate = newDate };

        // Assert
        modified.PublishDate.Should().Be(newDate);
        original.PublishDate.Should().BeNull();
    }

    #endregion

    #region Deconstruction Tests

    [Fact]
    public void Deconstruct_ShouldExtractAllProperties()
    {
        // Arrange
        var publishDate = new DateOnly(2024, 1, 15);
        var result = new ComicSearchResult(
            "Title",
            "Serie",
            "1234567890",
            5,
            "https://example.com/image.jpg",
            "Author",
            "Publisher",
            publishDate,
            150,
            true);

        // Act
        var (title, serie, isbn, volumeNumber, imageUrl, authors, publishers, pubDate, pages, found) = result;

        // Assert
        title.Should().Be("Title");
        serie.Should().Be("Serie");
        isbn.Should().Be("1234567890");
        volumeNumber.Should().Be(5);
        imageUrl.Should().Be("https://example.com/image.jpg");
        authors.Should().Be("Author");
        publishers.Should().Be("Publisher");
        pubDate.Should().Be(publishDate);
        pages.Should().Be(150);
        found.Should().BeTrue();
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void Constructor_ShouldAcceptEmptyStrings()
    {
        // Act
        var result = new ComicSearchResult(
            string.Empty,
            string.Empty,
            string.Empty,
            0,
            string.Empty,
            string.Empty,
            string.Empty,
            null,
            null,
            false);

        // Assert
        result.Title.Should().Be(string.Empty);
        result.Serie.Should().Be(string.Empty);
        result.Isbn.Should().Be(string.Empty);
        result.ImageUrl.Should().Be(string.Empty);
        result.Authors.Should().Be(string.Empty);
        result.Publishers.Should().Be(string.Empty);
    }

    [Fact]
    public void Constructor_ShouldAcceptNegativeVolumeNumber()
    {
        // Act
        var result = new ComicSearchResult("Title", "Serie", "1234", -1, "", "", "", null, null, false);

        // Assert
        result.VolumeNumber.Should().Be(-1);
    }

    [Fact]
    public void Constructor_ShouldAcceptZeroNumberOfPages()
    {
        // Act
        var result = new ComicSearchResult("Title", "Serie", "1234", 1, "", "", "", null, 0, true);

        // Assert
        result.NumberOfPages.Should().Be(0);
    }

    #endregion
}
