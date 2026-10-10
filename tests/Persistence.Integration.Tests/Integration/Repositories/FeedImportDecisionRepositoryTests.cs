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

    [Fact]
    public async Task SaveChangesAsync_Should_AllowSiblingDecisionsForTheSameEntry()
    {
        // Arrange
        var userId = Guid.CreateVersion7();
        var decision = CreateDecision(userId, 4004);
        FeedImportDecisionRepository.Add(decision);
        FeedImportDecisionRepository.Add(decision.CreateSibling(1).Value!);

        // Act
        var result = await UnitOfWork.SaveChangesAsync(TestContext.Current.CancellationToken);
        var original = await FeedImportDecisionRepository.GetByMinifluxEntryIdAsync(userId, 4004, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.Should().BeTrue();
        original.Should().NotBeNull();
        original.Id.Should().Be(decision.Id);
    }

    [Fact]
    public async Task GetPendingIdsAsync_Should_ReturnOnlyPendingDecisionsOfUser_OldestFirst()
    {
        // Arrange
        var userId = Guid.CreateVersion7();
        var first = CreateDecision(userId, 5005);
        var second = CreateDecision(userId, 5006);
        var failed = CreateDecision(userId, 5007);
        failed.Fail("Source", "Domaine source non autorisé.");
        FeedImportDecisionRepository.Add(first);
        FeedImportDecisionRepository.Add(second);
        FeedImportDecisionRepository.Add(failed);
        FeedImportDecisionRepository.Add(CreateDecision(Guid.CreateVersion7(), 5008));
        await UnitOfWork.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var ids = await FeedImportDecisionRepository.GetPendingIdsAsync(userId, TestContext.Current.CancellationToken);

        // Assert
        ids.Should().Equal(first.Id, second.Id);
    }

    [Fact]
    public async Task GetByIdAsync_Should_RoundTripCandidatesAndArbitration()
    {
        // Arrange
        var decision = CreateDecision(Guid.CreateVersion7(), 6006);
        var candidate = new DownloadCandidate("Blacksad T03", "Blacksad T03.cbz", 1234,
            [new DownloadMirror("https://1fichier.com/?a", "1fichier.com")]);
        decision.RequestArbitration(FeedImportArbitrationKind.AmbiguousLinks, [candidate], new ParsedComicTitle("Blacksad", null, 3), null, "Ambigu", FeedImportDecidedBy.Auto);
        FeedImportDecisionRepository.Add(decision);
        await UnitOfWork.SaveChangesAsync(TestContext.Current.CancellationToken);
        Context.ChangeTracker.Clear();

        // Act
        var found = await FeedImportDecisionRepository.GetByIdAsync(decision.Id, TestContext.Current.CancellationToken);

        // Assert
        found.Should().NotBeNull();
        found.ArbitrationKind.Should().Be(FeedImportArbitrationKind.AmbiguousLinks);
        found.ParsedVolume.Should().Be(3);
        found.GetCandidates().Should().ContainSingle().Which.Should().BeEquivalentTo(candidate);
    }

    [Fact]
    public async Task SaveChangesAsync_Should_InsertNewEvent_WhenLoadedDecisionTransitions()
    {
        // Arrange
        var decision = CreateDecision(Guid.CreateVersion7(), 7007);
        FeedImportDecisionRepository.Add(decision);
        await UnitOfWork.SaveChangesAsync(TestContext.Current.CancellationToken);
        Context.ChangeTracker.Clear();
        var loaded = await FeedImportDecisionRepository.GetByIdAsync(decision.Id, TestContext.Current.CancellationToken);
        loaded!.Fail("Step", "Erreur");

        // Act
        var result = await UnitOfWork.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var eventCount = await Context.FeedImportDecisionEvents
            .CountAsync(e => e.FeedImportDecisionId == decision.Id, TestContext.Current.CancellationToken);
        eventCount.Should().Be(2);
    }

    [Fact]
    public async Task GetIdsByStatusAsync_Should_ReturnDecisionsOfUserWithStatus_IncludingSplitBooks()
    {
        // Arrange
        var userId = Guid.CreateVersion7();
        var candidate = new DownloadCandidate("Blacksad T03", "Blacksad T03.cbz", null, [new DownloadMirror("https://1fichier.com/?a", "1fichier.com")]);
        var article = CreateDecision(userId, 7007);
        var sibling = article.CreateSibling(1).Value!;
        article.RecordLinks([candidate], ParsedComicTitle.Empty, "1 lien", FeedImportDecidedBy.Auto);
        sibling.RecordLinks([candidate], ParsedComicTitle.Empty, "1 lien", FeedImportDecidedBy.Auto);
        var pending = CreateDecision(userId, 7008);
        FeedImportDecisionRepository.Add(article);
        FeedImportDecisionRepository.Add(sibling);
        FeedImportDecisionRepository.Add(pending);
        await UnitOfWork.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var ids = await FeedImportDecisionRepository.GetIdsByStatusAsync(userId, FeedImportDecisionStatus.LinksExtracted, TestContext.Current.CancellationToken);

        // Assert
        ids.Should().Equal(article.Id, sibling.Id);
    }

    [Fact]
    public async Task GetByStatusAsync_Should_ReturnTrackedDecisionsOfUserWithStatus()
    {
        // Arrange
        var userId = Guid.CreateVersion7();
        var failed = CreateDecision(userId, 8008);
        failed.Fail("Step", "Erreur");
        var otherUserFailed = CreateDecision(Guid.CreateVersion7(), 8009);
        otherUserFailed.Fail("Step", "Erreur");
        FeedImportDecisionRepository.Add(failed);
        FeedImportDecisionRepository.Add(otherUserFailed);
        FeedImportDecisionRepository.Add(CreateDecision(userId, 8010));
        await UnitOfWork.SaveChangesAsync(TestContext.Current.CancellationToken);
        Context.ChangeTracker.Clear();

        // Act
        var decisions = await FeedImportDecisionRepository.GetByStatusAsync(userId, FeedImportDecisionStatus.Failed, TestContext.Current.CancellationToken);

        // Assert
        decisions.Should().ContainSingle().Which.Id.Should().Be(failed.Id);
        Context.Entry(decisions[0]).State.Should().Be(EntityState.Unchanged);
    }

    [Fact]
    public async Task Remove_Should_DeleteDecisionAndItsEvents_AndKeepSiblings()
    {
        // Arrange
        var userId = Guid.CreateVersion7();
        var decision = CreateDecision(userId, 5005);
        var sibling = decision.CreateSibling(1).Value!;
        FeedImportDecisionRepository.Add(decision);
        FeedImportDecisionRepository.Add(sibling);
        await UnitOfWork.SaveChangesAsync(TestContext.Current.CancellationToken);
        Context.ChangeTracker.Clear();
        var loaded = await FeedImportDecisionRepository.GetByIdAsync(decision.Id, TestContext.Current.CancellationToken);

        // Act
        FeedImportDecisionRepository.Remove(loaded!);
        var result = await UnitOfWork.SaveChangesAsync(TestContext.Current.CancellationToken);
        Context.ChangeTracker.Clear();

        // Assert
        result.IsSuccess.Should().BeTrue();
        (await FeedImportDecisionRepository.GetByIdAsync(decision.Id, TestContext.Current.CancellationToken)).Should().BeNull();
        (await FeedImportDecisionRepository.GetByIdAsync(sibling.Id, TestContext.Current.CancellationToken)).Should().NotBeNull();
        var events = await Context.FeedImportDecisionEvents
            .Where(e => e.FeedImportDecisionId == decision.Id)
            .ToListAsync(TestContext.Current.CancellationToken);
        events.Should().BeEmpty();
    }
}
