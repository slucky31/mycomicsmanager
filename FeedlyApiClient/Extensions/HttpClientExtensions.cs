using System.Net.Http.Headers;
using FeedlyApiClient.Constants;

namespace FeedlyApiClient.Extensions;

public static class HttpClientExtensions
{
    // https://josef.codes/dealing-with-access-tokens-in-dotnet/
    public static HttpClient AddHeaders(
        this HttpClient httpClient,
        string host,
        string accessToken)
    {
        var headers = httpClient.DefaultRequestHeaders;
        headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return httpClient;
    }
}