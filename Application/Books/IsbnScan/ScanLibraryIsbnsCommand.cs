using Application.Abstractions.Messaging;

namespace Application.Books.IsbnScan;

// Run in the background: the ownership of the library is checked when the scan is started.
public record ScanLibraryIsbnsCommand(Guid LibraryId) : ICommand<LibraryIsbnScanSummary>;

public sealed record LibraryIsbnScanSummary(int Assigned, int WithCandidates, int WithoutIsbn, int NotScanned);
