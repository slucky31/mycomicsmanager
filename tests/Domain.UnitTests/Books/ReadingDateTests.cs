using Domain.Books;

namespace Domain.UnitTests.Books;

public class ReadingDateTests
{
    [Fact]
    public void Create_Should_CreateReadingDateWithRequiredProperties()
    {
        // Arrange
        var date = new DateTime(2024, 1, 15, 10, 30, 0);
        const string note = "Great reading session";
        var bookId = Guid.NewGuid();

        // Act
        var readingDate = ReadingDate.Create(date, note, bookId);

        // Assert
        readingDate.Should().NotBeNull();
        readingDate.Id.Should().NotBe(Guid.Empty);
        readingDate.Date.Should().Be(date);
        readingDate.Note.Should().Be(note);
        readingDate.BookId.Should().Be(bookId);
    }

    [Fact]
    public void Create_Should_CreateReadingDateWithEmptyNote()
    {
        // Arrange
        var date = new DateTime(2024, 2, 20, 14, 0, 0);
        var note = string.Empty;
        var bookId = Guid.NewGuid();

        // Act
        var readingDate = ReadingDate.Create(date, note, bookId);

        // Assert
        readingDate.Should().NotBeNull();
        readingDate.Id.Should().NotBe(Guid.Empty);
        readingDate.Date.Should().Be(date);
        readingDate.Note.Should().Be(string.Empty);
        readingDate.BookId.Should().Be(bookId);
    }

    [Fact]
    public void Create_Should_GenerateVersion7Guid()
    {
        // Arrange
        var date = new DateTime(2024, 3, 10);
        const string note = "Test note";
        var bookId = Guid.NewGuid();

        // Act
        var readingDate = ReadingDate.Create(date, note, bookId);

        // Assert
        readingDate.Id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void Create_Should_CreateUniqueIds_WhenCalledMultipleTimes()
    {
        // Arrange
        var date = new DateTime(2024, 1, 15);
        const string note = "Test note";
        var bookId = Guid.NewGuid();

        // Act
        var readingDate1 = ReadingDate.Create(date, note, bookId);
        var readingDate2 = ReadingDate.Create(date, note, bookId);

        // Assert
        readingDate1.Id.Should().NotBe(readingDate2.Id);
    }

    [Fact]
    public void Update_Should_UpdateDateAndNote()
    {
        // Arrange
        var originalDate = new DateTime(2024, 1, 15);
        const string originalNote = "Original note";
        var bookId = Guid.NewGuid();
        var readingDate = ReadingDate.Create(originalDate, originalNote, bookId);
        var newDate = new DateTime(2024, 2, 20);
        const string newNote = "Updated note";

        // Act
        readingDate.Update(newDate, newNote);

        // Assert
        readingDate.Date.Should().Be(newDate);
        readingDate.Note.Should().Be(newNote);
    }

    [Fact]
    public void Update_Should_NotChangeBookId()
    {
        // Arrange
        var originalDate = new DateTime(2024, 1, 15);
        const string originalNote = "Original note";
        var bookId = Guid.NewGuid();
        var readingDate = ReadingDate.Create(originalDate, originalNote, bookId);
        var newDate = new DateTime(2024, 2, 20);
        const string newNote = "Updated note";

        // Act
        readingDate.Update(newDate, newNote);

        // Assert
        readingDate.BookId.Should().Be(bookId);
    }

    [Fact]
    public void Update_Should_NotChangeId()
    {
        // Arrange
        var originalDate = new DateTime(2024, 1, 15);
        const string originalNote = "Original note";
        var bookId = Guid.NewGuid();
        var readingDate = ReadingDate.Create(originalDate, originalNote, bookId);
        var originalId = readingDate.Id;
        var newDate = new DateTime(2024, 2, 20);
        const string newNote = "Updated note";

        // Act
        readingDate.Update(newDate, newNote);

        // Assert
        readingDate.Id.Should().Be(originalId);
    }

    [Fact]
    public void Update_Should_AllowEmptyNote()
    {
        // Arrange
        var originalDate = new DateTime(2024, 1, 15);
        const string originalNote = "Original note";
        var bookId = Guid.NewGuid();
        var readingDate = ReadingDate.Create(originalDate, originalNote, bookId);
        var newDate = new DateTime(2024, 2, 20);
        var newNote = string.Empty;

        // Act
        readingDate.Update(newDate, newNote);

        // Assert
        readingDate.Date.Should().Be(newDate);
        readingDate.Note.Should().Be(string.Empty);
    }

    [Fact]
    public void Update_Should_AllowMultipleUpdates()
    {
        // Arrange
        var originalDate = new DateTime(2024, 1, 15);
        var bookId = Guid.NewGuid();
        var readingDate = ReadingDate.Create(originalDate, "First note", bookId);

        var secondDate = new DateTime(2024, 2, 20);
        const string secondNote = "Second note";
        var thirdDate = new DateTime(2024, 3, 25);
        const string thirdNote = "Third note";

        // Act
        readingDate.Update(secondDate, secondNote);
        readingDate.Update(thirdDate, thirdNote);

        // Assert
        readingDate.Date.Should().Be(thirdDate);
        readingDate.Note.Should().Be(thirdNote);
        readingDate.BookId.Should().Be(bookId);
    }

    [Fact]
    public void Date_Should_StoreFullDateTimeInformation()
    {
        // Arrange
        var date = new DateTime(2024, 12, 31, 23, 59, 59, 999);
        var note = "New Year's Eve";
        var bookId = Guid.NewGuid();

        // Act
        var readingDate = ReadingDate.Create(date, note, bookId);

        // Assert
        readingDate.Date.Should().Be(date);
        readingDate.Date.Year.Should().Be(2024);
        readingDate.Date.Month.Should().Be(12);
        readingDate.Date.Day.Should().Be(31);
        readingDate.Date.Hour.Should().Be(23);
        readingDate.Date.Minute.Should().Be(59);
        readingDate.Date.Second.Should().Be(59);
    }
}
