using Application.Abstractions.Messaging;
using Application.Books.IsbnScan;
using Application.Interfaces;
using Domain.Primitives;

namespace Web.Services;

public class IsbnScanService(
    ICommandHandler<StartLibraryIsbnScanCommand, int> startScanHandler,
    ICommandHandler<ScanBookIsbnCommand, IsbnScanOutcome> scanBookHandler,
    ICurrentUserService currentUserService) : IIsbnScanService
{
    public async Task<Result<int>> StartLibraryScanAsync(Guid libraryId, CancellationToken cancellationToken = default)
    {
        var userIdResult = await currentUserService.GetCurrentUserIdAsync(cancellationToken);
        if (userIdResult.IsFailure)
        {
            return userIdResult.Error!;
        }

        return await startScanHandler.Handle(new StartLibraryIsbnScanCommand(libraryId, userIdResult.Value), cancellationToken);
    }

    public async Task<Result<IsbnScanOutcome>> ScanBookAsync(Guid bookId, CancellationToken cancellationToken = default)
    {
        var userIdResult = await currentUserService.GetCurrentUserIdAsync(cancellationToken);
        if (userIdResult.IsFailure)
        {
            return userIdResult.Error!;
        }

        return await scanBookHandler.Handle(new ScanBookIsbnCommand(bookId, userIdResult.Value), cancellationToken);
    }
}
