using AwesomeAssertions;
using Domain.ImportJobs;
using MudBlazor;
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

        viewModel.ConversionDetail.Should().Be("3/10 images converties");
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
    [InlineData(500L, "500 o")]
    [InlineData(2_048L, "2 Ko")]
    [InlineData(5_242_880L, "5.0 Mo")]
    [InlineData(2_147_483_648L, "2.0 Go")]
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

    // ── GetStatusDisplay / GetStatusColor / GetProgressPercent (via From) ────

    [Theory]
    [InlineData(ImportJobStatus.Pending, "En attente", Color.Default, 0)]
    [InlineData(ImportJobStatus.Extracting, "Extraction...", Color.Info, 15)]
    [InlineData(ImportJobStatus.SearchingMetadata, "Recherche métadonnées...", Color.Info, 55)]
    [InlineData(ImportJobStatus.UploadingCover, "Upload couverture...", Color.Info, 70)]
    [InlineData(ImportJobStatus.BuildingArchive, "Construction archive...", Color.Info, 85)]
    [InlineData(ImportJobStatus.Completed, "Terminé", Color.Success, 100)]
    [InlineData(ImportJobStatus.Failed, "Échoué", Color.Error, 0)]
    public void From_Should_MapStatusDisplayColorAndProgress(
        ImportJobStatus status, string expectedDisplay, Color expectedColor, int expectedProgress)
    {
        var viewModel = ViewModelWithStatus(status);

        viewModel.StatusDisplay.Should().Be(expectedDisplay);
        viewModel.StatusColor.Should().Be(expectedColor);
        viewModel.ProgressPercent.Should().Be(expectedProgress);
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
