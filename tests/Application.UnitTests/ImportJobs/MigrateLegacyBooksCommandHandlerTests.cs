using Application.ImportJobs;
using Application.ImportJobs.MigrateLegacy;
using Application.Libraries;
using Domain.Errors;
using Domain.Libraries;
using Domain.Primitives;
using NSubstitute;

namespace Application.UnitTests.ImportJobs;

public sealed class MigrateLegacyBooksCommandHandlerTests
{
    private static readonly Guid s_userId = Guid.CreateVersion7();
    private static readonly Library s_imported = Library.Create(LibraryConstants.ImportedName, "#5C6BC0", "Bookmark", LibraryBookType.Digital, s_userId).Value!;
    private static readonly string s_importDirectory = Path.Combine("/data/import", s_imported.ImportDirectoryName);

    private readonly ILegacyLibrarySource _source = Substitute.For<ILegacyLibrarySource>();
    private readonly ILibraryReadService _libraryReadService = Substitute.For<ILibraryReadService>();
    private readonly IImportDirectoryStorage _storage = Substitute.For<IImportDirectoryStorage>();
    private readonly MigrateLegacyBooksCommandHandler _handler;

    public MigrateLegacyBooksCommandHandlerTests()
    {
        _libraryReadService.GetByNameAsync(LibraryConstants.ImportedName, s_userId, Arg.Any<CancellationToken>()).Returns(s_imported);
        _storage.EnsureExists(s_imported.ImportDirectoryName).Returns(Result.Success());
        _storage.GetAvailableFilePath(s_imported.ImportDirectoryName, Arg.Any<string>())
            .Returns(call => Result<string>.Success(Path.Combine(s_importDirectory, call.ArgAt<string>(1))));
        _storage.DepositFile(Arg.Any<string>(), Arg.Any<string>()).Returns(Result.Success());
        _handler = new MigrateLegacyBooksCommandHandler(_source, _libraryReadService, _storage, NullLogger<MigrateLegacyBooksCommandHandler>.Instance);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(MigrateLegacyBooksCommandHandler.MaxCount + 1)]
    public async Task Handle_Should_ReturnBadRequest_WhenCountIsOutOfRange(int count)
    {
        var result = await _handler.Handle(new MigrateLegacyBooksCommand(count, s_userId), TestContext.Current.CancellationToken);

        result.Error.Should().Be(LegacyMigrationError.BadRequest);
        _source.DidNotReceive().GetFirstFiles(Arg.Any<int>());
    }

    [Fact]
    public async Task Handle_Should_ReturnBadRequest_WhenUserIdIsEmpty()
    {
        var result = await _handler.Handle(new MigrateLegacyBooksCommand(20, Guid.Empty), TestContext.Current.CancellationToken);

        result.Error.Should().Be(LegacyMigrationError.BadRequest);
        await _libraryReadService.DidNotReceive().GetByNameAsync(Arg.Any<string>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_ReturnImportedLibraryNotFound_WhenTheUserHasNoImportedLibrary()
    {
        _libraryReadService.GetByNameAsync(LibraryConstants.ImportedName, s_userId, Arg.Any<CancellationToken>()).Returns((Library?)null);

        var result = await _handler.Handle(new MigrateLegacyBooksCommand(20, s_userId), TestContext.Current.CancellationToken);

        result.Error.Should().Be(LegacyMigrationError.ImportedLibraryNotFound);
        _source.DidNotReceive().GetFirstFiles(Arg.Any<int>());
    }

    [Fact]
    public async Task Handle_Should_ReturnImportedLibraryNotFound_WhenTheImportedLibraryIsPhysical()
    {
        var physical = Library.Create(LibraryConstants.ImportedName, "#5C6BC0", "Bookmark", LibraryBookType.Physical, s_userId).Value!;
        _libraryReadService.GetByNameAsync(LibraryConstants.ImportedName, s_userId, Arg.Any<CancellationToken>()).Returns(physical);

        var result = await _handler.Handle(new MigrateLegacyBooksCommand(20, s_userId), TestContext.Current.CancellationToken);

        result.Error.Should().Be(LegacyMigrationError.ImportedLibraryNotFound);
    }

    [Fact]
    public async Task Handle_Should_ReturnTheSourceError_WhenTheOldLibraryCannotBeRead()
    {
        _source.GetFirstFiles(20).Returns(LegacyMigrationError.NotConfigured);

        var result = await _handler.Handle(new MigrateLegacyBooksCommand(20, s_userId), TestContext.Current.CancellationToken);

        result.Error.Should().Be(LegacyMigrationError.NotConfigured);
        _storage.DidNotReceive().EnsureExists(Arg.Any<string>());
    }

    [Fact]
    public async Task Handle_Should_ReturnTheStorageError_WhenTheImportDirectoryCannotBeCreated()
    {
        _source.GetFirstFiles(20).Returns(new LegacyLibraryBatch(["/old/a.cbz"], 1));
        _storage.EnsureExists(s_imported.ImportDirectoryName).Returns(ImportDirectoryStorageError.InvalidPath);

        var result = await _handler.Handle(new MigrateLegacyBooksCommand(20, s_userId), TestContext.Current.CancellationToken);

        result.Error.Should().Be(ImportDirectoryStorageError.InvalidPath);
        _storage.DidNotReceive().DepositFile(Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact]
    public async Task Handle_Should_DepositEachFileInTheImportDirectory_AndCountWhatIsLeft()
    {
        _source.GetFirstFiles(2).Returns(new LegacyLibraryBatch(["/old/a.cbz", "/old/b.cbz"], 5));

        var result = await _handler.Handle(new MigrateLegacyBooksCommand(2, s_userId), TestContext.Current.CancellationToken);

        result.Value.Should().Be(new MigrateLegacyBooksResult(Moved: 2, Skipped: 0, Failed: 0, Remaining: 3));
        _storage.Received(1).DepositFile("/old/a.cbz", Path.Combine(s_importDirectory, "a.cbz"));
        _storage.Received(1).DepositFile("/old/b.cbz", Path.Combine(s_importDirectory, "b.cbz"));
    }

    [Fact]
    public async Task Handle_Should_SkipAFile_WhenAFileWithTheSameNameIsAlreadyInTheImportDirectory()
    {
        _source.GetFirstFiles(1).Returns(new LegacyLibraryBatch(["/old/a.cbz"], 1));
        _storage.GetAvailableFilePath(s_imported.ImportDirectoryName, "a.cbz").Returns(Path.Combine(s_importDirectory, "a (2).cbz"));

        var result = await _handler.Handle(new MigrateLegacyBooksCommand(1, s_userId), TestContext.Current.CancellationToken);

        result.Value.Should().Be(new MigrateLegacyBooksResult(Moved: 0, Skipped: 1, Failed: 0, Remaining: 1));
        _storage.DidNotReceive().DepositFile(Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact]
    public async Task Handle_Should_CountAFailureAndGoOn_WhenAFileCannotBeMoved()
    {
        _source.GetFirstFiles(3).Returns(new LegacyLibraryBatch(["/old/a.cbz", "/old/b?.cbz", "/old/c.cbz"], 3));
        _storage.GetAvailableFilePath(s_imported.ImportDirectoryName, "b?.cbz").Returns(ImportDirectoryStorageError.InvalidFileName);
        _storage.DepositFile("/old/c.cbz", Arg.Any<string>()).Returns(ImportDirectoryStorageError.DepositFailed);

        var result = await _handler.Handle(new MigrateLegacyBooksCommand(3, s_userId), TestContext.Current.CancellationToken);

        result.Value.Should().Be(new MigrateLegacyBooksResult(Moved: 1, Skipped: 0, Failed: 2, Remaining: 2));
    }
}
