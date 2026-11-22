using Application.Interfaces;
using Domain.FeedImports;

namespace Persistence.Repositories;

public class AutoRetryDownloadRepository(ApplicationDbContext context) : IAutoRetryDownloadRepository
{
    public void Add(AutoRetryDownload download) => context.AutoRetryDownloads.Add(download);
}
