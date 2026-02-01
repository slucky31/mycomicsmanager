using Domain.Books;

namespace Domain.UnitTests.Books;

public class ReadingDateTests
{
    [Fact]
    public void Create_Should_CreateReadingDateWithRequiredProperties()
    {
        // Arrange
        var date = new DateTime(2024, 1, 15, 10, 30, 0);
        var bookId = Guid.NewGuid();

        // Act
        var readingDate = ReadingDate.Create(date, bookId);

        // Assert
        readingDate.Should().NotBeNull();
        readingDate.Id.Should().NotBe(Guid.Empty);
        readingDate.Date.Should().Be(date);
        readingDate.BookId.Should().Be(bookId);
    }

    [Fact]
    public void Create_Should_GenerateVersion7Guid()
    {
        // Arrange
        var date = new DateTime(2024, 3, 10);
        var bookId = Guid.NewGuid();

        // Act
        var readingDate = ReadingDate.Create(date, bookId);

        // Assert
        readingDate.Id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void Create_Should_CreateUniqueIds_WhenCalledMultipleTimes()
    {
        // Arrange
        var date = new DateTime(2024, 1, 15);
        var bookId = Guid.NewGuid();

        // Act
        var readingDate1 = ReadingDate.Create(date, bookId);
        var readingDate2 = ReadingDate.Create(date, bookId);

        // Assert
        readingDate1.Id.Should().NotBe(readingDate2.Id);
    }

    [Fact]
    public void Update_Should_UpdateDate()
    {
        // Arrange
        var originalDate = new DateTime(2024, 1, 15);
        var bookId = Guid.NewGuid();
        var readingDate = ReadingDate.Create(originalDate, bookId);
        var newDate = new DateTime(2024, 2, 20);

        // Act
        readingDate.Update(newDate);

        // Assert
        readingDate.Date.Should().Be(newDate);
    }

    [Fact]
    public void Update_Should_NotChangeBookId()
    {
        // Arrange
        var originalDate = new DateTime(2024, 1, 15);
        var bookId = Guid.NewGuid();
        var readingDate = ReadingDate.Create(originalDate, bookId);
        var newDate = new DateTime(2024, 2, 20);

        // Act
        readingDate.Update(newDate);

        // Assert
        readingDate.BookId.Should().Be(bookId);
    }

    [Fact]
    public void Update_Should_NotChangeId()
    {
        // Arrange
        var originalDate = new DateTime(2024, 1, 15);
        var bookId = Guid.NewGuid();
        var readingDate = ReadingDate.Create(originalDate, bookId);
        var originalId = readingDate.Id;
        var newDate = new DateTime(2024, 2, 20);

        // Act
        readingDate.Update(newDate);

        // Assert
        readingDate.Id.Should().Be(originalId);
    }

    [Fact]
    public void Update_Should_AllowMultipleUpdates()
    {
        // Arrange
        var originalDate = new DateTime(2024, 1, 15);
        var bookId = Guid.NewGuid();
        var readingDate = ReadingDate.Create(originalDate, bookId);

        var secondDate = new DateTime(2024, 2, 20);
        var thirdDate = new DateTime(2024, 3, 25);

        // Act
        readingDate.Update(secondDate);
        readingDate.Update(thirdDate);

        // Assert
        readingDate.Date.Should().Be(thirdDate);
        readingDate.BookId.Should().Be(bookId);
    }

    [Fact]
    public void Date_Should_StoreFullDateTimeInformation()
    {
        // Arrange
        var date = new DateTime(2024, 12, 31, 23, 59, 59, 999);
        var bookId = Guid.NewGuid();

        // Act
        var readingDate = ReadingDate.Create(date, bookId);

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
