using Application.Abstractions.Messaging;
using Application.Interfaces;
using Domain.Books;
using Domain.Primitives;
using Microsoft.Extensions.Logging;

namespace Application.Books.Covers;

public sealed class DeleteUnusedCoversCommandHandler(
    UnusedCoverFinder finder,
    ICloudinaryService cloudinary,
    ILogger<DeleteUnusedCoversCommandHandler> logger) : ICommandHandler<DeleteUnusedCoversCommand, DeletedCovers>
{
    public async Task<Result<DeletedCovers>> Handle(DeleteUnusedCoversCommand request, CancellationToken cancellationToken)
    {
        if (request?.PublicIds is null || request.PublicIds.Count == 0)
        {
            return BooksError.BadRequest;
        }

        // Found again: an image a book took since it was shown is not deleted.
        var result = await finder.FindAsync(cancellationToken);
        if (result.IsFailure)
        {
            return result.Error!;
        }

        var requested = request.PublicIds.ToHashSet(StringComparer.Ordinal);
        var images = result.Value!.Where(i => requested.Contains(i.PublicId)).ToList();
        if (images.Count == 0)
        {
            return new DeletedCovers(0, 0);
        }

        var deleteResult = await cloudinary.DeleteImagesAsync([.. images.Select(i => i.PublicId)], cancellationToken);
        if (deleteResult.IsFailure)
        {
            return deleteResult.Error!;
        }

        logger.LogInformation("{Deleted} unused cover image(s) deleted from Cloudinary", deleteResult.Value);
        return new DeletedCovers(deleteResult.Value, images.Sum(i => i.Bytes));
    }
}
