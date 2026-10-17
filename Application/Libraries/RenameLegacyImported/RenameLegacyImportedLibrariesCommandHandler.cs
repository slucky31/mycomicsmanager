using Application.Abstractions.Messaging;
using Application.Libraries.Update;
using Domain.Libraries;
using Domain.Primitives;
using Microsoft.Extensions.Logging;

namespace Application.Libraries.RenameLegacyImported;

// Renames every "À trier" library to "Imported", its folders and the paths of its books with it;
// returns how many were renamed. A user who already has an "Imported" library keeps both, untouched.
public sealed class RenameLegacyImportedLibrariesCommandHandler(
    ILibraryReadService libraryReadService,
    ICommandHandler<UpdateLibraryCommand, Library> updateLibraryHandler,
    ILogger<RenameLegacyImportedLibrariesCommandHandler> logger) : ICommandHandler<RenameLegacyImportedLibrariesCommand, int>
{
    public const string LegacyName = "À trier";

    public async Task<Result<int>> Handle(RenameLegacyImportedLibrariesCommand request, CancellationToken cancellationToken)
    {
        var renamed = 0;
        foreach (var library in await libraryReadService.ListByNameAsync(LegacyName, cancellationToken))
        {
            if (await libraryReadService.ExistsByNameAsync(LibraryConstants.ImportedName, library.UserId, library.Id, cancellationToken))
            {
                logger.LogWarning("Library {LibraryId} keeps the name {LegacyName}: its user already has a library {ImportedName}",
                    library.Id, LegacyName, LibraryConstants.ImportedName);
                continue;
            }

            var result = await updateLibraryHandler.Handle(
                new UpdateLibraryCommand(library.Id, LibraryConstants.ImportedName, library.Color, library.Icon, library.UserId), cancellationToken);
            if (result.IsFailure)
            {
                logger.LogError("Library {LibraryId} could not be renamed {ImportedName}: [{Code}] {Description}",
                    library.Id, LibraryConstants.ImportedName, result.Error!.Code, result.Error.Description);
                continue;
            }

            logger.LogInformation("Library {LibraryId} renamed from {LegacyName} to {ImportedName}", library.Id, LegacyName, LibraryConstants.ImportedName);
            renamed++;
        }

        return renamed;
    }
}
