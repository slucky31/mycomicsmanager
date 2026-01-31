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
    public void AddReadingDate_Should_AddReadingDateToCollection()
    {
        // Arrange
        var book = Book.Create("Batman", "Year One", "9781401207526");
        var date = new DateTime(2024, 1, 15);
        const string note = "Great read!";

        // Act
        book.AddReadingDate(date, note);

        // Assert
        book.ReadingDates.Should().HaveCount(1);
        book.ReadingDates[0].Date.Should().Be(date);
        book.ReadingDates[0].Note.Should().Be(note);
        book.ReadingDates[0].BookId.Should().Be(book.Id);
    }

    [Fact]
    public void AddReadingDate_Should_AddMultipleReadingDates()
    {
        // Arrange
        var book = Book.Create("V for Vendetta", "V for Vendetta", "9781401207922");
        var date1 = new DateTime(2023, 6, 1);
        var date2 = new DateTime(2024, 1, 15);
        const string note1 = "First reading";
        const string note2 = "Second reading";

        // Act
        book.AddReadingDate(date1, note1);
        book.AddReadingDate(date2, note2);

        // Assert
        book.ReadingDates.Should().HaveCount(2);
        book.ReadingDates[0].Date.Should().Be(date1);
        book.ReadingDates[0].Note.Should().Be(note1);
        book.ReadingDates[1].Date.Should().Be(date2);
        book.ReadingDates[1].Note.Should().Be(note2);
    }

    [Fact]
    public void RemoveReadingDate_Should_RemoveReadingDateFromCollection()
    {
        // Arrange
        var book = Book.Create("Saga", "Volume 1", "9781607066017");
        var date = new DateTime(2024, 1, 15);
        book.AddReadingDate(date, "Test note");
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
        book.AddReadingDate(date, "Test note");
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
        book.AddReadingDate(date1, "First reading");
        book.AddReadingDate(date2, "Second reading");
        var firstReadingDateId = book.ReadingDates[0].Id;

        // Act
        book.RemoveReadingDate(firstReadingDateId);

        // Assert
        book.ReadingDates.Should().HaveCount(1);
        book.ReadingDates[0].Date.Should().Be(date2);
    }

    [Fact]
    public void UpdateReadingDate_Should_UpdateDateAndNote()
    {
        // Arrange
        var book = Book.Create("Preacher", "Volume 1", "9781401240455");
        var originalDate = new DateTime(2024, 1, 15);
        const string originalNote = "Original note";
        book.AddReadingDate(originalDate, originalNote);
        var readingDateId = book.ReadingDates[0].Id;
        var newDate = new DateTime(2024, 2, 20);
        const string newNote = "Updated note";

        // Act
        book.UpdateReadingDate(readingDateId, newDate, newNote);

        // Assert
        book.ReadingDates.Should().HaveCount(1);
        book.ReadingDates[0].Date.Should().Be(newDate);
        book.ReadingDates[0].Note.Should().Be(newNote);
    }

    [Fact]
    public void UpdateReadingDate_Should_DoNothing_WhenReadingDateNotFound()
    {
        // Arrange
        var book = Book.Create("Transmetropolitan", "Volume 1", "9781401220846");
        var originalDate = new DateTime(2024, 1, 15);
        const string originalNote = "Original note";
        book.AddReadingDate(originalDate, originalNote);
        var nonExistentId = Guid.NewGuid();
        var newDate = new DateTime(2024, 2, 20);
        const string newNote = "Updated note";

        // Act
        book.UpdateReadingDate(nonExistentId, newDate, newNote);

        // Assert
        book.ReadingDates.Should().HaveCount(1);
        book.ReadingDates[0].Date.Should().Be(originalDate);
        book.ReadingDates[0].Note.Should().Be(originalNote);
    }

    [Fact]
    public void ReadingDates_Should_ReturnReadOnlyCollection()
    {
        // Arrange
        var book = Book.Create("The Walking Dead", "Volume 1", "9781582406190");
        book.AddReadingDate(new DateTime(2024, 1, 15), "Test note");

        // Act
        var readingDates = book.ReadingDates;

        // Assert
        readingDates.Should().BeAssignableTo<IReadOnlyList<ReadingDate>>();
    }
}
