using Application.ImportJobs.Create;
using Application.Interfaces;
using Domain.ImportJobs;
using Domain.Libraries;
using NSubstitute;

namespace Application.UnitTests.ImportJobs;

public class CreateImportJobCommandHandlerTests
{
    private readonly CreateImportJobCommandHandler _handler;
    private readonly IImportJobRepository _importJobRepository;
    private readonly IRepository<Library, Guid> _libraryRepository;
    private readonly IUnitOfWork _unitOfWork;

    private static readonly Guid s_userId = Guid.CreateVersion7();
    private static readonly Guid s_libraryId = Guid.CreateVersion7();

    public CreateImportJobCommandHandlerTests()
    {
        _importJobRepository = Substitute.For<IImportJobRepository>();
        _libraryRepository = Substitute.For<IRepository<Library, Guid>>();
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _handler = new CreateImportJobCommandHandler(_importJobRepository, _libraryRepository, _unitOfWork);
    }

    private static Library CreateDigitalLibrary(Guid? userId = null) =>
        Library.Create("Digital", "#000000", "Icon", LibraryBookType.Digital, userId ?? s_userId).Value!;

    private static CreateImportJobCommand ValidCommand(Guid? libraryId = null, Guid? userId = null) => new(
        "comic.cbz",
        "/srv/uploads/comic.cbz",
        10_240,
        libraryId ?? s_libraryId,
        userId ?? s_userId);

    private void SetupLibrary(Library library) =>
        _libraryRepository.GetByIdAsync(library.Id).Returns(library);

    // ── Happy path ────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_Should_ReturnSuccess_WhenValidCommand()
    {
        // Arrange
        var library = CreateDigitalLibrary();
        _libraryRepository.GetByIdAsync(Arg.Any<Guid>()).Returns(library);

        // Act
        var result = await _handler.Handle(ValidCommand(library.Id), default);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be(ImportJobStatus.Pending);
    }

    [Fact]
    public async Task Handle_Should_ReturnImportJobWithPendingStatus()
    {
        // Arrange
        var library = CreateDigitalLibrary();
        _libraryRepository.GetByIdAsync(Arg.Any<Guid>()).Returns(library);

        // Act
        var result = await _handler.Handle(ValidCommand(library.Id), default);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be(ImportJobStatus.Pending);
    }

    [Fact]
    public async Task Handle_Should_SaveImportJob()
    {
        // Arrange
        var library = CreateDigitalLibrary();
        _libraryRepository.GetByIdAsync(Arg.Any<Guid>()).Returns(library);

        // Act
        await _handler.Handle(ValidCommand(library.Id), default);

        // Assert
        _importJobRepository.Received(1).Add(Arg.Any<ImportJob>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    // ── Input validation ──────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_Should_ReturnBadRequest_WhenFileNameIsEmpty()
    {
        var cmd = new CreateImportJobCommand(string.Empty, "/path/file.cbz", 1024, s_libraryId, s_userId);
        var result = await _handler.Handle(cmd, default);
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ImportJobError.BadRequest);
    }

    [Fact]
    public async Task Handle_Should_ReturnBadRequest_WhenFilePathIsEmpty()
    {
        var cmd = new CreateImportJobCommand("comic.cbz", string.Empty, 1024, s_libraryId, s_userId);
        var result = await _handler.Handle(cmd, default);
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ImportJobError.BadRequest);
    }

    [Fact]
    public async Task Handle_Should_ReturnBadRequest_WhenFileSizeIsZero()
    {
        var cmd = new CreateImportJobCommand("comic.cbz", "/path/file.cbz", 0, s_libraryId, s_userId);
        var result = await _handler.Handle(cmd, default);
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ImportJobError.BadRequest);
    }

    [Fact]
    public async Task Handle_Should_ReturnBadRequest_WhenLibraryIdIsEmpty()
    {
        var cmd = new CreateImportJobCommand("comic.cbz", "/path/file.cbz", 1024, Guid.Empty, s_userId);
        var result = await _handler.Handle(cmd, default);
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ImportJobError.BadRequest);
    }

    [Fact]
    public async Task Handle_Should_ReturnBadRequest_WhenFileExtensionIsInvalid()
    {
        var cmd = new CreateImportJobCommand("comic.epub", "/path/file.epub", 1024, s_libraryId, s_userId);
        var result = await _handler.Handle(cmd, default);
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ImportJobError.BadRequest);
    }

    // ── Library checks ────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_Should_ReturnNotFound_WhenLibraryDoesNotExist()
    {
        // Arrange
        _libraryRepository.GetByIdAsync(Arg.Any<Guid>()).Returns((Library?)null);

        // Act
        var result = await _handler.Handle(ValidCommand(), default);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(LibrariesError.NotFound);
    }

    [Fact]
    public async Task Handle_Should_ReturnNotFound_WhenLibraryBelongsToDifferentUser()
    {
        // Arrange
        var otherUserId = Guid.CreateVersion7();
        var library = Library.Create("Digital", "#000000", "Icon", LibraryBookType.Digital, otherUserId).Value!;
        _libraryRepository.GetByIdAsync(library.Id).Returns(library);

        // Act
        var result = await _handler.Handle(ValidCommand(library.Id, s_userId), default);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(LibrariesError.NotFound);
    }

    [Fact]
    public async Task Handle_Should_ReturnBadRequest_WhenLibraryIsNotDigital()
    {
        // Arrange
        var library = Library.Create("Physical", "#000000", "Icon", LibraryBookType.Physical, s_userId).Value!;
        _libraryRepository.GetByIdAsync(library.Id).Returns(library);

        // Act
        var result = await _handler.Handle(ValidCommand(library.Id), default);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(LibrariesError.BookTypeMismatch);
    }
}
