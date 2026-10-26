using AwesomeAssertions;
using Domain.ImportJobs;
using Web.Models;
using Xunit;

namespace Web.Tests.Models;

public sealed class ImportJobViewModelTests
{
    private static ImportJob CreateJob(string fileName = "comic.cbz", string filePath = "/srv/comic.cbz", long size = 1024)
        => ImportJob.Create(fileName, filePath, size, Guid.CreateVersion7()).Value!;

    // ── From ──────────────────────────────────────────────────────────────────

    [Fact]
    public void From_Should_MapAllFields()
    {
        var job = CreateJob("comic.cbz", "/srv/comic.cbz", 2048);

        var viewModel = ImportJobViewModel.From(job);

        viewModel.Id.Should().Be(job.Id);
        viewModel.OriginalFileName.Should().Be("comic.cbz");
        viewModel.OriginalFileSize.Should().Be(2048);
        viewModel.Status.Should().Be("Pending");
        viewModel.CreatedAt.Should().Be(job.CreatedAt);
        viewModel.CompletedAt.Should().BeNull();
        viewModel.ErrorMessage.Should().BeNull();
        viewModel.ErrorStep.Should().BeNull();
    }

    [Fact]
    public void CreatedAtDisplay_Should_ShowTheLocalDateAndTime_LikeTheOtherDatesOfTheApp()
    {
        var viewModel = ImportJobViewModel.From(CreateJob()) with { CreatedAt = new DateTime(2026, 10, 3, 17, 23, 0, DateTimeKind.Utc) };

        viewModel.CreatedAtDisplay.Should().MatchRegex(@"^0[34]/10/2026 \d{2}:23$");
    }

    [Fact]
    public void From_Should_MapErrorDetails_WhenJobFailed()
    {
        var job = CreateJob();
        job.Fail("Extracting", "boom");

        var viewModel = ImportJobViewModel.From(job);

        viewModel.Status.Should().Be("Failed");
        viewModel.ErrorMessage.Should().Be("boom");
        viewModel.ErrorStep.Should().Be("Extracting");
    }

    // ── IsTerminal ────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(ImportJobStatus.Pending, false)]
    [InlineData(ImportJobStatus.Extracting, false)]
    [InlineData(ImportJobStatus.Converting, false)]
    [InlineData(ImportJobStatus.Completed, true)]
    [InlineData(ImportJobStatus.Failed, true)]
    public void IsTerminal_Should_ReflectStatus(ImportJobStatus status, bool expected)
    {
        var viewModel = ViewModelWithStatus(status);

        viewModel.IsTerminal.Should().Be(expected);
    }

    // ── DurationDisplay ───────────────────────────────────────────────────────

    [Fact]
    public void DurationDisplay_Should_ReturnNull_WhenNotCompleted()
    {
        var viewModel = ImportJobViewModel.From(CreateJob());

        viewModel.DurationDisplay.Should().BeNull();
    }

    [Fact]
    public void DurationDisplay_Should_FormatSeconds_WhenUnderOneMinute()
    {
        var viewModel = ViewModelWithDuration(TimeSpan.FromSeconds(42));

        viewModel.DurationDisplay.Should().Be("42s");
    }

    [Fact]
    public void DurationDisplay_Should_FormatMinutesAndSeconds_WhenUnderOneHour()
    {
        var viewModel = ViewModelWithDuration(TimeSpan.FromSeconds(125));

        viewModel.DurationDisplay.Should().Be("2m 5s");
    }

    [Fact]
    public void DurationDisplay_Should_FormatHoursAndMinutes_WhenOverOneHour()
    {
        var viewModel = ViewModelWithDuration(TimeSpan.FromMinutes(75));

        viewModel.DurationDisplay.Should().Be("1h 15m");
    }

    // ── ConversionDetail ──────────────────────────────────────────────────────

    [Fact]
    public void ConversionDetail_Should_ReturnNull_WhenNotConverting()
    {
        var viewModel = ViewModelWithStatus(ImportJobStatus.Pending);

        viewModel.ConversionDetail.Should().BeNull();
    }

    [Fact]
    public void ConversionDetail_Should_ReturnProgress_WhenConvertingWithTotal()
    {
        var job = CreateJob();
        job.Advance(ImportJobStatus.Extracting);
        job.Advance(ImportJobStatus.Converting);
        job.UpdateConversionProgress(3, 10);

        var viewModel = ImportJobViewModel.From(job);

        viewModel.ConversionDetail.Should().Be("3/10 images converted");
    }

    [Fact]
    public void ConversionDetail_Should_ReturnNull_WhenConvertingWithZeroTotal()
    {
        var job = CreateJob();
        job.Advance(ImportJobStatus.Extracting);
        job.Advance(ImportJobStatus.Converting);

        var viewModel = ImportJobViewModel.From(job);

        viewModel.ConversionDetail.Should().BeNull();
    }

    // ── FileSizeDisplay ───────────────────────────────────────────────────────

    [Theory]
    [InlineData(500L, "500 B")]
    [InlineData(2_048L, "2 KB")]
    [InlineData(5_242_880L, "5.0 MB")]
    [InlineData(2_147_483_648L, "2.0 GB")]
    public void FileSizeDisplay_Should_FormatAccordingToMagnitude(long size, string expected)
    {
        // FileSizeDisplay interpolates with the thread's current culture; pin it to
        // InvariantCulture so the expected "." decimal separator holds on any machine/CI.
        var originalCulture = System.Globalization.CultureInfo.CurrentCulture;
        System.Globalization.CultureInfo.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
        try
        {
            var viewModel = ImportJobViewModel.From(CreateJob(size: size));

            viewModel.FileSizeDisplay.Should().Be(expected);
        }
        finally
        {
            System.Globalization.CultureInfo.CurrentCulture = originalCulture;
        }
    }

    // ── StatusCssClass ────────────────────────────────────────────────────────

    [Theory]
    [InlineData(ImportJobStatus.Pending, "status-pending")]
    [InlineData(ImportJobStatus.Extracting, "status-processing")]
    [InlineData(ImportJobStatus.Completed, "status-completed")]
    [InlineData(ImportJobStatus.Failed, "status-failed")]
    public void StatusCssClass_Should_MapStatus(ImportJobStatus status, string expected)
    {
        var viewModel = ViewModelWithStatus(status);

        viewModel.StatusCssClass.Should().Be(expected);
    }

    // ── GetStatusDisplay / GetProgressPercent / StepDisplay (via From) ───────

    [Theory]
    [InlineData(ImportJobStatus.Pending, "Waiting", 0, "Step 1 of 6 · 0%")]
    [InlineData(ImportJobStatus.Extracting, "Extracting pages", 15, "Step 2 of 6 · 15%")]
    [InlineData(ImportJobStatus.Converting, "Converting pages", 35, "Step 3 of 6 · 35%")]
    [InlineData(ImportJobStatus.SearchingMetadata, "Searching details", 55, "Step 4 of 6 · 55%")]
    [InlineData(ImportJobStatus.UploadingCover, "Uploading cover", 70, "Step 5 of 6 · 70%")]
    [InlineData(ImportJobStatus.BuildingArchive, "Building the book", 85, "Step 6 of 6 · 85%")]
    [InlineData(ImportJobStatus.Completed, "Imported", 100, null)]
    [InlineData(ImportJobStatus.Failed, "Failed", 0, null)]
    public void From_Should_MapStatusDisplayProgressAndStep(
        ImportJobStatus status, string expectedDisplay, int expectedProgress, string? expectedStep)
    {
        var viewModel = ViewModelWithStatus(status);

        viewModel.StatusDisplay.Should().Be(expectedDisplay);
        viewModel.ProgressPercent.Should().Be(expectedProgress);
        viewModel.StepDisplay.Should().Be(expectedStep);
    }

    // ── BookHref ──────────────────────────────────────────────────────────────

    [Fact]
    public void BookHref_Should_LinkTheBook_WhenImportCompleted()
    {
        var job = CreateJob();
        var bookId = Guid.CreateVersion7();
        foreach (var step in new[] { ImportJobStatus.Extracting, ImportJobStatus.Converting, ImportJobStatus.SearchingMetadata, ImportJobStatus.UploadingCover, ImportJobStatus.BuildingArchive })
        {
            job.Advance(step);
        }

        job.Complete(bookId).IsSuccess.Should().BeTrue();

        var viewModel = ImportJobViewModel.From(job);

        viewModel.DigitalBookId.Should().Be(bookId);
        viewModel.BookHref.Should().Be($"/books/{bookId}");
    }

    [Fact]
    public void BookHref_Should_BeNull_WhenImportIsNotCompleted()
    {
        var viewModel = ViewModelWithStatus(ImportJobStatus.Converting) with { DigitalBookId = Guid.CreateVersion7() };

        viewModel.BookHref.Should().BeNull();
    }

    // ── FailureDisplay / FailedStepDisplay ────────────────────────────────────

    [Theory]
    [InlineData("Extracting", "The PDF has no readable page.", "The PDF has no readable page.", "Failed while: Extracting pages")]
    [InlineData("Init", "No space left", "No space left", "Failed while: Init")]
    [InlineData("SearchingMetadata", " ", "The import failed.", "Failed while: Searching details")]
    public void FailureDisplay_Should_ExplainTheFailure_WhenImportFailed(string step, string message, string expectedFailure, string expectedStep)
    {
        var job = CreateJob();
        job.Fail(step, message);

        var viewModel = ImportJobViewModel.From(job);

        viewModel.FailureDisplay.Should().Be(expectedFailure);
        viewModel.FailedStepDisplay.Should().Be(expectedStep);
    }

    [Fact]
    public void FailureDisplay_Should_BeNull_WhenImportDidNotFail()
    {
        var viewModel = ViewModelWithStatus(ImportJobStatus.Completed) with { ErrorMessage = "old", ErrorStep = "Extracting" };

        viewModel.FailureDisplay.Should().BeNull();
        viewModel.FailedStepDisplay.Should().BeNull();
    }

    [Fact]
    public void FailedStepDisplay_Should_BeNull_WhenFailureHasNoStep()
    {
        var viewModel = ViewModelWithStatus(ImportJobStatus.Failed) with { ErrorStep = null };

        viewModel.FailedStepDisplay.Should().BeNull();
    }

    [Fact]
    public void From_Should_ComputeConvertingProgress_ProportionalToImageCount()
    {
        var job = CreateJob();
        job.Advance(ImportJobStatus.Extracting);
        job.Advance(ImportJobStatus.Converting);
        job.UpdateConversionProgress(5, 10);

        var viewModel = ImportJobViewModel.From(job);

        viewModel.ProgressPercent.Should().Be(45); // 35 + 5*20/10
    }

    [Fact]
    public void From_Should_ComputeConvertingProgress_AsBaseline_WhenTotalIsZero()
    {
        var job = CreateJob();
        job.Advance(ImportJobStatus.Extracting);
        job.Advance(ImportJobStatus.Converting);

        var viewModel = ImportJobViewModel.From(job);

        viewModel.ProgressPercent.Should().Be(35);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static ImportJobViewModel ViewModelWithStatus(ImportJobStatus status)
    {
        var job = CreateJob();
        switch (status)
        {
            case ImportJobStatus.Pending:
                break;
            case ImportJobStatus.Failed:
                job.Fail("Init", "error");
                break;
            default:
                foreach (var step in new[]
                {
                    ImportJobStatus.Extracting, ImportJobStatus.Converting, ImportJobStatus.SearchingMetadata,
                    ImportJobStatus.UploadingCover, ImportJobStatus.BuildingArchive, ImportJobStatus.Completed
                })
                {
                    job.Advance(step);
                    if (step == status)
                    {
                        break;
                    }
                }
                break;
        }
        return ImportJobViewModel.From(job);
    }

    private static ImportJobViewModel ViewModelWithDuration(TimeSpan duration)
    {
        var job = CreateJob();
        job.Advance(ImportJobStatus.Extracting);
        job.Advance(ImportJobStatus.Converting);
        job.Advance(ImportJobStatus.SearchingMetadata);
        job.Advance(ImportJobStatus.UploadingCover);
        job.Advance(ImportJobStatus.BuildingArchive);
        job.Complete(Guid.CreateVersion7());

        var viewModel = ImportJobViewModel.From(job);
        return viewModel with { CreatedAt = viewModel.CompletedAt!.Value - duration };
    }
}
