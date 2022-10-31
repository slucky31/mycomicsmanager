using FeedlyApiClient.Models;

namespace FeedlyApiClient;

public interface IFeedlyApiClient
{
    Task<List<Tag>?> GetStreamContentAsync(string streamId, CancellationToken cancellationToken);
    
}