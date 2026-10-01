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
        var page1 = await FeedImportDecisionReadService.GetPagedAsync(userId, null, null, 1, 3, TestContext.Current.CancellationToken);
        var page2 = await FeedImportDecisionReadService.GetPagedAsync(userId, null, null, 2, 3, TestContext.Current.CancellationToken);

        // Assert
        page1.TotalCount.Should().Be(5);
        page1.Items.Should().HaveCount(3);
        page2.Items.Should().HaveCount(2);
        var allIds = page1.Items!.Concat(page2.Items!).Select(d => d.Id).ToList();
        allIds.Should().OnlyHaveUniqueItems();
        page1.Items!.Should().AllSatisfy(d => d.Events.Should().ContainSingle());
    }

    [Fact]
    public async Task GetPagedAsync_Should_FilterByStatusAndSearchTerm()
    {
        // Arrange
        var userId = Guid.CreateVersion7();
        await SeedAsync(userId, "Blacksad - Tome 3", "Astérix 100%", "Largo Winch");

        // Act
        var byTitle = await FeedImportDecisionReadService.GetPagedAsync(userId, null, "blacksad", 1, 10, TestContext.Current.CancellationToken);
        var byLikeChar = await FeedImportDecisionReadService.GetPagedAsync(userId, null, "%", 1, 10, TestContext.Current.CancellationToken);
        var byOtherStatus = await FeedImportDecisionReadService.GetPagedAsync(userId, FeedImportDecisionStatus.Failed, null, 1, 10, TestContext.Current.CancellationToken);
        var byPending = await FeedImportDecisionReadService.GetPagedAsync(userId, FeedImportDecisionStatus.Pending, null, 1, 10, TestContext.Current.CancellationToken);

        // Assert
        byTitle.Items.Should().ContainSingle().Which.EntryTitle.Should().Be("Blacksad - Tome 3");
        byLikeChar.Items.Should().ContainSingle().Which.EntryTitle.Should().Be("Astérix 100%");
        byOtherStatus.TotalCount.Should().Be(0);
        byPending.TotalCount.Should().Be(3);
    }
}
