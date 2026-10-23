using Domain.Series;

namespace Domain.UnitTests.Series;

public class SerieNamesTests
{
    [Theory]
    [InlineData(" Thorgal ", "THORGAL")]
    [InlineData("thorgal", "THORGAL")]
    [InlineData("Astérix", "ASTÉRIX")]
    [InlineData(null, "")]
    public void Normalize_Should_IgnoreTheCaseAndTheSpacesAround(string? serie, string expected)
    {
        SerieNames.Normalize(serie).Should().Be(expected);
    }

    [Theory]
    [InlineData("One Shot", true)]
    [InlineData(" one shot ", true)]
    [InlineData("One Shots", false)]
    [InlineData(null, false)]
    public void IsOneShot_Should_RecognizeTheBooksOutsideASeries(string? serie, bool expected)
    {
        SerieNames.IsOneShot(serie).Should().Be(expected);
    }
}
