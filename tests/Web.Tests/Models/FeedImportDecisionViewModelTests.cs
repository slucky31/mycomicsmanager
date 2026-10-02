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

    [Fact]
    public void From_Should_ExposeCandidatesAndArbitrationActions_WhenLinksAreAmbiguous()
    {
        var decision = FeedImportDecision.Create(Guid.CreateVersion7(), 12, "Blacksad - Tome 3", "https://planete-bd.org/12", null).Value!;
        var candidate = new DownloadCandidate("Blacksad T03", "Blacksad T03.cbz", 52_428_800,
            [new DownloadMirror("https://1fichier.com/?a", "1fichier.com")]);
        decision.RequestArbitration(FeedImportArbitrationKind.AmbiguousLinks, [candidate, candidate with { Label = "Autre" }],
            new ParsedComicTitle("Blacksad", null, 3), null, "Ambigu", FeedImportDecidedBy.Auto);

        var viewModel = FeedImportDecisionViewModel.From(decision);

        viewModel.CanChooseCandidate.Should().BeTrue();
        viewModel.CanResolveDuplicate.Should().BeFalse();
        viewModel.StatusColor.Should().Be(Color.Warning);
        viewModel.ParsedDisplay.Should().Be("Blacksad · T3");
        viewModel.Candidates.Should().HaveCount(2);
        viewModel.Candidates[0].Index.Should().Be(0);
        viewModel.Candidates[0].SizeDisplay.Should().Be("50.0 Mo");
        viewModel.Candidates[0].Mirrors.Should().ContainSingle().Which.Host.Should().Be("1fichier.com");
        viewModel.Candidates[1].Index.Should().Be(1);
    }

    [Fact]
    public void From_Should_FlagDuplicateResolutionAndItemIndex_WhenSiblingIsProbableDuplicate()
    {
        var original = FeedImportDecision.Create(Guid.CreateVersion7(), 12, "Blacksad - Tomes 1 à 2", "https://planete-bd.org/12", null).Value!;
        var sibling = original.CreateSibling(1).Value!;
        var bookId = Guid.CreateVersion7();
        sibling.RequestArbitration(FeedImportArbitrationKind.ProbableDuplicate,
            [new DownloadCandidate("Blacksad_T02", "Blacksad_T02.cbz", null, [new DownloadMirror("https://1fichier.com/?b", "1fichier.com")])],
            new ParsedComicTitle("Blacksad", null, 2), bookId, "Doublon probable", FeedImportDecidedBy.Auto);

        var viewModel = FeedImportDecisionViewModel.From(sibling);

        viewModel.CanResolveDuplicate.Should().BeTrue();
        viewModel.CanChooseCandidate.Should().BeFalse();
        viewModel.MatchedBookId.Should().Be(bookId);
        viewModel.ItemDisplay.Should().Be("Livre 2 de l'article");
        viewModel.Candidates.Should().ContainSingle().Which.SizeDisplay.Should().BeNull();
    }
}
