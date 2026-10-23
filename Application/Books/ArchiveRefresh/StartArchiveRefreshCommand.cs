using Application.Abstractions.Messaging;

namespace Application.Books.ArchiveRefresh;

// Returns the number of digital books whose cover and pages will be read again from their archive in the background.
public record StartArchiveRefreshCommand(Guid UserId) : ICommand<int>;
