using Base.Integration.Tests;
using Domain.FeedImports;

namespace Persistence.Tests.Integration.Queries;

[Collection("DatabaseCollectionTests")]
public class FeedImportDecisionReadServiceTests(IntegrationTestWebAppFactory factory) : FeedImportDecisionIntegrationTest(factory)
{
    private async Task<List<FeedImportDecision>> SeedAsync(Guid userId, params string[] titles)
    {
        var decisions = new List<FeedImportDecision>();
        for (var i = 0; i < titles.Length; i++)
        {
            var decision = FeedImportDecision.Create(userId, 5000 + i, titles[i], $"https://zone-ebook.com/{i}", null).Value!;
            FeedImportDecisionRepository.Add(decision);
            decisions.Add(decision);
        }
        await UnitOfWork.SaveChangesAsync(TestContext.Current.CancellationToken);
        return decisions;
    }

    [Fact]
    public async Task GetPagedAsync_Should_ReturnOnlyUserDecisionsWithStableOrder_WhenPaging()
    {
        // Arrange
        var userId = Guid.CreateVersion7();
        await SeedAsync(userId, "A", "B", "C", "D", "E");
        await SeedAsync(Guid.CreateVersion7(), "Other user");

        // Act
        var page1 = await FeedImportDecisionReadService.GetPagedAsync(userId, null, null, null, 1, 3, TestContext.Current.CancellationToken);
        var page2 = await FeedImportDecisionReadService.GetPagedAsync(userId, null, null, null, 2, 3, TestContext.Current.CancellationToken);

        // Assert
        page1.TotalCount.Should().Be(5);
        page1.Items.Should().HaveCount(3);
        page2.Items.Should().HaveCount(2);
        var allIds = page1.Items.Concat(page2.Items).Select(d => d.Id).ToList();
        allIds.Should().OnlyHaveUniqueItems();
        page1.Items.Should().AllSatisfy(d => d.Events.Should().ContainSingle());
    }

    [Fact]
    public async Task GetPagedAsync_Should_FilterByStatusAndSearchTerm()
    {
        // Arrange
        var userId = Guid.CreateVersion7();
        await SeedAsync(userId, "Blacksad - Tome 3", "Astérix 100%", "Largo Winch");

        // Act
        var byTitle = await FeedImportDecisionReadService.GetPagedAsync(userId, null, null, "blacksad", 1, 10, TestContext.Current.CancellationToken);
        var byLikeChar = await FeedImportDecisionReadService.GetPagedAsync(userId, null, null, "%", 1, 10, TestContext.Current.CancellationToken);
        var byOtherStatus = await FeedImportDecisionReadService.GetPagedAsync(userId, FeedImportDecisionStatus.Failed, null, null, 1, 10, TestContext.Current.CancellationToken);
        var byPending = await FeedImportDecisionReadService.GetPagedAsync(userId, FeedImportDecisionStatus.Pending, null, null, 1, 10, TestContext.Current.CancellationToken);

        // Assert
        byTitle.Items.Should().ContainSingle().Which.EntryTitle.Should().Be("Blacksad - Tome 3");
        byLikeChar.Items.Should().ContainSingle().Which.EntryTitle.Should().Be("Astérix 100%");
        byOtherStatus.TotalCount.Should().Be(0);
        byPending.TotalCount.Should().Be(3);
    }

    [Fact]
    public async Task GetPagedAsync_Should_KeepBooksOfAnArticleTogether_AndReportMultiBookEntries()
    {
        // Arrange
        var userId = Guid.CreateVersion7();
        var article = FeedImportDecision.Create(userId, 7000, "Blacksad - Tomes 1 à 2", "https://planete-bd.org/7000", null).Value!;
        FeedImportDecisionRepository.Add(article);
        await UnitOfWork.SaveChangesAsync(TestContext.Current.CancellationToken);
        var later = (await SeedAsync(userId, "Plus récent"))[0];
        FeedImportDecisionRepository.Add(article.CreateSibling(1).Value!);
        await UnitOfWork.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var page = await FeedImportDecisionReadService.GetPagedAsync(userId, null, null, null, 1, 10, TestContext.Current.CancellationToken);
        var multiBook = await FeedImportDecisionReadService.GetMultiBookEntryIdsAsync(
            userId, page.Items!.Select(d => d.MinifluxEntryId).ToList(), TestContext.Current.CancellationToken);

        // Assert
        page.Items!.Select(d => (d.MinifluxEntryId, d.ItemIndex)).Should().Equal(
            (later.MinifluxEntryId, 0), (7000L, 0), (7000L, 1));
        multiBook.Should().BeEquivalentTo([7000L]);
    }

    [Fact]
    public async Task CountByStatusAsync_Should_CountOnlyUserDecisionsWithStatus()
    {
        // Arrange
        var userId = Guid.CreateVersion7();
        var decisions = await SeedAsync(userId, "A", "B", "C");
        decisions[0].Fail("Source", "Domaine source non autorisé.");
        decisions[1].Fail("Source", "Domaine source non autorisé.");
        await UnitOfWork.SaveChangesAsync(TestContext.Current.CancellationToken);
        await SeedAsync(Guid.CreateVersion7(), "Other user");

        // Act
        var failed = await FeedImportDecisionReadService.CountByStatusAsync(userId, FeedImportDecisionStatus.Failed, TestContext.Current.CancellationToken);
        var pending = await FeedImportDecisionReadService.CountByStatusAsync(userId, FeedImportDecisionStatus.Pending, TestContext.Current.CancellationToken);

        // Assert
        failed.Should().Be(2);
        pending.Should().Be(1);
    }

    [Fact]
    public async Task CountPerStatusAsync_Should_CountUserDecisionsOfEachStatus()
    {
        // Arrange
        var userId = Guid.CreateVersion7();
        var decisions = await SeedAsync(userId, "A", "B", "C");
        decisions[0].Fail("Source", "Domaine source non autorisé.");
        await UnitOfWork.SaveChangesAsync(TestContext.Current.CancellationToken);
        await SeedAsync(Guid.CreateVersion7(), "Other user");

        // Act
        var counts = await FeedImportDecisionReadService.CountPerStatusAsync(userId, null, TestContext.Current.CancellationToken);

        // Assert
        counts.Should().BeEquivalentTo(new Dictionary<FeedImportDecisionStatus, int>
        {
            [FeedImportDecisionStatus.Failed] = 1,
            [FeedImportDecisionStatus.Pending] = 2,
        });
    }

    private static readonly ParsedComicTitle s_parsed = new("Blacksad", null, 1);

    private static DownloadCandidate Candidate(params string[] hosts) =>
        new("Blacksad T01", null, null, [.. hosts.Select(h => new DownloadMirror($"https://{h}/file", h))]);

    // A: 1fichier + rapidgator, B: turbobit (failed), C: no link.
    private async Task<List<FeedImportDecision>> SeedWithHostsAsync(Guid userId)
    {
        var decisions = await SeedAsync(userId, "A", "B", "C");
        decisions[0].RecordLinks([Candidate("1fichier.com", "rapidgator.net")], s_parsed, "Links found", FeedImportDecidedBy.Auto);
        decisions[1].FailWithLinks([Candidate("turbobit.net")], s_parsed, "Link extraction", "Host not supported");
        await UnitOfWork.SaveChangesAsync(TestContext.Current.CancellationToken);
        await SeedAsync(Guid.CreateVersion7(), "Other user");
        return decisions;
    }

    [Fact]
    public async Task GetPagedAsync_Should_ShowTheDecisionsOfAHost_OrThoseWithoutLink()
    {
        // Arrange
        var userId = Guid.CreateVersion7();
        await SeedWithHostsAsync(userId);

        // Act
        var onRapidgator = await FeedImportDecisionReadService.GetPagedAsync(userId, null, "rapidgator.net", null, 1, 10, TestContext.Current.CancellationToken);
        var withoutLink = await FeedImportDecisionReadService.GetPagedAsync(userId, null, FeedImportConstants.NoHost, null, 1, 10, TestContext.Current.CancellationToken);
        var failedOnTurbobit = await FeedImportDecisionReadService.GetPagedAsync(
            userId, FeedImportDecisionStatus.Failed, "turbobit.net", null, 1, 10, TestContext.Current.CancellationToken);

        // Assert
        onRapidgator.Items!.Select(d => d.EntryTitle).Should().Equal("A");
        onRapidgator.Items!.First().Hosts.Should().Equal("1fichier.com", "rapidgator.net");
        withoutLink.Items!.Select(d => d.EntryTitle).Should().Equal("C");
        failedOnTurbobit.Items!.Select(d => d.EntryTitle).Should().Equal("B");
    }

    [Fact]
    public async Task CountPerHostAsync_Should_CountTheDecisionsOfEachHost_InTheStatusGiven()
    {
        // Arrange
        var userId = Guid.CreateVersion7();
        await SeedWithHostsAsync(userId);

        // Act
        var all = await FeedImportDecisionReadService.CountPerHostAsync(userId, null, TestContext.Current.CancellationToken);
        var failed = await FeedImportDecisionReadService.CountPerHostAsync(userId, FeedImportDecisionStatus.Failed, TestContext.Current.CancellationToken);

        // Assert
        all.All.Should().Be(3);
        all.NoHost.Should().Be(1);
        all.Hosts.Select(h => (h.Host, h.Count)).Should().Equal(("1fichier.com", 1), ("rapidgator.net", 1), ("turbobit.net", 1));
        failed.All.Should().Be(1);
        failed.NoHost.Should().Be(0);
        failed.Hosts.Select(h => (h.Host, h.Count)).Should().Equal(("turbobit.net", 1));
    }

    [Fact]
    public async Task CountPerStatusAsync_Should_CountTheDecisionsOfTheHostGiven()
    {
        // Arrange
        var userId = Guid.CreateVersion7();
        await SeedWithHostsAsync(userId);

        // Act
        var onTurbobit = await FeedImportDecisionReadService.CountPerStatusAsync(userId, "turbobit.net", TestContext.Current.CancellationToken);
        var withoutLink = await FeedImportDecisionReadService.CountPerStatusAsync(userId, FeedImportConstants.NoHost, TestContext.Current.CancellationToken);

        // Assert
        onTurbobit.Should().BeEquivalentTo(new Dictionary<FeedImportDecisionStatus, int> { [FeedImportDecisionStatus.Failed] = 1 });
        withoutLink.Should().BeEquivalentTo(new Dictionary<FeedImportDecisionStatus, int> { [FeedImportDecisionStatus.Pending] = 1 });
    }
}
