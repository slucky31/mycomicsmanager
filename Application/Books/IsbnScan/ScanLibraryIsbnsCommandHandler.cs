using Application.Abstractions.Messaging;
using Application.Interfaces;
using Domain.Books;
using Domain.Libraries;
using Domain.Primitives;
using Microsoft.Extensions.Logging;

namespace Application.Books.IsbnScan;

/// <summary>
/// Reads the ISBN printed on the pages of the digital books of a library that have none
/// and were never scanned (see <see cref="BookIsbnScan"/> for what is kept on each book).
/// </summary>
public sealed class ScanLibraryIsbnsCommandHandler(
    IBookRepository bookRepository,
    IIsbnPageScanner scanner,
    IUnitOfWork unitOfWork,
    TimeProvider clock,
    ILogger<ScanLibraryIsbnsCommandHandler> logger) : ICommandHandler<ScanLibraryIsbnsCommand, LibraryIsbnScanSummary>
{
    public async Task<Result<LibraryIsbnScanSummary>> Handle(ScanLibraryIsbnsCommand request, CancellationToken cancellationToken)
    {
        if (request is null || request.LibraryId == Guid.Empty)
        {
            return LibrariesError.BadRequest;
        }

        var bookIds = await bookRepository.ListIdsToScanForIsbnAsync(request.LibraryId, cancellationToken);
        var outcomes = new List<IsbnScanOutcome>(bookIds.Count);
        foreach (var bookId in bookIds)
        {
            outcomes.Add(await ScanBookAsync(bookId, cancellationToken));
        }

        return new LibraryIsbnScanSummary(
            outcomes.Count(o => o == IsbnScanOutcome.Assigned),
            outcomes.Count(o => o == IsbnScanOutcome.WithCandidates),
            outcomes.Count(o => o == IsbnScanOutcome.WithoutIsbn),
            outcomes.Count(o => o == IsbnScanOutcome.NotScanned));
    }

    // Each book is saved on its own, so a scan interrupted midway keeps what it found.
    private async Task<IsbnScanOutcome> ScanBookAsync(Guid bookId, CancellationToken ct)
    {
        // The book may have been edited, scanned or deleted since the list was read.
        if (await bookRepository.GetByIdAsync(bookId) is not DigitalBook { IsbnScannedAt: null } book
            || !string.IsNullOrWhiteSpace(book.ISBN))
        {
            return IsbnScanOutcome.NotScanned;
        }

        var result = await BookIsbnScan.ScanAndSaveAsync(book, scanner, bookRepository, unitOfWork, clock, ct);
        if (result.IsFailure)
        {
            logger.LogError("Unable to save the ISBN scan of book {BookId}: {Error}", book.Id, result.Error?.Code);
            return IsbnScanOutcome.NotScanned;
        }

        return result.Value;
    }
}
