using Application.Interfaces;

namespace Application.ImportJobs.Process;

public record ProcessImportJobExternalServices(
    IComicSearchService ComicSearch,
    ICloudinaryService Cloudinary,
    ITempWorkspace TempWorkspace,
    IImportDirectoryStorage ImportStorage,
    IIsbnPageScanner IsbnScanner,
    TimeProvider Clock);
