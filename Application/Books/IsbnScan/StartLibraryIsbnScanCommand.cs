using Application.Abstractions.Messaging;

namespace Application.Books.IsbnScan;

// Returns the number of books whose pages will be scanned in the background.
public record StartLibraryIsbnScanCommand(Guid LibraryId, Guid UserId) : ICommand<int>;
