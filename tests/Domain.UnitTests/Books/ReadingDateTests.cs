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
        const int rating = 4;

        // Act
        var readingDate = ReadingDate.Create(date, rating, bookId);

        // Assert
        readingDate.Should().NotBeNull();
        readingDate.Id.Should().NotBe(Guid.Empty);
        readingDate.Date.Should().Be(date);
        readingDate.Rating.Should().Be(rating);
        readingDate.BookId.Should().Be(bookId);
    }

    [Fact]
    public void Create_Should_CreateUniqueIds_WhenCalledMultipleTimes()
    {
        // Arrange
        var date = new DateTime(2024, 1, 15);
        var bookId = Guid.NewGuid();

        // Act
        var readingDate1 = ReadingDate.Create(date, 3, bookId);
        var readingDate2 = ReadingDate.Create(date, 3, bookId);

        // Assert
        readingDate1.Id.Should().NotBe(readingDate2.Id);
    }

    [Fact]
    public void Update_Should_UpdateDateAndRating()
    {
        // Arrange
        var originalDate = new DateTime(2024, 1, 15);
        var bookId = Guid.NewGuid();
        var readingDate = ReadingDate.Create(originalDate, 3, bookId);
        var originalId = readingDate.Id;
        var newDate = new DateTime(2024, 2, 20);

        // Act
        readingDate.Update(newDate, 5);

        // Assert
        readingDate.Date.Should().Be(newDate);
        readingDate.Rating.Should().Be(5);
        readingDate.BookId.Should().Be(bookId);
        readingDate.Id.Should().Be(originalId);
    }
}
