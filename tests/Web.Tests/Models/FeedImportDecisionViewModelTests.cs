using AwesomeAssertions;
using Domain.FeedImports;
using MudBlazor;
using Web.Models;
using Xunit;

namespace Web.Tests.Models;

public sealed class FeedImportDecisionViewModelTests
{
    [Fact]
    public void From_Should_PrecomputeDisplayValues_WhenDecisionIsPending()
    {
        var publishedAt = new DateTime(2026, 9, 30, 18, 0, 0, DateTimeKind.Utc);
        var decision = FeedImportDecision.Create(Guid.CreateVersion7(), 12, "Blacksad T3", "https://planete-bd.org/12", publishedAt).Value!;

        var viewModel = FeedImportDecisionViewModel.From(decision);

        viewModel.Id.Should().Be(decision.Id);
        viewModel.EntryTitle.Should().Be("Blacksad T3");
        viewModel.EntryUrl.Should().Be("https://planete-bd.org/12");
        viewModel.PublishedAt.Should().Be(publishedAt);
        viewModel.Status.Should().Be(FeedImportDecisionStatus.Pending);
        viewModel.StatusDisplay.Should().Be("En attente");
        viewModel.StatusColor.Should().Be(Color.Default);
        viewModel.Reason.Should().Be(FeedImportDecision.CreatedReason);
        viewModel.DecidedByDisplay.Should().Be("Automatique");
        viewModel.ParsedDisplay.Should().BeNull();
        viewModel.ErrorDisplay.Should().BeNull();
        var evt = viewModel.Events.Should().ContainSingle().Subject;
        evt.StatusDisplay.Should().Be("En attente");
        evt.Description.Should().Be(FeedImportDecision.CreatedReason);
    }

    [Fact]
    public void GetStatusDisplay_Should_ReturnLabel_ForEveryStatus()
    {
        FeedImportDecisionViewModel.FilterableStatuses.Should().HaveCount(Enum.GetValues<FeedImportDecisionStatus>().Length);
        foreach (var status in FeedImportDecisionViewModel.FilterableStatuses)
        {
            FeedImportDecisionViewModel.GetStatusDisplay(status).Should().NotBe(status.ToString());
        }
    }
}
