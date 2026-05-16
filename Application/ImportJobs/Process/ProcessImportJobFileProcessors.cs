using Application.Interfaces;

namespace Application.ImportJobs.Process;

public record ProcessImportJobFileProcessors(
    IArchiveExtractor ArchiveExtractor,
    IPdfImageExtractor PdfImageExtractor,
    IImageProcessor ImageProcessor,
    IComicArchiveBuilder ArchiveBuilder,
    IComicInfoXmlService ComicInfoXml);
