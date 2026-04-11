using Application.ImportJobs.GetById;
using Application.Interfaces;
using Domain.ImportJobs;
using NSubstitute;

namespace Application.UnitTests.ImportJobs;

public class GetImportJobQueryHandlerTests
{
    private readonly GetImportJobQueryHandler _handler;
    private readonly IImportJobRepository _repository;

    public GetImportJobQueryHandlerTests()
    {
        _repository = Substitute.For<IImportJobRepository>();
        _handler = new GetImportJobQueryHandler(_repository);
    }

    private static ImportJob CreateJob()
    {
        var job = ImportJob.Create("file.cbz", "/tmp/file.cbz", 1024, Guid.CreateVersion7()).Value!;
        return job;
    }

    [Fact]
    public async Task Handle_Should_ReturnImportJob_WhenFound()
    {
        // Arrange
        var job = CreateJob();
        _repository.GetByIdAsync(job.Id, Arg.Any<CancellationToken>()).Returns(job);

        // Act
        var result = await _handler.Handle(new GetImportJobQuery(job.Id), default);

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
        var result = await _handler.Handle(new GetImportJobQuery(Guid.CreateVersion7()), default);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ImportJobError.NotFound);
    }

    [Fact]
    public async Task Handle_Should_ReturnBadRequest_WhenIdIsEmpty()
    {
        // Act
        var result = await _handler.Handle(new GetImportJobQuery(Guid.Empty), default);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ImportJobError.BadRequest);
    }
}
