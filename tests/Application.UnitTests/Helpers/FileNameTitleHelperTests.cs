using Application.Helpers;
using Domain.FeedImports;

namespace Application.UnitTests.Helpers;

public class FileNameTitleHelperTests
{
    [Theory]
    [InlineData("Ashita.no.Joe.T08.[BD.fr].(2024).cbz", "Ashita no Joe", "Ashita no Joe", 8)]
    [InlineData("Ashita_no_Joe_T08.cbz", "Ashita no Joe", "Ashita no Joe", 8)]
    [InlineData("Blacksad.T03.Ame.Rouge.cbr", "Blacksad", "Ame Rouge", 3)]
    [InlineData("Thorgal (Tome 12) [FR].pdf", "Thorgal", "Thorgal", 12)]
    [InlineData("Les.Vieux.Fourneaux.[FR].(Dargaud).cbz", "Les Vieux Fourneaux", "Les Vieux Fourneaux", null)]
    [InlineData("  Maus  .cbz", "Maus", "Maus", null)]
    public void Parse_Should_CleanTheNameAndReadTheVolume(string fileName, string serie, string title, int? volume)
    {
        FileNameTitleHelper.Parse(fileName).Should().Be(new ParsedComicTitle(serie, title, volume));
    }

    [Theory]
    [InlineData("[Scan].cbz", "[Scan]", null)]
    [InlineData("T08.cbz", "T08", 8)]
    public void Parse_Should_KeepTheName_WhenNoSerieRemains(string fileName, string serie, int? volume)
    {
        var parsed = FileNameTitleHelper.Parse(fileName);

        parsed.Serie.Should().Be(serie);
        parsed.Title.Should().Be(serie);
        parsed.Volume.Should().Be(volume);
    }
}
