using Domain.FeedImports;

namespace Domain.UnitTests.FeedImports;

public class FeedImportDecisionTests
{
    private static readonly Guid s_userId = Guid.CreateVersion7();
    private const long DefaultEntryId = 42;
    private const string DefaultTitle = "Blacksad - Tome 3 - Âme rouge";
    private const string DefaultUrl = "https://planete-bd.org/blacksad-tome-3";
    private static readonly DateTime s_publishedAt = new(2026, 9, 30, 18, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Create_Should_ReturnPendingDecisionWithCreationEvent_WhenParametersAreValid()
    {
        // Act
        var result = FeedImportDecision.Create(s_userId, DefaultEntryId, DefaultTitle, DefaultUrl, s_publishedAt);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var decision = result.Value!;
        decision.Id.Should().NotBe(Guid.Empty);
        decision.UserId.Should().Be(s_userId);
        decision.MinifluxEntryId.Should().Be(DefaultEntryId);
        decision.EntryTitle.Should().Be(DefaultTitle);
        decision.EntryUrl.Should().Be(DefaultUrl);
        decision.PublishedAt.Should().Be(s_publishedAt);
        decision.Status.Should().Be(FeedImportDecisionStatus.Pending);
        decision.DecidedBy.Should().Be(FeedImportDecidedBy.Auto);
        decision.Reason.Should().Be(FeedImportDecision.CreatedReason);
        decision.UpdatedAt.Should().Be(decision.CreatedAt);

        var creationEvent = decision.Events.Should().ContainSingle().Subject;
        creationEvent.FeedImportDecisionId.Should().Be(decision.Id);
        creationEvent.PreviousStatus.Should().BeNull();
        creationEvent.Status.Should().Be(FeedImportDecisionStatus.Pending);
        creationEvent.DecidedBy.Should().Be(FeedImportDecidedBy.Auto);
        creationEvent.OccurredAt.Should().Be(decision.CreatedAt);
    }

    [Fact]
    public void Create_Should_ReturnBadRequest_WhenUserIdIsEmpty()
    {
        var result = FeedImportDecision.Create(Guid.Empty, DefaultEntryId, DefaultTitle, DefaultUrl, s_publishedAt);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(FeedImportError.BadRequest);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_Should_ReturnBadRequest_WhenEntryIdIsNotPositive(long entryId)
    {
        var result = FeedImportDecision.Create(s_userId, entryId, DefaultTitle, DefaultUrl, s_publishedAt);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(FeedImportError.BadRequest);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("/relative/path")]
    [InlineData("ftp://planete-bd.org/file")]
    [InlineData("javascript:alert(1)")]
    public void Create_Should_ReturnBadRequest_WhenUrlIsNotAnAbsoluteHttpUrl(string entryLink)
    {
        var result = FeedImportDecision.Create(s_userId, DefaultEntryId, DefaultTitle, entryLink, s_publishedAt);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(FeedImportError.BadRequest);
    }

    [Fact]
    public void Create_Should_ReturnBadRequest_WhenUrlIsTooLong()
    {
        var url = "https://planete-bd.org/" + new string('a', FeedImportConstants.MaxEntryUrlLength);

        var result = FeedImportDecision.Create(s_userId, DefaultEntryId, DefaultTitle, url, s_publishedAt);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(FeedImportError.BadRequest);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void Create_Should_UseUrlAsTitle_WhenTitleIsBlank(string title)
    {
        var result = FeedImportDecision.Create(s_userId, DefaultEntryId, title, DefaultUrl, null);

        result.IsSuccess.Should().BeTrue();
        result.Value!.EntryTitle.Should().Be(DefaultUrl);
        result.Value.PublishedAt.Should().BeNull();
    }

    [Fact]
    public void Create_Should_TruncateTitle_WhenTitleIsTooLong()
    {
        var title = new string('x', FeedImportConstants.MaxEntryTitleLength + 10);

        var result = FeedImportDecision.Create(s_userId, DefaultEntryId, title, DefaultUrl, s_publishedAt);

        result.IsSuccess.Should().BeTrue();
        result.Value!.EntryTitle.Should().HaveLength(FeedImportConstants.MaxEntryTitleLength);
    }
}
