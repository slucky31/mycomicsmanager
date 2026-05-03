using Domain.ImportJobs;

namespace Domain.UnitTests.ImportJobs;

public class ImportJobTests
{
    private static readonly Guid DefaultLibraryId = Guid.CreateVersion7();
    private const string DefaultFileName = "blacksad-t01.cbz";
    private const string DefaultFilePath = "/data/import/blacksad-t01.cbz";
    private const long DefaultFileSize = 52_428_800L; // 50 MB

    private static ImportJob CreateValidJob() =>
        ImportJob.Create(DefaultFileName, DefaultFilePath, DefaultFileSize, DefaultLibraryId).Value!;

    // -------------------------------------------------------
    // Create
    // -------------------------------------------------------

    [Fact]
    public void Create_Should_ReturnSuccess_WhenParametersAreValid()
    {
        // Act
        var result = ImportJob.Create(DefaultFileName, DefaultFilePath, DefaultFileSize, DefaultLibraryId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
    }

    [Fact]
    public void Create_Should_ReturnBadRequest_WhenFileNameIsEmpty()
    {
        // Act
        var result = ImportJob.Create(string.Empty, DefaultFilePath, DefaultFileSize, DefaultLibraryId);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ImportJobError.BadRequest);
    }

    [Fact]
    public void Create_Should_ReturnBadRequest_WhenFileNameIsWhitespace()
    {
        // Act
        var result = ImportJob.Create("   ", DefaultFilePath, DefaultFileSize, DefaultLibraryId);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ImportJobError.BadRequest);
    }

    [Fact]
    public void Create_Should_ReturnBadRequest_WhenFilePathIsEmpty()
    {
        // Act
        var result = ImportJob.Create(DefaultFileName, string.Empty, DefaultFileSize, DefaultLibraryId);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ImportJobError.BadRequest);
    }

    [Fact]
    public void Create_Should_ReturnBadRequest_WhenFilePathIsWhitespace()
    {
        // Act
        var result = ImportJob.Create(DefaultFileName, "   ", DefaultFileSize, DefaultLibraryId);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ImportJobError.BadRequest);
    }

    [Fact]
    public void Create_Should_ReturnBadRequest_WhenFileSizeIsZero()
    {
        // Act
        var result = ImportJob.Create(DefaultFileName, DefaultFilePath, 0, DefaultLibraryId);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ImportJobError.BadRequest);
    }

    [Fact]
    public void Create_Should_ReturnBadRequest_WhenFileSizeIsNegative()
    {
        // Act
        var result = ImportJob.Create(DefaultFileName, DefaultFilePath, -1, DefaultLibraryId);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ImportJobError.BadRequest);
    }

    [Fact]
    public void Create_Should_ReturnBadRequest_WhenLibraryIdIsEmpty()
    {
        // Act
        var result = ImportJob.Create(DefaultFileName, DefaultFilePath, DefaultFileSize, Guid.Empty);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ImportJobError.BadRequest);
    }

    [Fact]
    public void Create_Should_SetStatusToPending()
    {
        // Act
        var result = ImportJob.Create(DefaultFileName, DefaultFilePath, DefaultFileSize, DefaultLibraryId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be(ImportJobStatus.Pending);
    }

    [Fact]
    public void Create_Should_SetCreatedAtToUtcNow()
    {
        // Arrange
        var before = DateTime.UtcNow;

        // Act
        var result = ImportJob.Create(DefaultFileName, DefaultFilePath, DefaultFileSize, DefaultLibraryId);
        var after = DateTime.UtcNow;

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.CreatedAt.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
    }

    [Fact]
    public void Create_Should_SetCorrectProperties()
    {
        // Act
        var result = ImportJob.Create(DefaultFileName, DefaultFilePath, DefaultFileSize, DefaultLibraryId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.OriginalFileName.Should().Be(DefaultFileName);
        result.Value.OriginalFilePath.Should().Be(DefaultFilePath);
        result.Value.OriginalFileSize.Should().Be(DefaultFileSize);
        result.Value.LibraryId.Should().Be(DefaultLibraryId);
        result.Value.DigitalBookId.Should().BeNull();
        result.Value.CompletedAt.Should().BeNull();
        result.Value.ErrorMessage.Should().BeNull();
        result.Value.ErrorStep.Should().BeNull();
    }

    // -------------------------------------------------------
    // Advance
    // -------------------------------------------------------

    [Fact]
    public void Advance_Should_ReturnSuccess_WhenTransitionPendingToExtracting()
    {
        // Arrange
        var job = CreateValidJob();

        // Act
        var result = job.Advance(ImportJobStatus.Extracting);

        // Assert
        result.IsSuccess.Should().BeTrue();
        job.Status.Should().Be(ImportJobStatus.Extracting);
    }

    [Fact]
    public void Advance_Should_ReturnSuccess_WhenFullValidSequence()
    {
        // Arrange
        var job = CreateValidJob();

        // Act & Assert
        job.Advance(ImportJobStatus.Extracting).IsSuccess.Should().BeTrue();
        job.Advance(ImportJobStatus.Converting).IsSuccess.Should().BeTrue();
        job.Advance(ImportJobStatus.SearchingMetadata).IsSuccess.Should().BeTrue();
        job.Advance(ImportJobStatus.UploadingCover).IsSuccess.Should().BeTrue();
        job.Advance(ImportJobStatus.BuildingArchive).IsSuccess.Should().BeTrue();
        job.Status.Should().Be(ImportJobStatus.BuildingArchive);
    }

    [Fact]
    public void Advance_Should_ReturnError_WhenTransitionIsInvalid_PendingToCompleted()
    {
        // Arrange
        var job = CreateValidJob();

        // Act
        var result = job.Advance(ImportJobStatus.Completed);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ImportJobError.InvalidStatusTransition);
        job.Status.Should().Be(ImportJobStatus.Pending);
    }

    [Fact]
    public void Advance_Should_ReturnError_WhenTransitionIsInvalid_SkipsStep()
    {
        // Arrange
        var job = CreateValidJob();
        job.Advance(ImportJobStatus.Extracting);

        // Act — skip Converting, try to go to SearchingMetadata
        var result = job.Advance(ImportJobStatus.SearchingMetadata);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ImportJobError.InvalidStatusTransition);
        job.Status.Should().Be(ImportJobStatus.Extracting);
    }

    [Fact]
    public void Advance_Should_ReturnError_WhenTransitionIsInvalid_GoesBackwards()
    {
        // Arrange
        var job = CreateValidJob();
        job.Advance(ImportJobStatus.Extracting);
        job.Advance(ImportJobStatus.Converting);

        // Act — try to go back to Extracting
        var result = job.Advance(ImportJobStatus.Extracting);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ImportJobError.InvalidStatusTransition);
        job.Status.Should().Be(ImportJobStatus.Converting);
    }

    [Fact]
    public void Advance_Should_ReturnError_WhenAlreadyCompleted()
    {
        // Arrange
        var job = CreateValidJob();
        job.Advance(ImportJobStatus.Extracting);
        job.Advance(ImportJobStatus.Converting);
        job.Advance(ImportJobStatus.SearchingMetadata);
        job.Advance(ImportJobStatus.UploadingCover);
        job.Advance(ImportJobStatus.BuildingArchive);
        job.Complete(Guid.CreateVersion7());

        // Act
        var result = job.Advance(ImportJobStatus.Extracting);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ImportJobError.AlreadyCompleted);
    }

    [Fact]
    public void Advance_Should_ReturnError_WhenAlreadyFailed()
    {
        // Arrange
        var job = CreateValidJob();
        job.Fail("Extracting", "Archive corrupted");

        // Act
        var result = job.Advance(ImportJobStatus.Extracting);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ImportJobError.AlreadyFailed);
    }

    // -------------------------------------------------------
    // Fail
    // -------------------------------------------------------

    [Fact]
    public void Fail_Should_SetStatusAndErrorDetails()
    {
        // Arrange
        var job = CreateValidJob();
        job.Advance(ImportJobStatus.Extracting);

        // Act
        var result = job.Fail("Extracting", "Archive is corrupted");

        // Assert
        result.IsSuccess.Should().BeTrue();
        job.Status.Should().Be(ImportJobStatus.Failed);
        job.ErrorStep.Should().Be("Extracting");
        job.ErrorMessage.Should().Be("Archive is corrupted");
    }

    [Fact]
    public void Fail_Should_ReturnError_WhenAlreadyCompleted()
    {
        // Arrange
        var job = CreateValidJob();
        job.Advance(ImportJobStatus.Extracting);
        job.Advance(ImportJobStatus.Converting);
        job.Advance(ImportJobStatus.SearchingMetadata);
        job.Advance(ImportJobStatus.UploadingCover);
        job.Advance(ImportJobStatus.BuildingArchive);
        job.Complete(Guid.CreateVersion7());

        // Act
        var result = job.Fail("BuildingArchive", "Some error");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ImportJobError.AlreadyCompleted);
    }

    [Fact]
    public void Fail_Should_ReturnError_WhenAlreadyFailed()
    {
        // Arrange
        var job = CreateValidJob();
        job.Fail("Extracting", "First error");

        // Act
        var result = job.Fail("Converting", "Second error");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ImportJobError.AlreadyFailed);
    }

    // -------------------------------------------------------
    // Complete
    // -------------------------------------------------------

    [Fact]
    public void Complete_Should_SetStatusAndBookId()
    {
        // Arrange
        var job = CreateValidJob();
        job.Advance(ImportJobStatus.Extracting);
        job.Advance(ImportJobStatus.Converting);
        job.Advance(ImportJobStatus.SearchingMetadata);
        job.Advance(ImportJobStatus.UploadingCover);
        job.Advance(ImportJobStatus.BuildingArchive);
        var bookId = Guid.CreateVersion7();

        // Act
        var result = job.Complete(bookId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        job.Status.Should().Be(ImportJobStatus.Completed);
        job.DigitalBookId.Should().Be(bookId);
    }

    [Fact]
    public void Complete_Should_SetCompletedAt()
    {
        // Arrange
        var job = CreateValidJob();
        job.Advance(ImportJobStatus.Extracting);
        job.Advance(ImportJobStatus.Converting);
        job.Advance(ImportJobStatus.SearchingMetadata);
        job.Advance(ImportJobStatus.UploadingCover);
        job.Advance(ImportJobStatus.BuildingArchive);
        var before = DateTime.UtcNow;

        // Act
        job.Complete(Guid.CreateVersion7());
        var after = DateTime.UtcNow;

        // Assert
        job.CompletedAt.Should().NotBeNull();
        job.CompletedAt!.Value.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
    }

    [Fact]
    public void Complete_Should_ReturnError_WhenAlreadyFailed()
    {
        // Arrange
        var job = CreateValidJob();
        job.Fail("Extracting", "Archive corrupted");

        // Act
        var result = job.Complete(Guid.CreateVersion7());

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ImportJobError.AlreadyFailed);
    }

    [Fact]
    public void Complete_Should_ReturnError_WhenNotInBuildingArchiveStatus()
    {
        // Arrange
        var job = CreateValidJob();
        job.Advance(ImportJobStatus.Extracting);
        job.Advance(ImportJobStatus.Converting);
        // Still in Converting — not yet at BuildingArchive

        // Act
        var result = job.Complete(Guid.CreateVersion7());

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ImportJobError.InvalidStatusTransition);
    }

    [Fact]
    public void Complete_Should_ReturnError_WhenStillPending()
    {
        // Arrange
        var job = CreateValidJob();

        // Act
        var result = job.Complete(Guid.CreateVersion7());

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ImportJobError.InvalidStatusTransition);
    }
}
