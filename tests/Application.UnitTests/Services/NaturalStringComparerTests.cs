using Persistence.Services;

namespace Application.UnitTests.Services;

public class NaturalStringComparerTests
{
    [Theory]
    [InlineData(null, null, 0)]
    [InlineData(null, "a", -1)]
    [InlineData("a", null, 1)]
    [InlineData("page2.jpg", "page10.jpg", -1)]
    [InlineData("page10.jpg", "page2.jpg", 1)]
    [InlineData("page12.jpg", "page13.jpg", -1)]
    [InlineData("Page1.jpg", "page1.JPG", 0)]
    [InlineData("abc", "abd", -1)]
    [InlineData("abd", "abc", 1)]
    [InlineData("page", "page1", -1)]
    [InlineData("page1", "page", 1)]
    [InlineData("page007", "page7", 1)]
    [InlineData("page7", "page007", -1)]
    [InlineData("page0", "page", 1)]
    public void Compare_Should_OrderNumbersByValue_AndLettersWithoutCase(string? x, string? y, int expectedSign)
    {
        Math.Sign(NaturalStringComparer.Instance.Compare(x, y)).Should().Be(expectedSign);
    }

    [Fact]
    public void Compare_Should_ReturnZero_ForTheSameInstance()
    {
        const string name = "page1.jpg";

        NaturalStringComparer.Instance.Compare(name, name).Should().Be(0);
    }
}
