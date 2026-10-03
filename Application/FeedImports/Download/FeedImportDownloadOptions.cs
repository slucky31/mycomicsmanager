using Application.ImportJobs;
using Microsoft.Extensions.Options;

namespace Application.FeedImports.Download;

public sealed record FeedImportDownloadOptions(
    IOptions<FeedImportSettings> FeedImport,
    IOptions<DebridLinkSettings> DebridLink,
    IOptions<ImportSettings> Import);
