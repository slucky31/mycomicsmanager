using Application.FeedImports.List;
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

    [Fact]
    public void HostOptionsFrom_Should_LeaveOutNoLink_WhenEveryDecisionHasALink()
    {
        var options = FeedImportHostOptions.From(new FeedImportHostCounts(0, [new FeedImportHostCount("1fichier.com", 2)], 0));

        options.Options.Select(o => (o.Host, o.Label)).Should().Equal(((string?)null, "All hosts"), ("1fichier.com", "1fichier.com · 2"));
    }

    [Fact]
    public void HostOptionsEmpty_Should_OfferAllTheHostsOnly() =>
        FeedImportHostOptions.Empty.Options.Should().Equal(new FeedImportHostOption(null, "All hosts"));

    [Theory]
    [InlineData(null, "Host: All")]
    [InlineData(FeedImportConstants.NoHost, "Host: No link")]
    [InlineData("1fichier.com", "Host: 1fichier.com")]
    public void HostButtonLabel_Should_NameTheHostChosen(string? host, string expected) =>
        FeedImportFilters.HostButtonLabel(host).Should().Be(expected);

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
