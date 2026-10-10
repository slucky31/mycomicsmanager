using Application.Abstractions.Messaging;
using Application.Interfaces;
using Domain.Books;
using Domain.Libraries;
using Domain.Primitives;
using Microsoft.Extensions.Logging;

namespace Application.Books.IsbnScan;

/// <summary>
/// Reads the ISBN printed on the pages of the digital books of a library that have none.
/// A single ISBN is assigned to the book (its other metadata are left untouched); several,
/// or one already used by another book, are kept on the book for the user to pick.
/// </summary>
public sealed class ScanLibraryIsbnsCommandHandler(
    IBookRepository bookRepository,
    IIsbnPageScanner scanner,
    IUnitOfWork unitOfWork,
    TimeProvider clock,
    ILogger<ScanLibraryIsbnsCommandHandler> logger) : ICommandHandler<ScanLibraryIsbnsCommand, LibraryIsbnScanSummary>
{
    private enum Outcome { Assigned, WithCandidates, WithoutIsbn, NotScanned }

    public async Task<Result<LibraryIsbnScanSummary>> Handle(ScanLibraryIsbnsCommand request, CancellationToken cancellationToken)
    {
        if (request is null || request.LibraryId == Guid.Empty)
        {
            return LibrariesError.BadRequest;
        }

        var bookIds = await bookRepository.ListIdsToScanForIsbnAsync(request.LibraryId, cancellationToken);
        var outcomes = new List<Outcome>(bookIds.Count);
        foreach (var bookId in bookIds)
        {
            outcomes.Add(await ScanBookAsync(bookId, cancellationToken));
        }

        return new LibraryIsbnScanSummary(
            outcomes.Count(o => o == Outcome.Assigned),
            outcomes.Count(o => o == Outcome.WithCandidates),
            outcomes.Count(o => o == Outcome.WithoutIsbn),
            outcomes.Count(o => o == Outcome.NotScanned));
    }

    // Each book is saved on its own, so a scan interrupted midway keeps what it found.
    private async Task<Outcome> ScanBookAsync(Guid bookId, CancellationToken ct)
    {
        // The book may have been edited, scanned or deleted since the list was read.
        if (await bookRepository.GetByIdAsync(bookId) is not DigitalBook { IsbnScannedAt: null } book
            || !string.IsNullOrWhiteSpace(book.ISBN))
        {
            return Outcome.NotScanned;
        }

        var scan = await scanner.ScanArchiveAsync(book.FilePath, ct);
        if (!scan.Completed)
        {
            return Outcome.NotScanned;
        }

        var outcome = await ApplyScanAsync(book, scan.Isbns, ct);
        bookRepository.Update(book);
        var saveResult = await unitOfWork.SaveChangesAsync(ct);
        if (saveResult.IsFailure)
        {
            logger.LogError("Unable to save the ISBN scan of book {BookId}: {Error}", book.Id, saveResult.Error?.Code);
            return Outcome.NotScanned;
        }

        return outcome;
    }

    private async Task<Outcome> ApplyScanAsync(DigitalBook book, IReadOnlyList<string> isbns, CancellationToken ct)
    {
        var scannedAt = clock.GetUtcNow().UtcDateTime;
        if (isbns.Count == 1 && await bookRepository.GetByIsbnAsync(isbns[0], ct) is null)
        {
            book.RecordIsbnScan([], scannedAt);
            book.AssignIsbn(isbns[0]);
            return Outcome.Assigned;
        }

        // A single ISBN already used by another book is likely a duplicate: the user decides.
        book.RecordIsbnScan(isbns, scannedAt);
        return isbns.Count > 0 ? Outcome.WithCandidates : Outcome.WithoutIsbn;
    }
}
