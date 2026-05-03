using Application.ImportJobs.ForceFail;
using Application.Interfaces;
using Domain.ImportJobs;
using Domain.Libraries;
using NSubstitute;

namespace Application.UnitTests.ImportJobs;

public class ForceFailImportJobCommandHandlerTests
{
    private readonly ForceFailImportJobCommandHandler _handler;
    private readonly IImportJobRepository _importJobRepository;
    private readonly IRepository<Library, Guid> _libraryRepository;
    private readonly IUnitOfWork _unitOfWork;

    private static readonly Guid s_userId = Guid.CreateVersion7();
    private static readonly Guid s_libraryId = Guid.CreateVersion7();

    public ForceFailImportJobCommandHandlerTests()
    {
        _importJobRepository = Substitute.For<IImportJobRepository>();
        _libraryRepository = Substitute.For<IRepository<Library, Guid>>();
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _handler = new ForceFailImportJobCommandHandler(_importJobRepository, _libraryRepository, _unitOfWork);
    }

    private static Library CreateLibrary(Guid? userId = null)
        => Library.Create("Test", "#FF0000", "book", LibraryBookType.Digital, userId ?? s_userId).Value!;

    private static ImportJob CreateStuckJob(ImportJobStatus stuckStatus)
    {
        var job = ImportJob.Create("comic.cbz", "/data/comic.cbz", 1024, s_libraryId).Value!;
        if (stuckStatus != ImportJobStatus.Pending)
        {
            job.Advance(stuckStatus);
        }
        return job;
    }

    [Theory]
    [InlineData(ImportJobStatus.Pending)]
    [InlineData(ImportJobStatus.Extracting)]
    [InlineData(ImportJobStatus.Converting)]
    [InlineData(ImportJobStatus.BuildingArchive)]
    public async Task Handle_Should_ReturnSuccess_WhenJobIsNonTerminal(ImportJobStatus status)
    {
        // Arrange
        var job = CreateStuckJob(status);
        var command = new ForceFailImportJobCommand(job.Id, s_userId);
        _importJobRepository.GetByIdAsync(job.Id, Arg.Any<CancellationToken>()).Returns(job);
        _libraryRepository.GetByIdAsync(s_libraryId).Returns(CreateLibrary());

        // Act
        var result = await _handler.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.Should().BeTrue();
        job.Status.Should().Be(ImportJobStatus.Failed);
        _importJobRepository.Received(1).Update(job);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_ReturnAlreadyFailed_WhenJobIsAlreadyCompleted()
    {
        // Arrange
        var job = ImportJob.Create("comic.cbz", "/data/comic.cbz", 1024, s_libraryId).Value!;
        job.Advance(ImportJobStatus.Extracting);
        job.Advance(ImportJobStatus.Converting);
        job.Advance(ImportJobStatus.SearchingMetadata);
        job.Advance(ImportJobStatus.UploadingCover);
        job.Advance(ImportJobStatus.BuildingArchive);
        job.Complete(Guid.CreateVersion7());
        var command = new ForceFailImportJobCommand(job.Id, s_userId);
        _importJobRepository.GetByIdAsync(job.Id, Arg.Any<CancellationToken>()).Returns(job);
        _libraryRepository.GetByIdAsync(s_libraryId).Returns(CreateLibrary());

        // Act
        var result = await _handler.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ImportJobError.AlreadyFailed);
        _importJobRepository.DidNotReceive().Update(Arg.Any<ImportJob>());
    }

    [Fact]
    public async Task Handle_Should_ReturnAlreadyFailed_WhenJobIsAlreadyFailed()
    {
        // Arrange
        var job = ImportJob.Create("comic.cbz", "/data/comic.cbz", 1024, s_libraryId).Value!;
        job.Fail("Extracting", "Archive corrupted");
        var command = new ForceFailImportJobCommand(job.Id, s_userId);
        _importJobRepository.GetByIdAsync(job.Id, Arg.Any<CancellationToken>()).Returns(job);
        _libraryRepository.GetByIdAsync(s_libraryId).Returns(CreateLibrary());

        // Act
        var result = await _handler.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ImportJobError.AlreadyFailed);
    }

    [Fact]
    public async Task Handle_Should_ReturnNotFound_WhenJobDoesNotExist()
    {
        // Arrange
        var command = new ForceFailImportJobCommand(Guid.CreateVersion7(), s_userId);
        _importJobRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((ImportJob?)null);

        // Act
        var result = await _handler.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ImportJobError.NotFound);
    }

    [Fact]
    public async Task Handle_Should_ReturnNotFound_WhenJobBelongsToOtherUser()
    {
        // Arrange
        var job = CreateStuckJob(ImportJobStatus.Converting);
        var command = new ForceFailImportJobCommand(job.Id, Guid.CreateVersion7());
        _importJobRepository.GetByIdAsync(job.Id, Arg.Any<CancellationToken>()).Returns(job);
        _libraryRepository.GetByIdAsync(s_libraryId).Returns(CreateLibrary(s_userId));

        // Act
        var result = await _handler.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ImportJobError.NotFound);
        _importJobRepository.DidNotReceive().Update(Arg.Any<ImportJob>());
    }
}
