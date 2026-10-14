using AwesomeAssertions;
using Domain.FeedImports;
using Web.Models;
using Xunit;

namespace Web.Tests.Models;

public sealed class FeedImportFiltersTests
{
    [Fact]
    public void From_Should_SumEveryStatusAndPickTheFilteredOnes()
    {
        var counts = FeedImportStatusCounts.From(new Dictionary<FeedImportDecisionStatus, int>
        {
            [FeedImportDecisionStatus.AwaitingArbitration] = 1,
            [FeedImportDecisionStatus.Failed] = 14,
            [FeedImportDecisionStatus.Ignored] = 3,
        });

        counts.Should().Be(new FeedImportStatusCounts(18, 1, 14, 0));
        counts.Chips.Select(c => (c.Filter, c.Label)).Should().Equal(
            (FeedImportFilter.All, "All · 18"),
            (FeedImportFilter.ToDecide, "To decide · 1"),
            (FeedImportFilter.Failed, "Failed · 14"),
            (FeedImportFilter.Downloaded, "Downloaded"));
    }

    [Fact]
    public void Empty_Should_ShowTheNamesOnly()
    {
        FeedImportStatusCounts.Empty.Chips.Select(c => c.Label).Should().Equal("All", "To decide", "Failed", "Downloaded");
    }

    [Theory]
    [InlineData(FeedImportFilter.All, null)]
    [InlineData(FeedImportFilter.ToDecide, FeedImportDecisionStatus.AwaitingArbitration)]
    [InlineData(FeedImportFilter.Failed, FeedImportDecisionStatus.Failed)]
    [InlineData(FeedImportFilter.Downloaded, FeedImportDecisionStatus.Downloaded)]
    public void StatusOf_Should_MapTheFilterToItsStatus(FeedImportFilter filter, FeedImportDecisionStatus? expected)
    {
        FeedImportFilters.StatusOf(filter).Should().Be(expected);
    }
}
