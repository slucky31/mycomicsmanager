using Application.Interfaces;
using Application.Libraries;
using Microsoft.Extensions.Options;

namespace Application.ImportJobs.Process;

public record ProcessImportJobExternalServices(
    IComicSearchService ComicSearch,
    ICloudinaryService Cloudinary,
    ILibraryLocalStorage LibraryStorage,
    IImportDirectoryStorage ImportStorage,
    IOptions<ImportSettings> Settings);
