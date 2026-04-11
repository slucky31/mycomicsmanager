using Domain.Errors;
using Persistence.Services;

namespace Application.UnitTests.Services;

public sealed class PdfImageExtractorServiceTests : IDisposable
{
    private readonly PdfImageExtractorService _service = new();
    private readonly string _tempDir;

    public PdfImageExtractorServiceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, true);
        }
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Creates a minimal valid PDF-1.4 with one empty page and no embedded images.
    /// Byte offsets have been manually verified.
    /// </summary>
    private static byte[] CreateMinimalValidPdfBytes()
    {
        // Offsets (each object follows the previous without blank lines):
        //   Obj 1 at  9  (%PDF-1.4\n = 9 bytes)
        //   Obj 2 at 58  (obj1 block = 8+34+7 = 49 bytes → 9+49 = 58)
        //   Obj 3 at 115 (obj2 block = 8+42+7 = 57 bytes → 58+57 = 115)
        //   xref  at 182 (obj3 block = 8+52+7 = 67 bytes → 115+67 = 182)
        const string pdf =
            "%PDF-1.4\n" +
            "1 0 obj\n" +
            "<< /Type /Catalog /Pages 2 0 R >>\n" +
            "endobj\n" +
            "2 0 obj\n" +
            "<< /Type /Pages /Kids [3 0 R] /Count 1 >>\n" +
            "endobj\n" +
            "3 0 obj\n" +
            "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 3 3] >>\n" +
            "endobj\n" +
            "xref\n" +
            "0 4\n" +
            "0000000000 65535 f \n" +
            "0000000009 00000 n \n" +
            "0000000058 00000 n \n" +
            "0000000115 00000 n \n" +
            "trailer\n" +
            "<< /Size 4 /Root 1 0 R >>\n" +
            "startxref\n" +
            "182\n" +
            "%%EOF";
        return System.Text.Encoding.ASCII.GetBytes(pdf);
    }

    // -------------------------------------------------------
    // CanHandle
    // -------------------------------------------------------

    [Fact]
    public void CanHandle_Should_ReturnTrue_ForPdfExtension()
        => _service.CanHandle("document.pdf").Should().BeTrue();

    [Fact]
    public void CanHandle_Should_ReturnTrue_ForPdfExtensionUpperCase()
        => _service.CanHandle("document.PDF").Should().BeTrue();

    [Fact]
    public void CanHandle_Should_ReturnFalse_ForCbzExtension()
        => _service.CanHandle("comic.cbz").Should().BeFalse();

    [Fact]
    public void CanHandle_Should_ReturnFalse_ForUnknownExtension()
        => _service.CanHandle("file.docx").Should().BeFalse();

    // -------------------------------------------------------
    // ExtractImagesAsync
    // -------------------------------------------------------

    [Fact]
    public async Task ExtractImagesAsync_Should_ReturnError_WhenFileDoesNotExist()
    {
        // Act
        var result = await _service.ExtractImagesAsync(
            Path.Combine(_tempDir, "nonexistent.pdf"),
            Path.Combine(_tempDir, "dest"));

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(FileProcessingError.FileNotFound);
    }

    [Fact]
    public async Task ExtractImagesAsync_Should_ReturnError_WhenPdfIsCorrupt()
    {
        // Arrange
        var corruptPath = Path.Combine(_tempDir, "corrupt.pdf");
        await File.WriteAllBytesAsync(corruptPath, [0x00, 0x01, 0x02, 0x03, 0x04]);
        var destDir = Path.Combine(_tempDir, "dest");

        // Act
        var result = await _service.ExtractImagesAsync(corruptPath, destDir);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(FileProcessingError.CorruptArchive);
    }

    [Fact]
    public async Task ExtractImagesAsync_Should_ReturnSuccess_WhenValidPdfHasNoImages()
    {
        // Arrange
        var pdfPath = Path.Combine(_tempDir, "empty.pdf");
        await File.WriteAllBytesAsync(pdfPath, CreateMinimalValidPdfBytes());
        var destDir = Path.Combine(_tempDir, "dest");

        // Act
        var result = await _service.ExtractImagesAsync(pdfPath, destDir);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.ExtractedImagePaths.Should().BeEmpty();
        result.Value.PageCount.Should().Be(0);
    }
}
