using AwesomeAssertions;
using Web.Models;
using Xunit;

namespace Web.Tests.Models;

public sealed class BookDetailsComparisonTests
{
    private static readonly BookFieldDefinition s_title = BookFieldDefinition.All[0];

    private static BookFieldComparison Compare(string? current, params (BookSource Source, string? Value)[] candidates) =>
        BookFieldComparison.Compare(s_title, current, candidates);

    private static BookFieldComparison Field(int index, BookFieldMatch match) =>
        new(BookFieldDefinition.All[index], [new BookFieldOption("v", [BookSource.Current])], match);

    // ── BookFieldComparison.Compare ───────────────────────────────────────────

    [Fact]
    public void Compare_Should_GroupSourcesGivingTheSameValue_WhenTheyDiffer()
    {
        var comparison = Compare(" Extremity ",
            (BookSource.Bnf, "Extremity"),
            (BookSource.OpenLibrary, "EXTREMITY"),
            (BookSource.Google, " EXTREMITY"),
            (BookSource.Bedetheque, null));

        comparison.Match.Should().Be(BookFieldMatch.Differs);
        comparison.Options.Should().HaveCount(2);
        comparison.Options[0].Should().BeEquivalentTo(new { Value = "Extremity", Source = BookSource.Current, IsCurrent = true, SourcesLabel = "Current" });
        comparison.Options[1].Should().BeEquivalentTo(new { Value = "EXTREMITY", Source = BookSource.OpenLibrary, IsCurrent = false, SourcesLabel = "OpenLibrary · Google Books" });
    }

    [Fact]
    public void Compare_Should_Match_WhenEverySourceGivesTheCurrentValue()
    {
        var comparison = Compare("Extremity", (BookSource.Bnf, "Extremity"), (BookSource.Google, "  "));

        comparison.Match.Should().Be(BookFieldMatch.Matches);
        comparison.Options.Should().ContainSingle();
    }

    [Fact]
    public void Compare_Should_BeUnknown_WhenNoSourceGivesAValue()
    {
        var comparison = Compare(null, (BookSource.Bnf, null));

        comparison.Match.Should().Be(BookFieldMatch.Unknown);
        comparison.Options.Should().ContainSingle(o => o.Value == null && o.IsCurrent);
    }

    [Fact]
    public void Compare_Should_Differ_WhenTheBookHasNoValueYet()
    {
        var comparison = Compare("", (BookSource.Bedetheque, "Extremity"));

        comparison.Match.Should().Be(BookFieldMatch.Differs);
        comparison.Options.Select(o => (o.Value, o.SourcesLabel)).Should().Equal((null, "Current"), ("Extremity", "Bedetheque"));
    }

    // ── BookDetailsComparison ─────────────────────────────────────────────────

    [Fact]
    public void MatchSummary_Should_ListTheMatchingFields_WhenSeveralMatch()
    {
        var comparison = new BookDetailsComparison(
        [
            Field(0, BookFieldMatch.Matches),
            Field(1, BookFieldMatch.Matches),
            Field(2, BookFieldMatch.Unknown),
            Field(3, BookFieldMatch.Matches),
            Field(4, BookFieldMatch.Differs),
        ]);

        comparison.MatchSummary.Should().Be("Title, series and authors already match.");
        comparison.Differences.Should().ContainSingle(f => f.Field.Key == BookFieldKeys.Publishers);
        comparison.DifferencesSummary.Should().Be("1 difference found.");
    }

    [Fact]
    public void MatchSummary_Should_UseSingular_WhenOneFieldMatches()
    {
        var comparison = new BookDetailsComparison([Field(6, BookFieldMatch.Matches)]);

        comparison.MatchSummary.Should().Be("Pages already matches.");
        comparison.DifferencesSummary.Should().Be("No differences found.");
    }

    [Fact]
    public void MatchSummary_Should_BeNull_WhenNoFieldMatches()
    {
        var comparison = new BookDetailsComparison([Field(0, BookFieldMatch.Differs), Field(1, BookFieldMatch.Differs)]);

        comparison.MatchSummary.Should().BeNull();
        comparison.DifferencesSummary.Should().Be("2 differences found.");
    }

    [Fact]
    public void CountChanges_Should_CountTheFieldsWhereAnotherValueIsPicked()
    {
        var title = BookFieldComparison.Compare(BookFieldDefinition.All[0], "A", [(BookSource.Bnf, "B"), (BookSource.Google, "B")]);
        var pages = BookFieldComparison.Compare(BookFieldDefinition.All[6], "10", [(BookSource.Bnf, "12")]);
        var authors = BookFieldComparison.Compare(BookFieldDefinition.All[3], "X", [(BookSource.Bnf, "X")]);
        var comparison = new BookDetailsComparison([title, pages, authors]);

        var selected = new Dictionary<string, BookSource>
        {
            [BookFieldKeys.Title] = BookSource.Google,
            [BookFieldKeys.NumberOfPages] = BookSource.Current,
            [BookFieldKeys.Authors] = BookSource.Bnf,
        };

        comparison.CountChanges(selected).Should().Be(1);
    }

    [Theory]
    [InlineData(0, "Apply changes")]
    [InlineData(1, "Apply 1 change")]
    [InlineData(3, "Apply 3 changes")]
    public void ApplyLabel_Should_SayHowManyChanges(int changes, string expected)
    {
        BookDetailsComparison.ApplyLabel(changes).Should().Be(expected);
    }

    [Theory]
    [InlineData(BookSource.Current, "Current")]
    [InlineData(BookSource.Bnf, "BnF")]
    [InlineData(BookSource.Bedetheque, "Bedetheque")]
    [InlineData(BookSource.OpenLibrary, "OpenLibrary")]
    [InlineData(BookSource.Google, "Google Books")]
    public void Name_Should_NameEachSource(BookSource source, string expected)
    {
        BookSources.Name(source).Should().Be(expected);
    }
}
