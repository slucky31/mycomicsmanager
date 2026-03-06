using Application.Helpers;

namespace Application.UnitTests.Helpers;

public class PublishDateHelperTests
{
    // -------------------------------------------------------
    // Null / empty / whitespace
    // -------------------------------------------------------

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ParsePublishDate_Should_ReturnNull_WhenInputIsNullOrWhitespace(string? dateString)
    {
        // Act
        var result = PublishDateHelper.ParsePublishDate(dateString);

        // Assert
        result.Should().BeNull();
    }

    // -------------------------------------------------------
    // Explicit formats
    // -------------------------------------------------------

    [Theory]
    [InlineData("September 16, 1987", 1987, 9, 16)]    // MMMM d, yyyy
    [InlineData("September 06, 1987", 1987, 9, 6)]     // MMMM dd, yyyy
    [InlineData("Sep 16, 1987", 1987, 9, 16)]           // MMM d, yyyy
    [InlineData("Sep 06, 1987", 1987, 9, 6)]            // MMM dd, yyyy
    [InlineData("1987-09-16", 1987, 9, 16)]             // yyyy-MM-dd
    [InlineData("1987/09/16", 1987, 9, 16)]             // yyyy/MM/dd
    [InlineData("16/09/1987", 1987, 9, 16)]             // dd/MM/yyyy  (DisplayFormat)
    [InlineData("09/16/1987", 1987, 9, 16)]             // MM/dd/yyyy
    [InlineData("September 1987", 1987, 9, 1)]          // MMMM yyyy
    [InlineData("Sep 1987", 1987, 9, 1)]                // MMM yyyy
    [InlineData("1987", 1987, 1, 1)]                    // yyyy
    public void ParsePublishDate_Should_ReturnExpectedDate_WhenFormatIsRecognised(
        string dateString, int year, int month, int day)
    {
        // Act
        var result = PublishDateHelper.ParsePublishDate(dateString);

        // Assert
        result.Should().Be(new DateOnly(year, month, day));
    }

    // -------------------------------------------------------
    // Generic fallback (DateTime.TryParse)
    // -------------------------------------------------------

    [Fact]
    public void ParsePublishDate_Should_ReturnDate_WhenFallbackGenericParseSucceeds()
    {
        // ISO 8601 with time component – no explicit format matches, but DateTime.TryParse handles it
        var result = PublishDateHelper.ParsePublishDate("1987-09-16T00:00:00");

        result.Should().Be(new DateOnly(1987, 9, 16));
    }

    [Fact]
    public void ParsePublishDate_Should_ReturnNull_WhenDateStringIsUnparseable()
    {
        // Act
        var result = PublishDateHelper.ParsePublishDate("not-a-date");

        // Assert
        result.Should().BeNull();
    }
}
