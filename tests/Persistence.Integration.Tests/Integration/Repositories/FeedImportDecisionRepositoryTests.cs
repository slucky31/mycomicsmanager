using Base.Integration.Tests;
using Domain.FeedImports;
using Microsoft.EntityFrameworkCore;

namespace Persistence.Tests.Integration.Repositories;

[Collection("DatabaseCollectionTests")]
public class FeedImportDecisionRepositoryTests(IntegrationTestWebAppFactory factory) : FeedImportDecisionIntegrationTest(factory)
{
    private static FeedImportDecision CreateDecision(Guid userId, long entryId, string title = "Blacksad T3")
        => FeedImportDecision.Create(userId, entryId, title, $"https://planete-bd.org/{entryId}", DateTime.UtcNow).Value!;

    [Fact]
    public async Task GetByMinifluxEntryIdAsync_Should_ReturnDecisionWithEvents_WhenItExistsForUser()
    {
        // Arrange
        var userId = Guid.CreateVersion7();
        var decision = CreateDecision(userId, 1001);
        FeedImportDecisionRepository.Add(decision);
        await UnitOfWork.SaveChangesAsync(TestContext.Current.CancellationToken);
        Context.ChangeTracker.Clear();

        // Act
        var found = await FeedImportDecisionRepository.GetByMinifluxEntryIdAsync(userId, 1001, TestContext.Current.CancellationToken);
        var otherUser = await FeedImportDecisionRepository.GetByMinifluxEntryIdAsync(Guid.CreateVersion7(), 1001, TestContext.Current.CancellationToken);

        // Assert
        found.Should().NotBeNull();
        found.Id.Should().Be(decision.Id);
        found.Status.Should().Be(FeedImportDecisionStatus.Pending);
        otherUser.Should().BeNull();
        var events = await Context.FeedImportDecisionEvents
            .Where(e => e.FeedImportDecisionId == decision.Id)
            .ToListAsync(TestContext.Current.CancellationToken);
        events.Should().ContainSingle().Which.Status.Should().Be(FeedImportDecisionStatus.Pending);
    }

    [Fact]
    public async Task SaveChangesAsync_Should_Fail_WhenSameMinifluxEntryIsAddedTwiceForSameUser()
    {
        // Arrange
        var userId = Guid.CreateVersion7();
        FeedImportDecisionRepository.Add(CreateDecision(userId, 2002));
        (await UnitOfWork.SaveChangesAsync(TestContext.Current.CancellationToken)).IsSuccess.Should().BeTrue();

        // Act
        FeedImportDecisionRepository.Add(CreateDecision(userId, 2002));
        var result = await UnitOfWork.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task SaveChangesAsync_Should_Succeed_WhenSameMinifluxEntryBelongsToAnotherUser()
    {
        // Arrange
        FeedImportDecisionRepository.Add(CreateDecision(Guid.CreateVersion7(), 3003));
        FeedImportDecisionRepository.Add(CreateDecision(Guid.CreateVersion7(), 3003));

        // Act
        var result = await UnitOfWork.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }
}
