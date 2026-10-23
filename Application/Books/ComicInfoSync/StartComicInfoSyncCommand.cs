using Application.Abstractions.Messaging;

namespace Application.Books.ComicInfoSync;

// Returns the number of digital books whose ComicInfo.xml will be written in the background.
public record StartComicInfoSyncCommand(Guid UserId) : ICommand<int>;
