using AwesomeAssertions;
using Web.Models;
using Xunit;

namespace Web.Tests.Models;

public sealed class IsbnEntryTests
{
    [Theory]
    [InlineData("978-2-413-01540-6", "9782413015406")]
    [InlineData(" 978 2413 015406 ", "9782413015406")]
    [InlineData("0-8044-2957-x", "080442957X")]
    public void Check_Should_ReturnTheDigits_WhenTheIsbnIsValid(string typed, string expected)
    {
        IsbnEntry.Check(typed).Should().Be((expected, (string?)null));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" - ")]
    public void Check_Should_AskForTheIsbn_WhenNothingIsTyped(string? typed)
    {
        IsbnEntry.Check(typed).Should().Be(((string?)null, IsbnEntry.Missing));
    }

    [Theory]
    [InlineData("978-2-413-01540-7")]
    [InlineData("12345")]
    public void Check_Should_ReturnAnError_WhenTheIsbnIsInvalid(string typed)
    {
        IsbnEntry.Check(typed).Should().Be(((string?)null, IsbnEntry.Invalid));
    }
}
