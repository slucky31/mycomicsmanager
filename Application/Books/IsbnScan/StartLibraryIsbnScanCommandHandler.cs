using Application.Abstractions.Messaging;
using Application.Interfaces;
using Domain.Libraries;
using Domain.Primitives;

namespace Application.Books.IsbnScan;

public sealed class StartLibraryIsbnScanCommandHandler(
    IRepository<Library, Guid> libraryRepository,
    IBookRepository bookRepository,
    IIsbnScanJobEnqueuer enqueuer) : ICommandHandler<StartLibraryIsbnScanCommand, int>
{
    public async Task<Result<int>> Handle(StartLibraryIsbnScanCommand request, CancellationToken cancellationToken)
    {
        if (request is null || request.LibraryId == Guid.Empty || request.UserId == Guid.Empty)
        {
            return LibrariesError.BadRequest;
        }

        // A library of another user is reported as not found, so its existence is not disclosed.
        var library = await libraryRepository.GetByIdAsync(request.LibraryId);
        if (library is null || library.UserId != request.UserId)
        {
            return LibrariesError.NotFound;
        }

        if (library.BookType != LibraryBookType.Digital)
        {
            return LibrariesError.BookTypeMismatch;
        }

        var bookIds = await bookRepository.ListIdsToScanForIsbnAsync(library.Id, cancellationToken);
        if (bookIds.Count > 0)
        {
            enqueuer.Enqueue(library.Id);
        }

        return bookIds.Count;
    }
}
