using Domain.FeedImports;

namespace Domain.UnitTests.FeedImports;

public class SerieMatcherTests
{
    [Theory]
    [InlineData("L'Arabe du futur", "ARABE DU FUTUR")]
    [InlineData("Les Vieux Fourneaux", "VIEUX FOURNEAUX")]
    [InlineData("Astérix", "ASTERIX")]
    [InlineData("Spirou & Fantasio", "SPIROU FANTASIO")]
    [InlineData("Guerre Des Magiciens La", "GUERRE DES MAGICIENS")]
    [InlineData("Les", "LES")]
    [InlineData("Le Scorpion", "SCORPION")]
    [InlineData("La Quête de l'oiseau du temps", "QUETE DE L OISEAU DU TEMPS")]
    [InlineData("Un Ours", "OURS")]
    [InlineData("Une Vie", "VIE")]
    [InlineData("Des Dieux", "DIEUX")]
    [InlineData("The Walking Dead", "WALKING DEAD")]
    [InlineData("A Silent Voice", "SILENT VOICE")]
    [InlineData(null, "")]
    public void Normalize_Should_IgnoreCaseDiacriticsPunctuationAndLeadingArticle(string? serie, string expected)
    {
        SerieMatcher.Normalize(serie).Should().Be(expected);
    }

    [Fact]
    public void Similarity_Should_ReturnOne_WhenSeriesOnlyDifferByAccentsCaseOrArticle()
    {
        SerieMatcher.Similarity("Les Vieux Fourneaux", "vieux fourneaux").Should().Be(1);
        SerieMatcher.Similarity("Astérix", "ASTERIX").Should().Be(1);
    }

    [Fact]
    public void Similarity_Should_BeAboveThreshold_WhenSeriesHaveATypo()
    {
        SerieMatcher.Similarity("Spirou et Fantasio", "Spirou et Fantazio").Should().BeGreaterThanOrEqualTo(SerieMatcher.ProbableMatchThreshold);
    }

    [Fact]
    public void Similarity_Should_BeBelowThreshold_WhenSeriesAreDifferent()
    {
        SerieMatcher.Similarity("Blacksad", "Largo Winch").Should().BeLessThan(SerieMatcher.ProbableMatchThreshold);
        SerieMatcher.Similarity("Blacksad", null).Should().Be(0);
    }

    // 1 - Levenshtein distance / length of the longer series.
    [Theory]
    [InlineData("Kitten", "Sitting", 3, 7)]
    [InlineData("Sitting", "Kitten", 3, 7)]
    [InlineData("Abcd", "Abc", 1, 4)]
    [InlineData("Abc", "Xyz", 3, 3)]
    public void Similarity_Should_BeTheNormalizedLevenshteinRatio(string left, string right, int distance, int longerLength)
    {
        SerieMatcher.Similarity(left, right).Should().BeApproximately(1 - ((double)distance / longerLength), 1e-9);
    }

    [Fact]
    public void Similarity_Should_BeZero_WhenBothSeriesAreBlank()
    {
        SerieMatcher.Similarity(" ", null).Should().Be(0);
    }
}
