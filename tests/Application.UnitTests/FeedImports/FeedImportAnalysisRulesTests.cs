using Application.Books;
using Application.FeedImports.Analysis;
using Domain.FeedImports;

namespace Application.UnitTests.FeedImports;

public class FeedImportAnalysisRulesTests
{
    private const string ArticleTitle = "Blacksad - Tome 3 - Âme rouge";
    private static readonly DownloadMirror s_mirror = new("https://1fichier.com/?abc", "1fichier.com");
    private static readonly BookIdentityDto s_spirou12 = new(Guid.CreateVersion7(), "Spirou et Fantasio", "Le Nid des Marsupilamis", 12, null, "BD numériques");
    private static readonly BookIdentityDto s_maus = new(Guid.CreateVersion7(), "", "Maus", 1, "9782080665126", "BD");

    private static DownloadCandidate Candidate(string? fileName, string label = "Lien", int mirrors = 1) =>
        new(label, fileName, null, [.. Enumerable.Repeat(s_mirror, mirrors)]);

    private static FeedImportDecision CreatePending() =>
        FeedImportDecision.Create(Guid.CreateVersion7(), 42, ArticleTitle, "https://planete-bd.org/blacksad-3", null).Value!;

    // ── ParseCandidate ───────────────────────────────────────────────────────

    [Fact]
    public void ParseCandidate_Should_PreferTheArticleTitle_WhenTheFileIsTheOnlyBookOfTheArticle()
    {
        var parsed = FeedImportAnalysisRules.ParseCandidate(Candidate("Largo_Winch_T04_Enfer.cbz"), ArticleTitle, isOnlyBookOfArticle: true);

        parsed.Should().Be(new ParsedComicTitle("Blacksad", "Âme rouge", 3));
    }

    [Fact]
    public void ParseCandidate_Should_ReadTheFileName_WhenTheArticleTitleSaysNothing()
    {
        var parsed = FeedImportAnalysisRules.ParseCandidate(Candidate("Largo_Winch_T04_Enfer.cbz", label: "Autre chose"), " ", isOnlyBookOfArticle: true);

        parsed.Should().Be(new ParsedComicTitle("Largo Winch", "Enfer", 4));
    }

    [Fact]
    public void ParseCandidate_Should_PreferTheFileName_WhenTheArticleHasSeveralBooks()
    {
        var parsed = FeedImportAnalysisRules.ParseCandidate(Candidate("Largo_Winch_T04_Enfer.cbz"), ArticleTitle, isOnlyBookOfArticle: false);

        parsed.Should().Be(new ParsedComicTitle("Largo Winch", "Enfer", 4));
    }

    [Fact]
    public void ParseCandidate_Should_TakeTheSerieOfTheArticle_WhenTheFileNameHasNone()
    {
        var parsed = FeedImportAnalysisRules.ParseCandidate(Candidate("T04.cbz"), "Blacksad - Intégrale", isOnlyBookOfArticle: false);

        parsed.Should().Be(new ParsedComicTitle("Blacksad", null, 4));
    }

    [Fact]
    public void ParseCandidate_Should_ReadTheLabel_WhenTheFileNameIsUnknown()
    {
        var parsed = FeedImportAnalysisRules.ParseCandidate(Candidate(null, label: "Largo Winch T05"), ArticleTitle, isOnlyBookOfArticle: false, pageIsbn: "9782205049999");

        parsed.Should().Be(new ParsedComicTitle("Largo Winch", null, 5, "9782205049999"));
    }

    [Fact]
    public void ParseCandidate_Should_PreferTheIsbnOfTheFileName_ToTheIsbnOfThePage()
    {
        var parsed = FeedImportAnalysisRules.ParseCandidate(Candidate("Blacksad_T03 (9782205050196).cbz"), ArticleTitle, isOnlyBookOfArticle: true,
            pageIsbn: "9782205049999");

        parsed.Isbn.Should().Be("9782205050196");
    }

    [Fact]
    public void ParseCandidate_Should_TakeTheIsbnOfThePage_WhenTheFileNameHasNone()
    {
        var parsed = FeedImportAnalysisRules.ParseCandidate(Candidate("Blacksad_T03.cbz"), ArticleTitle, isOnlyBookOfArticle: true,
            pageIsbn: "9782205049999");

        parsed.Isbn.Should().Be("9782205049999");
    }

    [Fact]
    public void ParseCandidate_Should_Throw_WhenTheCandidateIsNull()
    {
        var act = () => FeedImportAnalysisRules.ParseCandidate(null!, ArticleTitle, isOnlyBookOfArticle: true);

        act.Should().Throw<ArgumentNullException>();
    }

    // ── ApplyDuplicateCheck ──────────────────────────────────────────────────

    [Fact]
    public void ApplyDuplicateCheck_Should_NameTheBookByItsTitle_WhenItHasNoSerie()
    {
        var decision = CreatePending();

        FeedImportAnalysisRules.ApplyDuplicateCheck(decision, Candidate("Maus.cbz"), new ParsedComicTitle("Maus", null, 1, "9782080665126"),
            [s_maus], FeedImportDecidedBy.Auto);

        decision.Status.Should().Be(FeedImportDecisionStatus.SkippedDuplicate);
        decision.Reason.Should().Be("Duplicate: \"Maus T1\" in \"BD\".");
    }

    [Fact]
    public void ApplyDuplicateCheck_Should_AskForAnArbitration_WhenTheDuplicateIsProbable()
    {
        var decision = CreatePending();

        FeedImportAnalysisRules.ApplyDuplicateCheck(decision, Candidate("Spirou.cbz"), new ParsedComicTitle("Spirou et Fantazio", null, 12),
            [s_spirou12], FeedImportDecidedBy.Auto);

        decision.Status.Should().Be(FeedImportDecisionStatus.AwaitingArbitration);
        decision.ArbitrationKind.Should().Be(FeedImportArbitrationKind.ProbableDuplicate);
        decision.MatchedBookId.Should().Be(s_spirou12.Id);
        decision.Reason.Should().Be("Probable duplicate: \"Spirou et Fantasio T12\" in \"BD numériques\".");
    }

    [Theory]
    [InlineData(1, "1 link found, no duplicate.")]
    [InlineData(2, "2 mirrors found, no duplicate.")]
    public void ApplyDuplicateCheck_Should_CountTheMirrors_WhenThereIsNoDuplicate(int mirrors, string reason)
    {
        var decision = CreatePending();

        FeedImportAnalysisRules.ApplyDuplicateCheck(decision, Candidate("Blacksad_T03.cbz", mirrors: mirrors), new ParsedComicTitle("Blacksad", null, 3),
            [s_spirou12], FeedImportDecidedBy.Auto);

        decision.Status.Should().Be(FeedImportDecisionStatus.LinksExtracted);
        decision.Reason.Should().Be(reason);
    }

    [Fact]
    public void ApplyDuplicateCheck_Should_Throw_WhenTheDecisionOrTheCandidateIsNull()
    {
        var withoutDecision = () => FeedImportAnalysisRules.ApplyDuplicateCheck(null!, Candidate("a.cbz"), ParsedComicTitle.Empty, [], FeedImportDecidedBy.Auto);
        var withoutCandidate = () => FeedImportAnalysisRules.ApplyDuplicateCheck(CreatePending(), null!, ParsedComicTitle.Empty, [], FeedImportDecidedBy.Auto);

        withoutDecision.Should().Throw<ArgumentNullException>();
        withoutCandidate.Should().Throw<ArgumentNullException>();
    }

    // ── Reapply ──────────────────────────────────────────────────────────────

    [Fact]
    public void Reapply_Should_AskToChooseTheLink_WhenSeveralFilesAreStored()
    {
        var decision = CreatePending();
        decision.FailWithLinks([Candidate("Blacksad_T03.cbz"), Candidate("Blacksad_T03_HD.cbz")], ParsedComicTitle.Empty, "Download", "Quota");
        decision.Retry(DateTime.UtcNow);

        FeedImportAnalysisRules.Reapply(decision, [], FeedImportDecidedBy.User).IsSuccess.Should().BeTrue();

        decision.Status.Should().Be(FeedImportDecisionStatus.AwaitingArbitration);
        decision.ArbitrationKind.Should().Be(FeedImportArbitrationKind.AmbiguousLinks);
        decision.Reason.Should().Be("Several files: choose the right link.");
    }

    [Fact]
    public void Reapply_Should_ParseTheFileAgain_AsTheOnlyBookOfTheArticle_WhenNothingIsStored()
    {
        var decision = CreatePending();
        decision.RecordLinks([Candidate("Largo_Winch_T04_Enfer.cbz")], ParsedComicTitle.Empty, "r", FeedImportDecidedBy.Auto);
        decision.Fail("Download", "Quota");
        decision.Retry(DateTime.UtcNow);

        FeedImportAnalysisRules.Reapply(decision, [], FeedImportDecidedBy.User);

        decision.ParsedSerie.Should().Be("Blacksad");
        decision.ParsedTitle.Should().Be("Âme rouge");
        decision.ParsedVolume.Should().Be(3);
    }

    [Fact]
    public void Reapply_Should_KeepTheCorrection_AndReadTheIsbnOfTheFile()
    {
        var decision = CreatePending();
        decision.RecordLinks([Candidate("Maus (9782080665126).cbz")], ParsedComicTitle.Empty, "r", FeedImportDecidedBy.Auto);
        decision.Correct(new ParsedComicTitle("Corrigé", null, null));

        FeedImportAnalysisRules.Reapply(decision, [s_maus], FeedImportDecidedBy.User);

        decision.ParsedSerie.Should().Be("Corrigé");
        decision.ParsedVolume.Should().BeNull();
        decision.Status.Should().Be(FeedImportDecisionStatus.SkippedDuplicate);
        decision.MatchedBookId.Should().Be(s_maus.Id);
    }

    [Fact]
    public void Reapply_Should_Throw_WhenTheDecisionIsNull()
    {
        var act = () => FeedImportAnalysisRules.Reapply(null!, [], FeedImportDecidedBy.Auto);

        act.Should().Throw<ArgumentNullException>();
    }
}
