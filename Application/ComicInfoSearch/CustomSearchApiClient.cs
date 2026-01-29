using Google.Apis.CustomSearchAPI.v1;
using Google.Apis.Services;

namespace Application.ComicInfoSearch;

public class CustomSearchApiClient : ICustomSearchApiClient, IDisposable
{
    private readonly CustomSearchAPIService _service;
    private readonly string _cx;
    private bool _disposed;

    public CustomSearchApiClient(IGoogleSearchSettings settings)
    {
        _service = new CustomSearchAPIService(new BaseClientService.Initializer
        {
            ApiKey = settings.ApiKey
        });
        _cx = settings.Cx;
    }

    public IList<string>? ExecuteSearch(string keyword, int startIndex)
    {
        var listRequest = _service.Cse.List();
        listRequest.Cx = _cx;
        listRequest.Q = keyword;
        listRequest.Start = startIndex;

        var result = listRequest.Execute();
        return result.Items?.Select(item => item.Link).ToList();
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _service.Dispose();
            }
            _disposed = true;
        }
    }
}
