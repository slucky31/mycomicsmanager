using Domain.FeedImports;

namespace Application.Interfaces;

public interface IAutoRetryDownloadRepository
{
    void Add(AutoRetryDownload download);
}
