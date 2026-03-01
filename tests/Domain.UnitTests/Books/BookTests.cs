using Domain.Books;

namespace Domain.UnitTests.Books;

public class BookTests
{
    private static readonly Guid DefaultLibraryId = Guid.CreateVersion7();

    [Fact]
    public void Create_Should_CreatePhysicalBookWithRequiredProperties()
    {
        // Arrange
        const string series = "The Sandman";
        const string title = "Preludes & Nocturnes";
        const string isbn = "9781401284770";

        // Act
        var result = PhysicalBook.Create(series, title, isbn, libraryId: DefaultLibraryId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var book = result.Value!;
        book.Should().NotBeNull();
        book.Id.Should().NotBe(Guid.Empty);
        book.Serie.Should().Be(series);
        book.Title.Should().Be(title);
        book.ISBN.Should().Be(isbn);
        book.VolumeNumber.Should().Be(1);
        book.ImageLink.Should().Be(string.Empty);
        book.ReadingDates.Should().BeEmpty();
        book.LibraryId.Should().Be(DefaultLibraryId);
    }

    [Fact]
    public void Create_Should_CreatePhysicalBookWithVolumeNumber()
    {
        // Arrange
        const string series = "The Sandman";
        const string title = "The Doll's House";
        const string isbn = "9781401284787";
        const int volumeNumber = 2;

        // Act
        var result = PhysicalBook.Create(series, title, isbn, volumeNumber, libraryId: DefaultLibraryId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var book = result.Value!;
        book.Should().NotBeNull();
        book.Serie.Should().Be(series);
        book.Title.Should().Be(title);
        book.ISBN.Should().Be(isbn);
        book.VolumeNumber.Should().Be(volumeNumber);
        book.ReadingDates.Should().BeEmpty();
    }

    [Fact]
    public void Create_Should_CreatePhysicalBookWithImageLink()
    {
        // Arrange
        const string series = "The Sandman";
        const string title = "Dream Country";
        const string isbn = "9781401284794";
        const int volumeNumber = 3;
        const string imageLink = "https://example.com/image.jpg";

        // Act
        var result = PhysicalBook.Create(series, title, isbn, volumeNumber, imageLink, libraryId: DefaultLibraryId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var book = result.Value!;
        book.ImageLink.Should().Be(imageLink);
        book.VolumeNumber.Should().Be(volumeNumber);
    }

    [Fact]
    public void Create_Should_GenerateVersion7Guid()
    {
        // Arrange & Act
        var result = PhysicalBook.Create("Watchmen", "Watchmen", "9781401245252", libraryId: DefaultLibraryId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void Create_Should_CreatePhysicalBookWithAllMetadata()
    {
        // Arrange
        const string series = "Saga";
        const string title = "Chapter One";
        const string isbn = "9781607066019";
        const int volumeNumber = 1;
        const string imageLink = "https://example.com/saga.jpg";
        const string authors = "Brian K. Vaughan, Fiona Staples";
        const string publishers = "Image Comics";
        var publishDate = new DateOnly(2012, 10, 10);
        const int numberOfPages = 160;

        // Act
        var result = PhysicalBook.Create(series, title, isbn, volumeNumber, imageLink,
            authors, publishers, publishDate, numberOfPages, DefaultLibraryId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var book = result.Value!;
        book.Serie.Should().Be(series);
        book.Title.Should().Be(title);
        book.ISBN.Should().Be(isbn);
        book.VolumeNumber.Should().Be(volumeNumber);
        book.ImageLink.Should().Be(imageLink);
        book.Authors.Should().Be(authors);
        book.Publishers.Should().Be(publishers);
        book.PublishDate.Should().Be(publishDate);
        book.NumberOfPages.Should().Be(numberOfPages);
        book.ReadingDates.Should().BeEmpty();
    }

    [Fact]
    public void Create_Should_ReturnFailure_WhenSerieIsEmpty()
    {
        // Act
        var result = PhysicalBook.Create("", "Title", "9781401245252", libraryId: DefaultLibraryId);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(BooksError.BadRequest);
    }

    [Fact]
    public void Create_Should_ReturnFailure_WhenTitleIsEmpty()
    {
        // Act
        var result = PhysicalBook.Create("Series", "", "9781401245252", libraryId: DefaultLibraryId);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(BooksError.BadRequest);
    }

    [Fact]
    public void Create_Should_ReturnFailure_WhenIsbnIsEmpty()
    {
        // Act
        var result = PhysicalBook.Create("Series", "Title", "", libraryId: DefaultLibraryId);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(BooksError.BadRequest);
    }

    [Fact]
    public void Create_Should_ReturnFailure_WhenLibraryIdIsEmpty()
    {
        // Act
        var result = PhysicalBook.Create("Series", "Title", "9781401245252", libraryId: Guid.Empty);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(BooksError.BadRequest);
    }

    [Fact]
    public void Create_Should_UseDefaultValues_WhenOptionalParametersNotProvided()
    {
        // Act
        var result = PhysicalBook.Create("Fables", "Volume 1", "9781563899423", libraryId: DefaultLibraryId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var book = result.Value!;
        book.VolumeNumber.Should().Be(1);
        book.ImageLink.Should().BeEmpty();
        book.Authors.Should().BeEmpty();
        book.Publishers.Should().BeEmpty();
        book.PublishDate.Should().BeNull();
        book.NumberOfPages.Should().BeNull();
    }

    [Fact]
    public void Update_Should_UpdateAllProperties()
    {
        // Arrange
        var book = PhysicalBook.Create("Old Series", "Old Title", "1234567890", libraryId: DefaultLibraryId).Value!;
        const string newSeries = "New Series";
        const string newTitle = "New Title";
        const string newIsbn = "9876543210";
        const int newVolumeNumber = 5;
        const string newImageLink = "https://example.com/new-image.jpg";

        // Act
        book.Update(newSeries, newTitle, newIsbn, newVolumeNumber, newImageLink);

        // Assert
        book.Serie.Should().Be(newSeries);
        book.Title.Should().Be(newTitle);
        book.ISBN.Should().Be(newIsbn);
        book.VolumeNumber.Should().Be(newVolumeNumber);
        book.ImageLink.Should().Be(newImageLink);
    }

    [Fact]
    public void Update_Should_UpdateAllMetadataFields()
    {
        // Arrange
        var book = PhysicalBook.Create("Old Series", "Old Title", "1234567890",
            authors: "Old Author", publishers: "Old Publisher",
            publishDate: new DateOnly(2020, 1, 1), numberOfPages: 100,
            libraryId: DefaultLibraryId).Value!;

        // Act
        book.Update("Updated Series", "Updated Title", "9876543210", 3,
            "https://example.com/updated.jpg",
            "New Author, Second Author", "New Publisher",
            new DateOnly(2024, 6, 15), 250);

        // Assert
        book.Serie.Should().Be("Updated Series");
        book.Title.Should().Be("Updated Title");
        book.ISBN.Should().Be("9876543210");
        book.VolumeNumber.Should().Be(3);
        book.Authors.Should().Be("New Author, Second Author");
        book.Publishers.Should().Be("New Publisher");
        book.PublishDate.Should().Be(new DateOnly(2024, 6, 15));
        book.NumberOfPages.Should().Be(250);
    }

    [Fact]
    public void Update_Should_ClearMetadata_WhenSetToEmptyValues()
    {
        // Arrange
        var book = PhysicalBook.Create("Series", "Title", "1234567890",
            authors: "Original Author", publishers: "Original Publisher",
            publishDate: new DateOnly(2020, 1, 1), numberOfPages: 100,
            libraryId: DefaultLibraryId).Value!;

        // Act
        book.Update(book.Serie, book.Title, book.ISBN, book.VolumeNumber, book.ImageLink,
            "", "", null, null);

        // Assert
        book.Authors.Should().BeEmpty();
        book.Publishers.Should().BeEmpty();
        book.PublishDate.Should().BeNull();
        book.NumberOfPages.Should().BeNull();
    }

    [Fact]
    public void AddReadingDate_Should_AddReadingDateToCollection()
    {
        // Arrange
        var book = PhysicalBook.Create("Batman", "Year One", "9781401207526", libraryId: DefaultLibraryId).Value!;
        var date = new DateTime(2024, 1, 15);

        // Act
        book.AddReadingDate(date, 4);

        // Assert
        book.ReadingDates.Should().HaveCount(1);
        book.ReadingDates[0].Date.Should().Be(date);
        book.ReadingDates[0].Rating.Should().Be(4);
        book.ReadingDates[0].BookId.Should().Be(book.Id);
    }

    [Fact]
    public void AddReadingDate_Should_AddMultipleReadingDates()
    {
        // Arrange
        var book = PhysicalBook.Create("V for Vendetta", "V for Vendetta", "9781401207922", libraryId: DefaultLibraryId).Value!;
        var date1 = new DateTime(2023, 6, 1);
        var date2 = new DateTime(2024, 1, 15);

        // Act
        book.AddReadingDate(date1, 3);
        book.AddReadingDate(date2, 5);

        // Assert
        book.ReadingDates.Should().HaveCount(2);
        book.ReadingDates[0].Date.Should().Be(date1);
        book.ReadingDates[0].Rating.Should().Be(3);
        book.ReadingDates[1].Date.Should().Be(date2);
        book.ReadingDates[1].Rating.Should().Be(5);
    }

    [Fact]
    public void RemoveReadingDate_Should_RemoveReadingDateFromCollection()
    {
        // Arrange
        var book = PhysicalBook.Create("Saga", "Volume 1", "9781607066017", libraryId: DefaultLibraryId).Value!;
        book.AddReadingDate(new DateTime(2024, 1, 15), 4);
        var readingDateId = book.ReadingDates[0].Id;

        // Act
        book.RemoveReadingDate(readingDateId);

        // Assert
        book.ReadingDates.Should().BeEmpty();
    }

    [Fact]
    public void RemoveReadingDate_Should_DoNothing_WhenReadingDateNotFound()
    {
        // Arrange
        var book = PhysicalBook.Create("Fables", "Volume 1", "9781563899423", libraryId: DefaultLibraryId).Value!;
        book.AddReadingDate(new DateTime(2024, 1, 15), 3);
        var nonExistentId = Guid.NewGuid();

        // Act
        book.RemoveReadingDate(nonExistentId);

        // Assert
        book.ReadingDates.Should().HaveCount(1);
    }

    [Fact]
    public void RemoveReadingDate_Should_RemoveOnlySpecifiedReadingDate()
    {
        // Arrange
        var book = PhysicalBook.Create("Y: The Last Man", "Volume 1", "9781401219512", libraryId: DefaultLibraryId).Value!;
        var date1 = new DateTime(2023, 6, 1);
        var date2 = new DateTime(2024, 1, 15);
        book.AddReadingDate(date1, 3);
        book.AddReadingDate(date2, 5);
        var firstReadingDateId = book.ReadingDates[0].Id;

        // Act
        book.RemoveReadingDate(firstReadingDateId);

        // Assert
        book.ReadingDates.Should().HaveCount(1);
        book.ReadingDates[0].Date.Should().Be(date2);
    }

    [Fact]
    public void ReadingDates_Should_ReturnReadOnlyCollection()
    {
        // Arrange
        var book = PhysicalBook.Create("The Walking Dead", "Volume 1", "9781582406190", libraryId: DefaultLibraryId).Value!;
        book.AddReadingDate(new DateTime(2024, 1, 15), 4);

        // Act
        var readingDates = book.ReadingDates;

        // Assert
        readingDates.Should().BeAssignableTo<IReadOnlyList<ReadingDate>>();
    }

    [Fact]
    public void PhysicalBook_Should_BeAssignableToBook()
    {
        // Act
        var result = PhysicalBook.Create("Series", "Title", "9781401245252", libraryId: DefaultLibraryId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeAssignableTo<Book>();
    }

    [Fact]
    public void Library_Should_BeNull_WhenBookIsCreated()
    {
        // Act
        var book = PhysicalBook.Create("Series", "Title", "9781401245252", libraryId: DefaultLibraryId).Value!;

        // Assert
        book.Library.Should().BeNull();
    }

    [Fact]
    public void AddReadingDate_Should_ReturnCreatedReadingDate()
    {
        // Arrange
        var book = PhysicalBook.Create("Batman", "Year One", "9781401207526", libraryId: DefaultLibraryId).Value!;
        var date = new DateTime(2024, 3, 10);

        // Act
        var readingDate = book.AddReadingDate(date, 5);

        // Assert
        readingDate.Should().NotBeNull();
        readingDate.Date.Should().Be(date);
        readingDate.Rating.Should().Be(5);
        readingDate.BookId.Should().Be(book.Id);
        readingDate.Id.Should().NotBe(Guid.Empty);
    }
}
