using AwesomeAssertions;
using Web.Models;
using Xunit;

namespace Web.Tests.Models;

public sealed class VirtualRowsTests
{
    private static readonly string[] s_items = ["a", "b", "c", "d", "e", "f", "g"];

    [Fact]
    public void Equals_Should_MatchTheRowsThatStartAtTheSameItem()
    {
        var row = new VirtualRow<string>(0, ["a", "b"]);
        var comparer = new VirtualRowComparer<string>(item => item);

        comparer.Equals(row, new VirtualRow<string>(0, ["a"])).Should().BeTrue();
        comparer.GetHashCode(row).Should().Be(comparer.GetHashCode(new VirtualRow<string>(0, ["a"])));
        comparer.Equals(row, new VirtualRow<string>(0, ["b"])).Should().BeFalse();
        comparer.Equals(row, new VirtualRow<string>(3, ["a"])).Should().BeFalse();
        comparer.Equals(new VirtualRow<string>(0, []), new VirtualRow<string>(0, [])).Should().BeTrue();
        comparer.Equals(row, null).Should().BeFalse();
        comparer.Equals(row, row).Should().BeTrue();
    }

    [Fact]
    public async Task GetRowsAsync_Should_CutTheItemsIntoRowsOfTheColumns_WhenTheRowsAreAskedFor()
    {
        var rows = new ListRows<string>(s_items, "All");

        var result = await rows.GetRowsAsync(1, 5, 3, TestContext.Current.CancellationToken);

        result.Value!.RowCount.Should().Be(3);
        result.Value.Rows.Select(r => r.FirstItem).Should().Equal(3, 6);
        result.Value.Rows[0].Items.Should().Equal("d", "e", "f");
        result.Value.Rows[1].Items.Should().Equal("g");
    }

    [Fact]
    public async Task GetRowsAsync_Should_GiveNoRow_WhenThereIsNoItem()
    {
        var result = await new ListRows<string>([], "All").GetRowsAsync(0, 4, 3, TestContext.Current.CancellationToken);

        result.Value!.RowCount.Should().Be(0);
        result.Value.Rows.Should().BeEmpty();
    }

    [Theory]
    [InlineData(-1, 1, 1)]
    [InlineData(0, -1, 1)]
    [InlineData(0, 1, 0)]
    public async Task GetRowsAsync_Should_Throw_WhenTheRequestIsOutOfRange(int startRow, int rowCount, int columns)
    {
        var act = () => new ListRows<string>(s_items, "All").GetRowsAsync(startRow, rowCount, columns, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void ShowsOtherItemsThan_Should_TellWhetherTheScopeChanged()
    {
        var rows = new ListRows<string>(s_items, "All");

        rows.ShowsOtherItemsThan(new ListRows<string>(["a"], "All")).Should().BeFalse();
        rows.ShowsOtherItemsThan(new ListRows<string>(s_items, "Complete")).Should().BeTrue();
        rows.ShowsOtherItemsThan(null).Should().BeTrue();
    }
}
