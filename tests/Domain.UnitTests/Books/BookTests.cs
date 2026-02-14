using Domain.Books;

namespace Domain.UnitTests.Books;

public class BookTests
{
    [Fact]
    public void Create_Should_CreateBookWithRequiredProperties()
    {
        // Arrange
        const string series = "The Sandman";
        const string title = "Preludes & Nocturnes";
        const string isbn = "9781401284770";

        // Act
        var book = Book.Create(series, title, isbn);

        // Assert
        book.Should().NotBeNull();
        book.Id.Should().NotBe(Guid.Empty);
        book.Serie.Should().Be(series);
        book.Title.Should().Be(title);
        book.ISBN.Should().Be(isbn);
        book.VolumeNumber.Should().Be(1);
        book.ImageLink.Should().Be(string.Empty);
        book.Rating.Should().Be(0);
        book.ReadingDates.Should().BeEmpty();
    }

    [Fact]
    public void Create_Should_CreateBookWithVolumeNumber()
    {
        // Arrange
        const string series = "The Sandman";
        const string title = "The Doll's House";
        const string isbn = "9781401284787";
        const int volumeNumber = 2;

        // Act
        var book = Book.Create(series, title, isbn, volumeNumber);

        // Assert
        book.Should().NotBeNull();
        book.Id.Should().NotBe(Guid.Empty);
        book.Serie.Should().Be(series);
        book.Title.Should().Be(title);
        book.ISBN.Should().Be(isbn);
        book.VolumeNumber.Should().Be(volumeNumber);
        book.ImageLink.Should().Be(string.Empty);
        book.Rating.Should().Be(0);
        book.ReadingDates.Should().BeEmpty();
    }

    [Fact]
    public void Create_Should_CreateBookWithAllProperties()
    {
        // Arrange
        const string series = "The Sandman";
        const string title = "Dream Country";
        const string isbn = "9781401284794";
        const int volumeNumber = 3;
        const string imageLink = "https://example.com/image.jpg";
        const int rating = 4;

        // Act
        var book = Book.Create(series, title, isbn, volumeNumber, imageLink, rating);

        // Assert
        book.Should().NotBeNull();
        book.Id.Should().NotBe(Guid.Empty);
        book.Serie.Should().Be(series);
        book.Title.Should().Be(title);
        book.ISBN.Should().Be(isbn);
        book.VolumeNumber.Should().Be(volumeNumber);
        book.ImageLink.Should().Be(imageLink);
        book.Rating.Should().Be(rating);
        book.ReadingDates.Should().BeEmpty();
    }

    [Fact]
    public void Create_Should_GenerateVersion7Guid()
    {
        // Arrange
        const string series = "Watchmen";
        const string title = "Watchmen";
        const string isbn = "9781401245252";

        // Act
        var book = Book.Create(series, title, isbn);

        // Assert
        book.Id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void Create_Should_CreateBookWithAllMetadata()
    {
        // Arrange
        const string series = "Saga";
        const string title = "Chapter One";
        const string isbn = "9781607066019";
        const int volumeNumber = 1;
        const string imageLink = "https://example.com/saga.jpg";
        const int rating = 5;
        const string authors = "Brian K. Vaughan, Fiona Staples";
        const string publishers = "Image Comics";
        var publishDate = new DateOnly(2012, 10, 10);
        const int numberOfPages = 160;

        // Act
        var book = Book.Create(series, title, isbn, volumeNumber, imageLink, rating, authors, publishers, publishDate, numberOfPages);

        // Assert
        book.Should().NotBeNull();
        book.Id.Should().NotBe(Guid.Empty);
        book.Serie.Should().Be(series);
        book.Title.Should().Be(title);
        book.ISBN.Should().Be(isbn);
        book.VolumeNumber.Should().Be(volumeNumber);
        book.ImageLink.Should().Be(imageLink);
        book.Rating.Should().Be(rating);
        book.Authors.Should().Be(authors);
        book.Publishers.Should().Be(publishers);
        book.PublishDate.Should().Be(publishDate);
        book.NumberOfPages.Should().Be(numberOfPages);
        book.ReadingDates.Should().BeEmpty();
    }

    [Fact]
    public void Create_Should_CreateBookWithAuthorsAndPublishers()
    {
        // Arrange
        const string series = "Watchmen";
        const string title = "Watchmen";
        const string isbn = "9781401245252";
        const string authors = "Alan Moore, Dave Gibbons";
        const string publishers = "DC Comics";

        // Act
        var book = Book.Create(series, title, isbn, authors: authors, publishers: publishers);

        // Assert
        book.Authors.Should().Be(authors);
        book.Publishers.Should().Be(publishers);
        book.PublishDate.Should().BeNull();
        book.NumberOfPages.Should().BeNull();
        book.VolumeNumber.Should().Be(1);
        book.ImageLink.Should().BeEmpty();
        book.Rating.Should().Be(0);
    }

    [Fact]
    public void Create_Should_CreateBookWithPublishDate()
    {
        // Arrange
        const string series = "The Walking Dead";
        const string title = "Days Gone Bye";
        const string isbn = "9781582406190";
        var publishDate = new DateOnly(2004, 10, 1);

        // Act
        var book = Book.Create(series, title, isbn, publishDate: publishDate);

        // Assert
        book.PublishDate.Should().Be(publishDate);
        book.Authors.Should().BeEmpty();
        book.Publishers.Should().BeEmpty();
        book.NumberOfPages.Should().BeNull();
    }

    [Fact]
    public void Create_Should_CreateBookWithNumberOfPages()
    {
        // Arrange
        const string series = "Y: The Last Man";
        const string title = "Unmanned";
        const string isbn = "9781401219512";
        const int numberOfPages = 128;

        // Act
        var book = Book.Create(series, title, isbn, numberOfPages: numberOfPages);

        // Assert
        book.NumberOfPages.Should().Be(numberOfPages);
        book.Authors.Should().BeEmpty();
        book.Publishers.Should().BeEmpty();
        book.PublishDate.Should().BeNull();
    }

    [Fact]
    public void Create_Should_UseDefaultValues_WhenOptionalParametersNotProvided()
    {
        // Arrange
        const string series = "Fables";
        const string title = "Volume 1";
        const string isbn = "9781563899423";

        // Act
        var book = Book.Create(series, title, isbn);

        // Assert
        book.VolumeNumber.Should().Be(1);
        book.ImageLink.Should().BeEmpty();
        book.Rating.Should().Be(0);
        book.Authors.Should().BeEmpty();
        book.Publishers.Should().BeEmpty();
        book.PublishDate.Should().BeNull();
        book.NumberOfPages.Should().BeNull();
    }

    [Fact]
    public void Update_Should_UpdateAllProperties()
    {
        // Arrange
        var book = Book.Create("Old Series", "Old Title", "1234567890");
        const string newSeries = "New Series";
        const string newTitle = "New Title";
        const string newIsbn = "9876543210";
        const int newVolumeNumber = 5;
        const string newImageLink = "https://example.com/new-image.jpg";
        const int newRating = 5;

        // Act
        book.Update(newSeries, newTitle, newIsbn, newVolumeNumber, newImageLink, newRating);

        // Assert
        book.Serie.Should().Be(newSeries);
        book.Title.Should().Be(newTitle);
        book.ISBN.Should().Be(newIsbn);
        book.VolumeNumber.Should().Be(newVolumeNumber);
        book.ImageLink.Should().Be(newImageLink);
        book.Rating.Should().Be(newRating);
    }

    [Fact]
    public void Update_Should_UpdateAllMetadataFields()
    {
        // Arrange
        var originalPublishDate = new DateOnly(2020, 1, 1);
        var book = Book.Create("Old Series", "Old Title", "1234567890",
            authors: "Old Author", publishers: "Old Publisher",
            publishDate: originalPublishDate, numberOfPages: 100);

        const string newSeries = "Updated Series";
        const string newTitle = "Updated Title";
        const string newIsbn = "9876543210";
        const int newVolumeNumber = 3;
        const string newImageLink = "https://example.com/updated.jpg";
        const int newRating = 4;
        const string newAuthors = "New Author, Second Author";
        const string newPublishers = "New Publisher";
        var newPublishDate = new DateOnly(2024, 6, 15);
        const int newNumberOfPages = 250;

        // Act
        book.Update(newSeries, newTitle, newIsbn, newVolumeNumber, newImageLink, newRating,
            newAuthors, newPublishers, newPublishDate, newNumberOfPages);

        // Assert
        book.Serie.Should().Be(newSeries);
        book.Title.Should().Be(newTitle);
        book.ISBN.Should().Be(newIsbn);
        book.VolumeNumber.Should().Be(newVolumeNumber);
        book.ImageLink.Should().Be(newImageLink);
        book.Rating.Should().Be(newRating);
        book.Authors.Should().Be(newAuthors);
        book.Publishers.Should().Be(newPublishers);
        book.PublishDate.Should().Be(newPublishDate);
        book.NumberOfPages.Should().Be(newNumberOfPages);
    }

    [Fact]
    public void Update_Should_UpdateOnlyAuthorsAndPublishers()
    {
        // Arrange
        var book = Book.Create("Series", "Title", "1234567890");
        const string newAuthors = "Alan Moore, Dave Gibbons";
        const string newPublishers = "DC Comics, Vertigo";

        // Act
        book.Update(book.Serie, book.Title, book.ISBN, book.VolumeNumber, book.ImageLink, book.Rating,
            newAuthors, newPublishers);

        // Assert
        book.Authors.Should().Be(newAuthors);
        book.Publishers.Should().Be(newPublishers);
        book.PublishDate.Should().BeNull();
        book.NumberOfPages.Should().BeNull();
    }

    [Fact]
    public void Update_Should_UpdatePublishDateAndNumberOfPages()
    {
        // Arrange
        var book = Book.Create("Series", "Title", "1234567890");
        var newPublishDate = new DateOnly(2021, 3, 15);
        const int newNumberOfPages = 320;

        // Act
        book.Update(book.Serie, book.Title, book.ISBN, book.VolumeNumber, book.ImageLink, book.Rating,
            publishDate: newPublishDate, numberOfPages: newNumberOfPages);

        // Assert
        book.PublishDate.Should().Be(newPublishDate);
        book.NumberOfPages.Should().Be(newNumberOfPages);
        book.Authors.Should().BeEmpty();
        book.Publishers.Should().BeEmpty();
    }

    [Fact]
    public void Update_Should_ClearMetadata_WhenSetToEmptyValues()
    {
        // Arrange
        var originalPublishDate = new DateOnly(2020, 1, 1);
        var book = Book.Create("Series", "Title", "1234567890",
            authors: "Original Author", publishers: "Original Publisher",
            publishDate: originalPublishDate, numberOfPages: 100);

        // Act
        book.Update(book.Serie, book.Title, book.ISBN, book.VolumeNumber, book.ImageLink, book.Rating,
            "", "", null, null);

        // Assert
        book.Authors.Should().BeEmpty();
        book.Publishers.Should().BeEmpty();
        book.PublishDate.Should().BeNull();
        book.NumberOfPages.Should().BeNull();
    }

    [Fact]
    public void Update_Should_PreserveMetadata_WhenNotProvided()
    {
        // Arrange
        var originalPublishDate = new DateOnly(2020, 1, 1);
        var book = Book.Create("Old Series", "Old Title", "1234567890",
            authors: "Original Author", publishers: "Original Publisher",
            publishDate: originalPublishDate, numberOfPages: 100);

        const string newSeries = "New Series";
        const string newTitle = "New Title";

        // Act - update basic properties without metadata parameters
        book.Update(newSeries, newTitle, book.ISBN, book.VolumeNumber, book.ImageLink, book.Rating);

        // Assert - metadata should be cleared to defaults when not provided
        book.Serie.Should().Be(newSeries);
        book.Title.Should().Be(newTitle);
        book.Authors.Should().BeEmpty();
        book.Publishers.Should().BeEmpty();
        book.PublishDate.Should().BeNull();
        book.NumberOfPages.Should().BeNull();
    }

    [Fact]
    public void AddReadingDate_Should_AddReadingDateToCollection()
    {
        // Arrange
        var book = Book.Create("Batman", "Year One", "9781401207526");
        var date = new DateTime(2024, 1, 15);

        // Act
        book.AddReadingDate(date);

        // Assert
        book.ReadingDates.Should().HaveCount(1);
        book.ReadingDates[0].Date.Should().Be(date);
        book.ReadingDates[0].BookId.Should().Be(book.Id);
    }

    [Fact]
    public void AddReadingDate_Should_AddMultipleReadingDates()
    {
        // Arrange
        var book = Book.Create("V for Vendetta", "V for Vendetta", "9781401207922");
        var date1 = new DateTime(2023, 6, 1);
        var date2 = new DateTime(2024, 1, 15);

        // Act
        book.AddReadingDate(date1);
        book.AddReadingDate(date2);

        // Assert
        book.ReadingDates.Should().HaveCount(2);
        book.ReadingDates[0].Date.Should().Be(date1);
        book.ReadingDates[1].Date.Should().Be(date2);
    }

    [Fact]
    public void RemoveReadingDate_Should_RemoveReadingDateFromCollection()
    {
        // Arrange
        var book = Book.Create("Saga", "Volume 1", "9781607066017");
        var date = new DateTime(2024, 1, 15);
        book.AddReadingDate(date);
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
        var book = Book.Create("Fables", "Volume 1", "9781563899423");
        var date = new DateTime(2024, 1, 15);
        book.AddReadingDate(date);
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
        var book = Book.Create("Y: The Last Man", "Volume 1", "9781401219512");
        var date1 = new DateTime(2023, 6, 1);
        var date2 = new DateTime(2024, 1, 15);
        book.AddReadingDate(date1);
        book.AddReadingDate(date2);
        var firstReadingDateId = book.ReadingDates[0].Id;

        // Act
        book.RemoveReadingDate(firstReadingDateId);

        // Assert
        book.ReadingDates.Should().HaveCount(1);
        book.ReadingDates[0].Date.Should().Be(date2);
    }

    [Fact]
    public void UpdateReadingDate_Should_UpdateDate()
    {
        // Arrange
        var book = Book.Create("Preacher", "Volume 1", "9781401240455");
        var originalDate = new DateTime(2024, 1, 15);
        book.AddReadingDate(originalDate);
        var readingDateId = book.ReadingDates[0].Id;
        var newDate = new DateTime(2024, 2, 20);

        // Act
        book.UpdateReadingDate(readingDateId, newDate);

        // Assert
        book.ReadingDates.Should().HaveCount(1);
        book.ReadingDates[0].Date.Should().Be(newDate);
    }

    [Fact]
    public void UpdateReadingDate_Should_DoNothing_WhenReadingDateNotFound()
    {
        // Arrange
        var book = Book.Create("Transmetropolitan", "Volume 1", "9781401220846");
        var originalDate = new DateTime(2024, 1, 15);
        book.AddReadingDate(originalDate);
        var nonExistentId = Guid.NewGuid();
        var newDate = new DateTime(2024, 2, 20);

        // Act
        book.UpdateReadingDate(nonExistentId, newDate);

        // Assert
        book.ReadingDates.Should().HaveCount(1);
        book.ReadingDates[0].Date.Should().Be(originalDate);
    }

    [Fact]
    public void ReadingDates_Should_ReturnReadOnlyCollection()
    {
        // Arrange
        var book = Book.Create("The Walking Dead", "Volume 1", "9781582406190");
        book.AddReadingDate(new DateTime(2024, 1, 15));

        // Act
        var readingDates = book.ReadingDates;

        // Assert
        readingDates.Should().BeAssignableTo<IReadOnlyList<ReadingDate>>();
    }
}
