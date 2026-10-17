using Application.Abstractions.Messaging;
using Application.Libraries;
using Application.Libraries.RenameLegacyImported;
using Application.Libraries.Update;
using Domain.Libraries;
using Domain.Primitives;
using NSubstitute;

namespace Application.UnitTests.Libraries;

public class RenameLegacyImportedLibrariesCommandHandlerTests
{
    private readonly ILibraryReadService _readService = Substitute.For<ILibraryReadService>();
    private readonly ICommandHandler<UpdateLibraryCommand, Library> _updateHandler = Substitute.For<ICommandHandler<UpdateLibraryCommand, Library>>();
    private readonly RenameLegacyImportedLibrariesCommandHandler _handler;

    public RenameLegacyImportedLibrariesCommandHandlerTests()
    {
        _updateHandler.Handle(Arg.Any<UpdateLibraryCommand>(), Arg.Any<CancellationToken>())
            .Returns(call => Result<Library>.Success(Library.Create("Imported", "#5C6BC0", "Inbox", LibraryBookType.Digital, call.Arg<UpdateLibraryCommand>().UserId).Value!));
        _handler = new RenameLegacyImportedLibrariesCommandHandler(_readService, _updateHandler, NullLogger<RenameLegacyImportedLibrariesCommandHandler>.Instance);
    }

    private static Library Legacy() => Library.Create("À trier", "#2E7D32", "Inbox", LibraryBookType.Digital, Guid.CreateVersion7()).Value!;

    [Fact]
    public async Task Handle_Should_RenameEveryLegacyLibrary_ExceptForAUserWhoAlreadyHasAnImportedOne()
    {
        // Arrange
        var renamed = Legacy();
        var kept = Legacy();
        var failed = Legacy();
        _readService.ListByNameAsync("À trier", Arg.Any<CancellationToken>()).Returns([renamed, kept, failed]);
        _readService.ExistsByNameAsync("Imported", kept.UserId, kept.Id, Arg.Any<CancellationToken>()).Returns(true);
        _updateHandler.Handle(Arg.Is<UpdateLibraryCommand>(c => c.Id == failed.Id), Arg.Any<CancellationToken>())
            .Returns(Result<Library>.Failure(LibrariesError.FolderNotMoved));

        // Act
        var result = await _handler.Handle(new RenameLegacyImportedLibrariesCommand(), TestContext.Current.CancellationToken);

        // Assert
        result.Value.Should().Be(1);
        await _updateHandler.Received(1).Handle(
            new UpdateLibraryCommand(renamed.Id, "Imported", "#2E7D32", "Inbox", renamed.UserId), Arg.Any<CancellationToken>());
        await _updateHandler.DidNotReceive().Handle(Arg.Is<UpdateLibraryCommand>(c => c.Id == kept.Id), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_RenameNothing_WhenNoLibraryHasTheLegacyName()
    {
        // Arrange
        _readService.ListByNameAsync("À trier", Arg.Any<CancellationToken>()).Returns([]);

        // Act
        var result = await _handler.Handle(new RenameLegacyImportedLibrariesCommand(), TestContext.Current.CancellationToken);

        // Assert
        result.Value.Should().Be(0);
        await _updateHandler.DidNotReceive().Handle(Arg.Any<UpdateLibraryCommand>(), Arg.Any<CancellationToken>());
    }
}
