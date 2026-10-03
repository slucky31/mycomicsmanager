using Domain.FeedImports;

namespace Domain.UnitTests.FeedImports;

public class FeedImportDecisionTransitionTests
{
    private static readonly Guid s_userId = Guid.CreateVersion7();
    private static readonly Guid s_bookId = Guid.CreateVersion7();
    private static readonly ParsedComicTitle s_parsed = new("Blacksad", "Âme rouge", 3);

    private static readonly DownloadCandidate s_candidate = new(
        "Blacksad T03", "Blacksad T03.cbz", 50_000_000,
        [new DownloadMirror("https://1fichier.com/?abc", "1fichier.com"), new DownloadMirror("https://rapidgator.net/file/x/Blacksad T03.cbz.html", "rapidgator.net")]);

    private static FeedImportDecision CreatePending() =>
        FeedImportDecision.Create(s_userId, 42, "Blacksad - Tome 3", "https://planete-bd.org/blacksad-3", null).Value!;

    private static FeedImportDecision CreateAwaiting(FeedImportArbitrationKind kind, Guid? matchedBookId = null)
    {
        var decision = CreatePending();
        decision.RequestArbitration(kind, [s_candidate], s_parsed, matchedBookId, "À arbitrer", FeedImportDecidedBy.Auto);
        return decision;
    }

    [Fact]
    public void RecordLinks_Should_StoreCandidatesAndParsedTitle_WhenPending()
    {
        var decision = CreatePending();

        var result = decision.RecordLinks([s_candidate], s_parsed, "2 miroirs trouvés", FeedImportDecidedBy.Auto);

        result.IsSuccess.Should().BeTrue();
        decision.Status.Should().Be(FeedImportDecisionStatus.LinksExtracted);
        decision.Reason.Should().Be("2 miroirs trouvés");
        decision.ParsedSerie.Should().Be("Blacksad");
        decision.ParsedTitle.Should().Be("Âme rouge");
        decision.ParsedVolume.Should().Be(3);
        decision.GetCandidates().Should().ContainSingle().Which.Should().BeEquivalentTo(s_candidate);
        var lastEvent = decision.Events[^1];
        lastEvent.PreviousStatus.Should().Be(FeedImportDecisionStatus.Pending);
        lastEvent.Status.Should().Be(FeedImportDecisionStatus.LinksExtracted);
    }

    [Fact]
    public void RecordLinks_Should_ReturnBadRequest_WhenNoCandidateOrCandidateWithoutMirror()
    {
        var decision = CreatePending();

        decision.RecordLinks([], s_parsed, "r", FeedImportDecidedBy.Auto).Error.Should().Be(FeedImportError.BadRequest);
        decision.RecordLinks([s_candidate with { Mirrors = [] }], s_parsed, "r", FeedImportDecidedBy.Auto).Error.Should().Be(FeedImportError.BadRequest);
        decision.Status.Should().Be(FeedImportDecisionStatus.Pending);
    }

    [Fact]
    public void RecordLinks_Should_ReturnInvalidTransition_WhenAlreadyExtracted()
    {
        var decision = CreatePending();
        decision.RecordLinks([s_candidate], s_parsed, "r", FeedImportDecidedBy.Auto);

        var result = decision.RecordLinks([s_candidate], s_parsed, "r", FeedImportDecidedBy.Auto);

        result.Error.Should().Be(FeedImportError.InvalidStatusTransition);
    }

    [Fact]
    public void RequestArbitration_Should_SetKindAndMatchedBook_WhenPending()
    {
        var decision = CreateAwaiting(FeedImportArbitrationKind.ProbableDuplicate, s_bookId);

        decision.Status.Should().Be(FeedImportDecisionStatus.AwaitingArbitration);
        decision.ArbitrationKind.Should().Be(FeedImportArbitrationKind.ProbableDuplicate);
        decision.MatchedBookId.Should().Be(s_bookId);
    }

    [Fact]
    public void RequestArbitration_Should_ReturnBadRequest_WhenKindIsNone()
    {
        var result = CreatePending().RequestArbitration(FeedImportArbitrationKind.None, [s_candidate], s_parsed, null, "r", FeedImportDecidedBy.Auto);

        result.Error.Should().Be(FeedImportError.BadRequest);
    }

    [Fact]
    public void MarkDuplicate_Should_SkipDecision_WhenUserResolvesAmbiguousLinks()
    {
        var decision = CreateAwaiting(FeedImportArbitrationKind.AmbiguousLinks);

        var result = decision.MarkDuplicate([s_candidate], s_parsed, s_bookId, "Doublon", FeedImportDecidedBy.User);

        result.IsSuccess.Should().BeTrue();
        decision.Status.Should().Be(FeedImportDecisionStatus.SkippedDuplicate);
        decision.MatchedBookId.Should().Be(s_bookId);
        decision.ArbitrationKind.Should().Be(FeedImportArbitrationKind.None);
        decision.DecidedBy.Should().Be(FeedImportDecidedBy.User);
    }

    [Fact]
    public void MarkDuplicate_Should_ReturnBadRequest_WhenBookIdIsEmpty()
    {
        CreatePending().MarkDuplicate([s_candidate], s_parsed, Guid.Empty, "r", FeedImportDecidedBy.Auto)
            .Error.Should().Be(FeedImportError.BadRequest);
    }

    [Fact]
    public void ConfirmNotDuplicate_Should_ClearMatchAndExtractLinks_WhenProbableDuplicate()
    {
        var decision = CreateAwaiting(FeedImportArbitrationKind.ProbableDuplicate, s_bookId);

        var result = decision.ConfirmNotDuplicate();

        result.IsSuccess.Should().BeTrue();
        decision.Status.Should().Be(FeedImportDecisionStatus.LinksExtracted);
        decision.MatchedBookId.Should().BeNull();
        decision.DecidedBy.Should().Be(FeedImportDecidedBy.User);
    }

    [Fact]
    public void ConfirmDuplicate_Should_SkipDecision_WhenProbableDuplicate()
    {
        var decision = CreateAwaiting(FeedImportArbitrationKind.ProbableDuplicate, s_bookId);

        var result = decision.ConfirmDuplicate();

        result.IsSuccess.Should().BeTrue();
        decision.Status.Should().Be(FeedImportDecisionStatus.SkippedDuplicate);
        decision.MatchedBookId.Should().Be(s_bookId);
    }

    [Fact]
    public void ConfirmDuplicate_Should_ReturnInvalidTransition_WhenArbitrationIsAboutLinks()
    {
        var decision = CreateAwaiting(FeedImportArbitrationKind.AmbiguousLinks);

        decision.ConfirmDuplicate().Error.Should().Be(FeedImportError.InvalidStatusTransition);
        decision.ConfirmNotDuplicate().Error.Should().Be(FeedImportError.InvalidStatusTransition);
    }

    [Fact]
    public void Fail_Should_RecordStepAndMessage_WhenPending()
    {
        var decision = CreatePending();

        var result = decision.Fail("Source", "Domaine source non autorisé.");

        result.IsSuccess.Should().BeTrue();
        decision.Status.Should().Be(FeedImportDecisionStatus.Failed);
        decision.ErrorStep.Should().Be("Source");
        decision.ErrorMessage.Should().Be("Domaine source non autorisé.");
        decision.Reason.Should().Be("Domaine source non autorisé.");
    }

    [Fact]
    public void Fail_Should_ReturnInvalidTransition_WhenDecisionIsFinal()
    {
        var decision = CreatePending();
        decision.Fail("Source", "x");

        decision.Fail("Source", "y").Error.Should().Be(FeedImportError.InvalidStatusTransition);
    }

    [Fact]
    public void CreateSibling_Should_CopyEntryWithNextItemIndex_WhenPending()
    {
        var decision = CreatePending();

        var result = decision.CreateSibling(2);

        result.IsSuccess.Should().BeTrue();
        var sibling = result.Value!;
        sibling.Id.Should().NotBe(decision.Id);
        sibling.ItemIndex.Should().Be(2);
        sibling.MinifluxEntryId.Should().Be(decision.MinifluxEntryId);
        sibling.UserId.Should().Be(decision.UserId);
        sibling.EntryUrl.Should().Be(decision.EntryUrl);
        sibling.CreatedAt.Should().Be(decision.CreatedAt);
        sibling.Status.Should().Be(FeedImportDecisionStatus.Pending);
        sibling.Events.Should().ContainSingle();
    }

    [Fact]
    public void CreateSibling_Should_Fail_WhenIndexIsInvalidOrDecisionAlreadyAnalyzed()
    {
        var decision = CreatePending();
        decision.CreateSibling(0).Error.Should().Be(FeedImportError.BadRequest);
        decision.CreateSibling(1).Value!.CreateSibling(2).Error.Should().Be(FeedImportError.BadRequest);

        decision.RecordLinks([s_candidate], s_parsed, "r", FeedImportDecidedBy.Auto);
        decision.CreateSibling(1).Error.Should().Be(FeedImportError.InvalidStatusTransition);
    }

    [Fact]
    public void GetCandidates_Should_ReturnEmpty_WhenNoLinksRecorded()
    {
        CreatePending().GetCandidates().Should().BeEmpty();
    }

    private static FeedImportDecision CreateDownloading()
    {
        var decision = CreatePending();
        decision.RecordLinks([s_candidate], s_parsed, "r", FeedImportDecidedBy.Auto);
        decision.StartDownload();
        return decision;
    }

    [Fact]
    public void StartDownload_Should_MoveToDownloading_WhenLinksExtracted()
    {
        var decision = CreatePending();
        decision.RecordLinks([s_candidate], s_parsed, "r", FeedImportDecidedBy.Auto);

        var result = decision.StartDownload();

        result.IsSuccess.Should().BeTrue();
        decision.Status.Should().Be(FeedImportDecisionStatus.Downloading);
        decision.Events[^1].PreviousStatus.Should().Be(FeedImportDecisionStatus.LinksExtracted);
    }

    [Fact]
    public void StartDownload_Should_ReturnInvalidTransition_WhenAwaitingArbitration()
    {
        var decision = CreateAwaiting(FeedImportArbitrationKind.AmbiguousLinks);

        decision.StartDownload().Error.Should().Be(FeedImportError.InvalidStatusTransition);
        decision.Status.Should().Be(FeedImportDecisionStatus.AwaitingArbitration);
    }

    [Fact]
    public void NoteMirrorFailure_Should_AddEventWithoutChangingStatus_WhenDownloading()
    {
        var decision = CreateDownloading();
        var eventCount = decision.Events.Count;

        var result = decision.NoteMirrorFailure("1fichier.com", "Fichier indisponible");

        result.IsSuccess.Should().BeTrue();
        decision.Status.Should().Be(FeedImportDecisionStatus.Downloading);
        decision.Events.Should().HaveCount(eventCount + 1);
        decision.Events[^1].Description.Should().Be("Échec du miroir 1fichier.com : Fichier indisponible");
    }

    [Fact]
    public void NoteMirrorFailure_Should_Fail_WhenNotDownloadingOrArgumentsMissing()
    {
        CreatePending().NoteMirrorFailure("1fichier.com", "err").Error.Should().Be(FeedImportError.InvalidStatusTransition);
        CreateDownloading().NoteMirrorFailure(" ", "err").Error.Should().Be(FeedImportError.BadRequest);
    }

    [Fact]
    public void MarkDownloaded_Should_LinkImportJobAndMirror_WhenDownloading()
    {
        var decision = CreateDownloading();
        var jobId = Guid.CreateVersion7();

        var result = decision.MarkDownloaded("https://1fichier.com/?abc", jobId, "À trier");

        result.IsSuccess.Should().BeTrue();
        decision.Status.Should().Be(FeedImportDecisionStatus.Downloaded);
        decision.ImportJobId.Should().Be(jobId);
        decision.ChosenMirror.Should().Be("https://1fichier.com/?abc");
        decision.Reason.Should().Contain("« À trier »");
    }

    [Fact]
    public void MarkDownloaded_Should_Fail_WhenArgumentsMissingOrNotDownloading()
    {
        CreateDownloading().MarkDownloaded("https://1fichier.com/?abc", Guid.Empty, "À trier").Error.Should().Be(FeedImportError.BadRequest);
        CreatePending().MarkDownloaded("https://1fichier.com/?abc", Guid.CreateVersion7(), "À trier").Error.Should().Be(FeedImportError.InvalidStatusTransition);
    }

    [Fact]
    public void MarkImported_Should_LinkDigitalBook_WhenDownloaded()
    {
        var decision = CreateDownloading();
        decision.MarkDownloaded("https://1fichier.com/?abc", Guid.CreateVersion7(), "À trier");

        var result = decision.MarkImported(s_bookId);

        result.IsSuccess.Should().BeTrue();
        decision.Status.Should().Be(FeedImportDecisionStatus.Imported);
        decision.DigitalBookId.Should().Be(s_bookId);
    }

    [Fact]
    public void MarkImported_Should_Fail_WhenBookIdIsEmptyOrNotDownloaded()
    {
        CreateDownloading().MarkImported(Guid.Empty).Error.Should().Be(FeedImportError.BadRequest);
        CreateDownloading().MarkImported(s_bookId).Error.Should().Be(FeedImportError.InvalidStatusTransition);
    }
}
