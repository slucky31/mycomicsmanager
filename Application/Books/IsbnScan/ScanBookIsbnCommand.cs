using Application.Abstractions.Messaging;

namespace Application.Books.IsbnScan;

// Reads the pages of one book right away, even if they were already scanned.
public record ScanBookIsbnCommand(Guid BookId, Guid UserId) : ICommand<IsbnScanOutcome>;
