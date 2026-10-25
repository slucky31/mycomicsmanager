using Application.Interfaces;

namespace Application.UnitTests.ComicInfoSearch;

public sealed class BookTextSearchTests
{
    private static BookCandidate Candidate(string title, int? volume, string? isbn) =>
        new(title, "Astérix", volume, [], null, isbn, null);

    [Theory]
    [InlineData("Astérix et les Goths", "Astérix", 3, "Astérix 3 Astérix et les Goths")]
    [InlineData("Blacksad", " Blacksad ", 0, "Blacksad")]
    [InlineData(null, "Okko", null, "Okko")]
    [InlineData("Le Noyé à deux têtes", null, null, "Le Noyé à deux têtes")]
    public void ToText_Should_JoinTheSerieTheVolumeAndTheTitle_WhenTheyAreKnown(string? title, string? serie, int? volume, string expected)
    {
        new BookTextQuery(title, serie, volume).ToText().Should().Be(expected);
    }

    [Theory]
    [InlineData(null, null, true)]
    [InlineData(" ", "", true)]
    [InlineData("Title", null, false)]
    [InlineData(null, "Serie", false)]
    public void IsEmpty_Should_BeTrue_WhenNeitherTitleNorSerieIsGiven(string? title, string? serie, bool expected)
    {
        new BookTextQuery(title, serie, 3).IsEmpty.Should().Be(expected);
    }

    [Theory]
    [InlineData("978-2-01-210135-7", "9782012101357")]
    [InlineData("2-84789-164-1", "2847891641")]
    [InlineData("978-2-01-210135-2", null)]
    [InlineData(" ", null)]
    [InlineData(null, null)]
    public void ValidIsbn_Should_NormalizeTheIsbn_WhenItIsValid(string? value, string? expected)
    {
        BookCandidate.ValidIsbn(value).Should().Be(expected);
    }

    [Fact]
    public void RankedFor_Should_PutTheVolumeSearchedFirst_ThenTheEditionsWithAnIsbn()
    {
        var result = new BookCandidatesResult(
        [
            Candidate("Le Gaulois", 1, "9782012101333"),
            Candidate("Les Goths, 1963", 3, null),
            Candidate("Intégrale", null, null),
            Candidate("Les Goths, 2004", 3, "9782012101357"),
            Candidate("La serpe d'or", 2, "9782012101340"),
        ]);

        var ranked = result.RankedFor(new BookTextQuery("Astérix et les Goths", "Astérix", 3));

        ranked.Candidates.Select(c => c.Title).Should().Equal(
            "Les Goths, 2004", "Les Goths, 1963", "Le Gaulois", "La serpe d'or", "Intégrale");
    }

    [Fact]
    public void RankedFor_Should_KeepTheFirstCandidates_WhenTheSourceGivesTooMany()
    {
        var result = new BookCandidatesResult([.. Enumerable.Range(1, 15).Select(i => Candidate($"T{i}", i, null))]);

        var ranked = result.RankedFor(new BookTextQuery(null, "Astérix", null));

        ranked.Candidates.Should().HaveCount(BookCandidatesResult.MaxCandidates);
        ranked.Candidates[0].Title.Should().Be("T1");
        ranked.Found.Should().BeTrue();
    }
}
