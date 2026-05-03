using Application.ImportJobs.List;
using Application.Interfaces;
using Domain.ImportJobs;
using Domain.Libraries;
using NSubstitute;

namespace Application.UnitTests.ImportJobs;

public class ListImportJobsQueryHandlerTests
{
    private readonly ListImportJobsQueryHandler _handler;
    private readonly IImportJobRepository _repository;
    private readonly IRepository<Library, Guid> _libraryRepository;

    private static readonly Guid s_userId = Guid.CreateVersion7();

    public ListImportJobsQueryHandlerTests()
    {
        _repository = Substitute.For<IImportJobRepository>();
        _libraryRepository = Substitute.For<IRepository<Library, Guid>>();
        _handler = new ListImportJobsQueryHandler(_repository, _libraryRepository);
    }

    private static Library CreateDigitalLibrary(Guid? userId = null) =>
        Library.Create("Digital", "#000000", "Icon", LibraryBookType.Digital, userId ?? s_userId).Value!;

    [Fact]
    public async Task Handle_Should_ReturnImportJobs_ForLibrary()
    {
        // Arrange
        var library = CreateDigitalLibrary();
        var jobs = new List<ImportJob>
        {
            ImportJob.Create("a.cbz", "/a.cbz", 1024, library.Id).Value!,
            ImportJob.Create("b.cbz", "/b.cbz", 2048, library.Id).Value!,
        };
        _libraryRepository.GetByIdAsync(library.Id).Returns(library);
        _repository.GetByLibraryIdAsync(library.Id, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<ImportJob>)jobs);

        // Act
        var result = await _handler.Handle(new ListImportJobsQuery(library.Id, s_userId), TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Should().HaveCount(2);
    }

    [Fact]
    public async Task Handle_Should_ReturnEmptyList_WhenNoJobs()
    {
        // Arrange
        var library = CreateDigitalLibrary();
        _libraryRepository.GetByIdAsync(library.Id).Returns(library);
        _repository.GetByLibraryIdAsync(library.Id, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<ImportJob>)[]);

        // Act
        var result = await _handler.Handle(new ListImportJobsQuery(library.Id, s_userId), TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_Should_ReturnNotFound_WhenLibraryDoesNotExist()
    {
        // Arrange
        _libraryRepository.GetByIdAsync(Arg.Any<Guid>()).Returns((Library?)null);

        // Act
        var result = await _handler.Handle(new ListImportJobsQuery(Guid.CreateVersion7(), s_userId), TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(LibrariesError.NotFound);
    }
}
