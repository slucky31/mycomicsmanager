using Application.ImportJobs;
using Application.Interfaces;
using Domain.ImportJobs;
using Domain.Libraries;
using NSubstitute;

namespace Application.UnitTests.ImportJobs;

public class FailedImportJobLoaderTests
{
    private readonly IImportJobRepository _importJobRepository = Substitute.For<IImportJobRepository>();
    private readonly IRepository<Library, Guid> _libraryRepository = Substitute.For<IRepository<Library, Guid>>();
    private readonly FailedImportJobLoader _loader;
    private readonly Guid _userId = Guid.CreateVersion7();
    private readonly Library _library;

    public FailedImportJobLoaderTests()
    {
        _loader = new FailedImportJobLoader(_importJobRepository, _libraryRepository);
        _library = Library.Create("Test", "#FF0000", "book", LibraryBookType.Digital, _userId).Value!;
        _libraryRepository.GetByIdAsync(_library.Id).Returns(_library);
    }

    private ImportJob CreateJob(bool failed)
    {
        var job = ImportJob.Create("comic.cbz", "/import/comic.cbz", 1024, _library.Id).Value!;
        if (failed)
        {
            job.Fail("Extracting", "Corrupted");
        }

        _importJobRepository.GetByIdAsync(job.Id, Arg.Any<CancellationToken>()).Returns(job);
        return job;
    }

    [Fact]
    public async Task LoadAsync_Should_ReturnTheJobAndItsLibrary_WhenTheJobFailed()
    {
        var job = CreateJob(failed: true);

        var result = await _loader.LoadAsync(job.Id, _userId, TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value.Job.Should().BeSameAs(job);
        result.Value.Library.Should().BeSameAs(_library);
    }

    [Fact]
    public async Task LoadAsync_Should_ReturnBadRequest_WhenAnIdIsEmpty()
    {
        var result = await _loader.LoadAsync(Guid.Empty, _userId, TestContext.Current.CancellationToken);

        result.Error.Should().Be(ImportJobError.BadRequest);
        await _importJobRepository.DidNotReceive().GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task LoadAsync_Should_ReturnNotFound_WhenTheJobDoesNotExist()
    {
        var result = await _loader.LoadAsync(Guid.CreateVersion7(), _userId, TestContext.Current.CancellationToken);

        result.Error.Should().Be(ImportJobError.NotFound);
    }

    [Fact]
    public async Task LoadAsync_Should_ReturnNotFound_WhenTheLibraryBelongsToAnotherUser()
    {
        var job = CreateJob(failed: true);

        var result = await _loader.LoadAsync(job.Id, Guid.CreateVersion7(), TestContext.Current.CancellationToken);

        result.Error.Should().Be(ImportJobError.NotFound);
    }

    [Fact]
    public async Task LoadAsync_Should_ReturnNotFailed_WhenTheJobDidNotFail()
    {
        var job = CreateJob(failed: false);

        var result = await _loader.LoadAsync(job.Id, _userId, TestContext.Current.CancellationToken);

        result.Error.Should().Be(ImportJobError.NotFailed);
    }
}
