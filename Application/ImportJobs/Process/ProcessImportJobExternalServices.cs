using Application.Books;
using Application.Interfaces;

namespace Application.ImportJobs.Process;

public record ProcessImportJobExternalServices(
    IComicSearchService ComicSearch,
    BookCoverStore Covers,
    ITempWorkspace TempWorkspace,
    IImportDirectoryStorage ImportStorage,
    IIsbnPageScanner IsbnScanner,
    TimeProvider Clock);
