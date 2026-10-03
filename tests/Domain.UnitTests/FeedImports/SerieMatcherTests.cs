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
}
