using Application.ImportJobs.GetById;
using Application.Interfaces;
using Domain.ImportJobs;
using Domain.Libraries;
using NSubstitute;

namespace Application.UnitTests.ImportJobs;

public class GetImportJobQueryHandlerTests
{
    private readonly GetImportJobQueryHandler _handler;
    private readonly IImportJobRepository _repository;
    private readonly IRepository<Library, Guid> _libraryRepository;

    private static readonly Guid s_userId = Guid.CreateVersion7();
    private static readonly Guid s_libraryId = Guid.CreateVersion7();

    public GetImportJobQueryHandlerTests()
    {
        _repository = Substitute.For<IImportJobRepository>();
        _libraryRepository = Substitute.For<IRepository<Library, Guid>>();
        _handler = new GetImportJobQueryHandler(_repository, _libraryRepository);
    }

    private static Library CreateLibrary(Guid? userId = null)
        => Library.Create("Test", "#FF0000", "book", LibraryBookType.Digital, userId ?? s_userId).Value!;

    private static ImportJob CreateJob()
        => ImportJob.Create("file.cbz", "/tmp/file.cbz", 1024, s_libraryId).Value!;

    [Fact]
    public async Task Handle_Should_ReturnImportJob_WhenFound()
    {
        // Arrange
        var job = CreateJob();
        _repository.GetByIdAsync(job.Id, Arg.Any<CancellationToken>()).Returns(job);
        _libraryRepository.GetByIdAsync(s_libraryId).Returns(CreateLibrary());

        // Act
        var result = await _handler.Handle(new GetImportJobQuery(job.Id, s_userId), TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Id.Should().Be(job.Id);
    }

    [Fact]
    public async Task Handle_Should_ReturnNotFound_WhenNotFound()
    {
        // Arrange
        _repository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((ImportJob?)null);

        // Act
        var result = await _handler.Handle(new GetImportJobQuery(Guid.CreateVersion7(), s_userId), TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ImportJobError.NotFound);
    }

    [Fact]
    public async Task Handle_Should_ReturnNotFound_WhenJobBelongsToOtherUser()
    {
        // Arrange
        var job = CreateJob();
        var otherUserId = Guid.CreateVersion7();
        _repository.GetByIdAsync(job.Id, Arg.Any<CancellationToken>()).Returns(job);
        _libraryRepository.GetByIdAsync(s_libraryId).Returns(CreateLibrary(s_userId));

        // Act
        var result = await _handler.Handle(new GetImportJobQuery(job.Id, otherUserId), TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ImportJobError.NotFound);
    }

    [Fact]
    public async Task Handle_Should_ReturnBadRequest_WhenIdIsEmpty()
    {
        // Act
        var result = await _handler.Handle(new GetImportJobQuery(Guid.Empty, s_userId), TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ImportJobError.BadRequest);
    }

    [Fact]
    public async Task Handle_Should_ReturnBadRequest_WhenUserIdIsEmpty()
    {
        // Act
        var result = await _handler.Handle(new GetImportJobQuery(Guid.CreateVersion7(), Guid.Empty), TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ImportJobError.BadRequest);
    }
}
