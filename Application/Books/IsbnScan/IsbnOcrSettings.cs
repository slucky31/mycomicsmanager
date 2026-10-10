using Application.Helpers;

namespace Application.Books.IsbnScan;

/// <summary>Server-side OCR used to read the ISBN printed on the pages of digital books.</summary>
public class IsbnOcrSettings
{
    public bool Enabled { get; set; } = true;

    // The Tesseract command line, installed in the Docker image.
    public string TesseractPath { get; set; } = "tesseract";

    public string Language { get; set; } = "eng";

    public int PagesFromEachEnd { get; set; } = IsbnScanPages.DefaultPagesFromEachEnd;

    public int PageTimeoutSeconds { get; set; } = 30;
}
