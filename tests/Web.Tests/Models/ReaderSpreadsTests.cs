using AwesomeAssertions;
using Domain.Books;
using Web.Models;
using Xunit;

namespace Web.Tests.Models;

public sealed class ReaderSpreadsTests
{
    private static readonly ReaderSettings s_twoPages = new(ReaderLayout.TwoPages, ReaderScrolling.Continuous, ReadingDirection.LeftToRight);
    private static readonly ReaderSettings s_twoPagesManga = new(ReaderLayout.TwoPages, ReaderScrolling.PageByPage, ReadingDirection.RightToLeft);

    private static int[][] Rows(ReaderSpreads spreads) =>
        Enumerable.Range(0, spreads.Count).Select(row => spreads.PagesOf(row).ToArray()).ToArray();

    [Theory]
    [InlineData(ReaderLayout.FitWidth)]
    [InlineData(ReaderLayout.SinglePage)]
    public void Build_Should_PutOnePagePerRow_WhenTheLayoutShowsOnePage(ReaderLayout layout)
    {
        var spreads = ReaderSpreads.Build(3, ReaderSettings.Default with { Layout = layout, Direction = ReadingDirection.RightToLeft });

        int[][] expected = [[0], [1], [2]];
        Rows(spreads).Should().BeEquivalentTo(expected, o => o.WithStrictOrdering());
    }

    [Fact]
    public void Build_Should_KeepTheCoverAloneThenPairThePages()
    {
        var spreads = ReaderSpreads.Build(6, s_twoPages);

        int[][] expected = [[0], [1, 2], [3, 4], [5]];
        Rows(spreads).Should().BeEquivalentTo(expected, o => o.WithStrictOrdering());
    }

    [Fact]
    public void Build_Should_PutTheFirstPageOfAPairOnTheRight_WhenReadingRightToLeft()
    {
        var spreads = ReaderSpreads.Build(5, s_twoPagesManga);

        int[][] expected = [[0], [2, 1], [4, 3]];
        Rows(spreads).Should().BeEquivalentTo(expected, o => o.WithStrictOrdering());
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(3, 2)]
    [InlineData(4, 2)]
    [InlineData(-5, 0)]
    [InlineData(99, 3)]
    public void RowOf_Should_FindTheRowShowingThePage(int pageIndex, int expectedRow)
    {
        var spreads = ReaderSpreads.Build(6, s_twoPagesManga);

        spreads.RowOf(pageIndex).Should().Be(expectedRow);
    }

    [Fact]
    public void FirstPageOfAndLastPageOf_Should_IgnoreTheScreenOrder()
    {
        var spreads = ReaderSpreads.Build(6, s_twoPagesManga);

        spreads.FirstPageOf(1).Should().Be(1);
        spreads.LastPageOf(1).Should().Be(2);
    }

    [Theory]
    [InlineData(0, "1 / 6")]
    [InlineData(1, "2-3 / 6")]
    [InlineData(3, "6 / 6")]
    public void Counter_Should_ShowThePagesOfTheRow(int row, string expected)
    {
        var spreads = ReaderSpreads.Build(6, s_twoPages);

        spreads.Counter(row).Should().Be(expected);
    }

    [Fact]
    public void Build_Should_Throw_WhenTheBookHasNoPage()
    {
        var act = () => ReaderSpreads.Build(0, ReaderSettings.Default);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}
