using Application.Books;
using Application.FeedImports.Analysis;
using Domain.FeedImports;

namespace Application.UnitTests.FeedImports;

public class DuplicateBookFinderTests
{
    private static readonly BookIdentityDto s_blacksad3 = new(Guid.CreateVersion7(), "Blacksad", "Âme rouge", 3, "9782205055819", "BD");
    private static readonly BookIdentityDto s_spirou12 = new(Guid.CreateVersion7(), "Spirou et Fantasio", "Le Nid des Marsupilamis", 12, null, "BD numériques");
    private static readonly BookIdentityDto[] s_books = [s_blacksad3, s_spirou12];

    [Fact]
    public void Find_Should_ReturnCertain_WhenIsbnMatches()
    {
        var match = DuplicateBookFinder.Find(new ParsedComicTitle("Autre", null, 9, "978-2-205-05581-9"), s_books);

        match.Kind.Should().Be(DuplicateMatchKind.Certain);
        match.Book.Should().Be(s_blacksad3);
    }

    [Fact]
    public void Find_Should_ReturnCertain_WhenSameNormalizedSerieAndVolume()
    {
        var match = DuplicateBookFinder.Find(new ParsedComicTitle("BLACKSAD", null, 3), s_books);

        match.Kind.Should().Be(DuplicateMatchKind.Certain);
        match.Book.Should().Be(s_blacksad3);
    }

    [Fact]
    public void Find_Should_ReturnProbable_WhenSerieIsCloseAndVolumeIsTheSame()
    {
        var match = DuplicateBookFinder.Find(new ParsedComicTitle("Spirou et Fantazio", null, 12), s_books);

        match.Kind.Should().Be(DuplicateMatchKind.Probable);
        match.Book.Should().Be(s_spirou12);
    }

    [Fact]
    public void Find_Should_ReturnProbable_WhenSerieIsTheSameButVolumeIsUnknown()
    {
        var match = DuplicateBookFinder.Find(new ParsedComicTitle("Blacksad", "Intégrale", null), s_books);

        match.Kind.Should().Be(DuplicateMatchKind.Probable);
        match.Book.Should().Be(s_blacksad3);
    }

    [Theory]
    [InlineData("Blacksad", 4)]
    [InlineData("Largo Winch", 3)]
    [InlineData(null, 3)]
    public void Find_Should_ReturnNone_WhenNoBookMatches(string? serie, int volume)
    {
        DuplicateBookFinder.Find(new ParsedComicTitle(serie, null, volume), s_books).Should().Be(DuplicateMatch.None);
    }
}
