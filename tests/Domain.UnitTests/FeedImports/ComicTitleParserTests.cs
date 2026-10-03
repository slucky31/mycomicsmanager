using Domain.FeedImports;

namespace Domain.UnitTests.FeedImports;

public class ComicTitleParserTests
{
    [Theory]
    [InlineData("Blacksad - Tome 3 - Âme rouge", "Blacksad", 3, "Âme rouge")]
    [InlineData("Blacksad T03 : Âme rouge", "Blacksad", 3, "Âme rouge")]
    [InlineData("Blacksad T.3 - Âme rouge", "Blacksad", 3, "Âme rouge")]
    [InlineData("Les Vieux Fourneaux T08 (2024)", "Les Vieux Fourneaux", 8, null)]
    [InlineData("Astérix #40 - L'Iris blanc", "Astérix", 40, "L'Iris blanc")]
    [InlineData("One Piece Vol. 105", "One Piece", 105, null)]
    [InlineData("Largo Winch - 20 - Le Prix de l'argent", "Largo Winch", 20, "Le Prix de l'argent")]
    [InlineData("Thorgal (Tome 12) - La Parole d'Odin [FR]", "Thorgal", 12, "La Parole d'Odin")]
    [InlineData("Spirou et Fantasio n°56", "Spirou et Fantasio", 56, null)]
    [InlineData("Blacksad_T03_Ame_rouge.cbz", "Blacksad", 3, "Ame rouge")]
    [InlineData("Blacksad.T03.Ame.Rouge.cbr", "Blacksad", 3, "Ame Rouge")]
    [InlineData("Lucky Luke - Tome 01 - La mine d'or de Dick Digger.pdf", "Lucky Luke", 1, "La mine d'or de Dick Digger")]
    public void Parse_Should_ExtractSerieVolumeAndTitle_WhenTitleHasAVolume(string input, string serie, int volume, string? title)
    {
        var parsed = ComicTitleParser.Parse(input);

        parsed.Serie.Should().Be(serie);
        parsed.Volume.Should().Be(volume);
        parsed.Title.Should().Be(title);
    }

    [Theory]
    [InlineData("Le Château des animaux - Intégrale", "Le Château des animaux", "Intégrale")]
    [InlineData("Maus", "Maus", null)]
    [InlineData("L'Arabe du futur : Une jeunesse au Moyen-Orient", "L'Arabe du futur", "Une jeunesse au Moyen-Orient")]
    public void Parse_Should_ReturnNoVolume_WhenTitleHasNone(string input, string serie, string? title)
    {
        var parsed = ComicTitleParser.Parse(input);

        parsed.Serie.Should().Be(serie);
        parsed.Volume.Should().BeNull();
        parsed.Title.Should().Be(title);
    }

    [Theory]
    [InlineData("Spirou et Fantasio - Tomes 1 à 5", "Spirou et Fantasio")]
    [InlineData("Blacksad T01-T05", "Blacksad")]
    public void Parse_Should_ReturnNoVolume_WhenTitleIsAVolumeRange(string input, string serie)
    {
        var parsed = ComicTitleParser.Parse(input);

        parsed.Serie.Should().Be(serie);
        parsed.Volume.Should().BeNull();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Parse_Should_ReturnEmpty_WhenInputIsBlank(string? input)
    {
        ComicTitleParser.Parse(input).Should().Be(ParsedComicTitle.Empty);
    }

    // Real titles and file names seen on planete-bd.org / zone-ebook.com.
    [Theory]
    [InlineData("Tramp - Tome 15 - Les Naufragés du Lavalette - Kraehn (Jean-Charles) (2026)", "Tramp", 15)]
    [InlineData("La Guerre des magiciens T02: Londres - DAL'PRA+TRILLO+MANDRAFINA", "La Guerre des magiciens", 2)]
    [InlineData("Avaler La Lune - Tome 03 - Le Refuge (2026)", "Avaler La Lune", 3)]
    [InlineData("El Borbah - Tome 1", "El Borbah", 1)]
    [InlineData("Ladies.with.Guns.T02.pdf", "Ladies with Guns", 2)]
    [InlineData("Tramp.Tome.15.Les.Naufrag.s.du.Lavalette.rar", "Tramp", 15)]
    [InlineData("Guerre.Des.Magiciens.La.T02.Londres.cbz.cbz", "Guerre Des Magiciens La", 2)]
    [InlineData("Les.vents.de.la.colre.T01.cbz", "Les vents de la colre", 1)]
    public void Parse_Should_ReadRealFeedTitlesAndFileNames(string input, string serie, int volume)
    {
        var parsed = ComicTitleParser.Parse(input);

        parsed.Serie.Should().Be(serie);
        parsed.Volume.Should().Be(volume);
    }

    [Theory]
    [InlineData("Fluide (Re-Up)", "Fluide")]
    [InlineData("Metronom' - Les 5 tomes (Re-Up)", "Metronom'")]
    public void Parse_Should_DropReUploadMarker_WhenTitleHasNoVolume(string input, string serie)
    {
        var parsed = ComicTitleParser.Parse(input);

        parsed.Serie.Should().Be(serie);
        parsed.Volume.Should().BeNull();
    }
}
