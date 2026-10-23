using Application.ImportJobs;
using Application.ImportJobs.MigrateLegacy;
using Application.Interfaces;
using Application.Libraries;
using Domain.Errors;
using Domain.Libraries;
using NSubstitute;

namespace Application.UnitTests.ImportJobs;

public sealed class StartLegacyMigrationCommandHandlerTests
{
    private static readonly Guid s_userId = Guid.CreateVersion7();
    private static readonly Library s_imported = Library.Create(LibraryConstants.ImportedName, "#5C6BC0", "Bookmark", LibraryBookType.Digital, s_userId).Value!;

    private readonly ILegacyLibrarySource _source = Substitute.For<ILegacyLibrarySource>();
    private readonly ILibraryReadService _libraryReadService = Substitute.For<ILibraryReadService>();
    private readonly ILegacyMigrationJobEnqueuer _enqueuer = Substitute.For<ILegacyMigrationJobEnqueuer>();
    private readonly StartLegacyMigrationCommandHandler _handler;

    public StartLegacyMigrationCommandHandlerTests()
    {
        _libraryReadService.GetByNameAsync(LibraryConstants.ImportedName, s_userId, Arg.Any<CancellationToken>()).Returns(s_imported);
        _handler = new StartLegacyMigrationCommandHandler(_source, _libraryReadService, _enqueuer);
    }

    [Fact]
    public async Task Handle_Should_EnqueueTheMigration_AndReturnTheNumberOfFilesToMove()
    {
        _source.GetFirstFiles(20).Returns(new LegacyLibraryBatch(["/old/a.cbz", "/old/b.cbz"], 2));

        var result = await _handler.Handle(new StartLegacyMigrationCommand(20, s_userId), TestContext.Current.CancellationToken);

        result.Value.Should().Be(2);
        _enqueuer.Received(1).Enqueue(20, s_userId);
    }

    [Fact]
    public async Task Handle_Should_ReturnTheErrorWithoutEnqueuing_WhenTheOldLibraryIsEmpty()
    {
        _source.GetFirstFiles(20).Returns(LegacyMigrationError.SourceEmpty);

        var result = await _handler.Handle(new StartLegacyMigrationCommand(20, s_userId), TestContext.Current.CancellationToken);

        result.Error.Should().Be(LegacyMigrationError.SourceEmpty);
        _enqueuer.DidNotReceive().Enqueue(Arg.Any<int>(), Arg.Any<Guid>());
    }
}
