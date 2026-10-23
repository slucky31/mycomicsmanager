using Application.Helpers;

namespace Application.UnitTests.Helpers;

public class SeriesHelperTests
{
    [Theory]
    [InlineData("Les Aventures extraordinaires d'Adèle Blanc-Sec", "Les Aventures extraordinaires d'Adèle Blanc-Sec", null)]
    [InlineData("Blacksad ; 2", "Blacksad", 2)]
    [InlineData("Blacksad, tome 2", "Blacksad", 2)]
    [InlineData("Blacksad -- vol. 2", "Blacksad", 2)]
    [InlineData(" Adèle Blanc-Sec ; [6] ", "Adèle Blanc-Sec", 6)]
    public void ParseSerie_Should_SplitTheVolumeFromTheSeries(string value, string serie, int? volumeNumber)
    {
        SeriesHelper.ParseSerie(value).Should().Be((serie, volumeNumber));
    }

    [Theory]
    [InlineData("[6]", 6)]
    [InlineData("T. 12", 12)]
    [InlineData("6 bis", 6)]
    [InlineData("hors-série", null)]
    [InlineData(null, null)]
    public void ParseVolumeNumber_Should_ReadTheFirstNumber(string? value, int? expected)
    {
        SeriesHelper.ParseVolumeNumber(value).Should().Be(expected);
    }
}
