using System.Net.Http.Json;
using FeedlyApiClient.Constants;
using FeedlyApiClient.Models;
using Flurl;

namespace FeedlyApiClient;

// https://www.infoq.com/articles/creating-http-sdks-dotnet-6/
// https://github.com/NikiforovAll/http-sdk-guide/tree/main/src/ManualApiClient
public class FeedlyApiClient : IFeedlyApiClient
{
    private readonly HttpClient _httpClient;

    public FeedlyApiClient(HttpClient httpClient) =>
        this._httpClient = httpClient
                          ?? throw new ArgumentNullException(nameof(httpClient));


    public Task<List<Tag>?> GetStreamContentAsync(string streamId, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}