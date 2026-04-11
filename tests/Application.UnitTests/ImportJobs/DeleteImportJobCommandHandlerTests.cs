using Application.ImportJobs.Delete;
using Application.Interfaces;
using Domain.ImportJobs;
using Domain.Libraries;
using NSubstitute;

namespace Application.UnitTests.ImportJobs;

public class DeleteImportJobCommandHandlerTests
{
    private readonly DeleteImportJobCommandHandler _handler;
    private readonly IImportJobRepository _importJobRepository;
    private readonly IRepository<Library, Guid> _libraryRepository;
    private readonly IUnitOfWork _unitOfWork;

    private static readonly Guid s_userId = Guid.CreateVersion7();
    private static readonly Guid s_libraryId = Guid.CreateVersion7();

    public DeleteImportJobCommandHandlerTests()
    {
        _importJobRepository = Substitute.For<IImportJobRepository>();
        _libraryRepository = Substitute.For<IRepository<Library, Guid>>();
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _handler = new DeleteImportJobCommandHandler(_importJobRepository, _libraryRepository, _unitOfWork);
    }

    private static Library CreateLibrary(Guid? userId = null)
        => Library.Create("Test", "#FF0000", "book", LibraryBookType.Digital, userId ?? s_userId).Value!;

    private static ImportJob CreateCompletedJob()
    {
        var job = ImportJob.Create("comic.cbz", "/data/comic.cbz", 1024, s_libraryId).Value!;
        job.Advance(ImportJobStatus.Extracting);
        job.Advance(ImportJobStatus.Converting);
        job.Advance(ImportJobStatus.SearchingMetadata);
        job.Advance(ImportJobStatus.UploadingCover);
        job.Advance(ImportJobStatus.BuildingArchive);
        job.Complete(Guid.CreateVersion7());
        return job;
    }

    private static ImportJob CreateFailedJob()
    {
        var job = ImportJob.Create("comic.cbz", "/data/comic.cbz", 1024, s_libraryId).Value!;
        job.Fail("Extracting", "Archive is corrupted");
        return job;
    }

    private static ImportJob CreatePendingJob()
        => ImportJob.Create("comic.cbz", "/data/comic.cbz", 1024, s_libraryId).Value!;

    [Fact]
    public async Task Handle_Should_ReturnSuccess_WhenJobIsCompleted()
    {
        // Arrange
        var job = CreateCompletedJob();
        var command = new DeleteImportJobCommand(job.Id, s_userId);
        _importJobRepository.GetByIdAsync(job.Id, Arg.Any<CancellationToken>()).Returns(job);
        _libraryRepository.GetByIdAsync(s_libraryId).Returns(CreateLibrary());

        // Act
        var result = await _handler.Handle(command, default);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _importJobRepository.Received(1).Remove(job);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_ReturnSuccess_WhenJobIsFailed()
    {
        // Arrange
        var job = CreateFailedJob();
        var command = new DeleteImportJobCommand(job.Id, s_userId);
        _importJobRepository.GetByIdAsync(job.Id, Arg.Any<CancellationToken>()).Returns(job);
        _libraryRepository.GetByIdAsync(s_libraryId).Returns(CreateLibrary());

        // Act
        var result = await _handler.Handle(command, default);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _importJobRepository.Received(1).Remove(job);
    }

    [Fact]
    public async Task Handle_Should_ReturnNotTerminal_WhenJobIsInProgress()
    {
        // Arrange
        var job = CreatePendingJob();
        var command = new DeleteImportJobCommand(job.Id, s_userId);
        _importJobRepository.GetByIdAsync(job.Id, Arg.Any<CancellationToken>()).Returns(job);
        _libraryRepository.GetByIdAsync(s_libraryId).Returns(CreateLibrary());

        // Act
        var result = await _handler.Handle(command, default);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ImportJobError.NotTerminal);
        _importJobRepository.DidNotReceive().Remove(Arg.Any<ImportJob>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_ReturnNotFound_WhenJobDoesNotExist()
    {
        // Arrange
        var command = new DeleteImportJobCommand(Guid.CreateVersion7(), s_userId);
        _importJobRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((ImportJob?)null);

        // Act
        var result = await _handler.Handle(command, default);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ImportJobError.NotFound);
        _importJobRepository.DidNotReceive().Remove(Arg.Any<ImportJob>());
    }

    [Fact]
    public async Task Handle_Should_ReturnNotFound_WhenJobBelongsToOtherUser()
    {
        // Arrange
        var job = CreateCompletedJob();
        var otherUserId = Guid.CreateVersion7();
        var command = new DeleteImportJobCommand(job.Id, otherUserId);
        _importJobRepository.GetByIdAsync(job.Id, Arg.Any<CancellationToken>()).Returns(job);
        _libraryRepository.GetByIdAsync(s_libraryId).Returns(CreateLibrary(s_userId)); // owned by s_userId

        // Act
        var result = await _handler.Handle(command, default);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ImportJobError.NotFound);
        _importJobRepository.DidNotReceive().Remove(Arg.Any<ImportJob>());
    }
}
