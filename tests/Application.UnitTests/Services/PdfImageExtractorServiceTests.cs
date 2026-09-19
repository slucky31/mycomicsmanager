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
            Path.Combine(_tempDir, "dest"),
            TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(FileProcessingError.FileNotFound);
    }

    [Fact]
    public async Task ExtractImagesAsync_Should_ReturnError_WhenPdfIsCorrupt()
    {
        // Arrange
        var corruptPath = Path.Combine(_tempDir, "corrupt.pdf");
        await File.WriteAllBytesAsync(corruptPath, [0x00, 0x01, 0x02, 0x03, 0x04], TestContext.Current.CancellationToken);
        var destDir = Path.Combine(_tempDir, "dest");

        // Act
        var result = await _service.ExtractImagesAsync(corruptPath, destDir, TestContext.Current.CancellationToken);
        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(FileProcessingError.CorruptArchive);
    }

    [Fact]
    public async Task ExtractImagesAsync_Should_ReturnSuccess_WhenValidPdfHasNoImages()
    {
        // Arrange
        var pdfPath = Path.Combine(_tempDir, "empty.pdf");
        await File.WriteAllBytesAsync(pdfPath, CreateMinimalValidPdfBytes(), TestContext.Current.CancellationToken);
        var destDir = Path.Combine(_tempDir, "dest");

        // Act
        var result = await _service.ExtractImagesAsync(pdfPath, destDir, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.ExtractedImagePaths.Should().BeEmpty();
        result.Value.PageCount.Should().Be(0);
    }

    /// <summary>
    /// Creates a minimal valid PDF-1.4 with one page whose content stream draws a single
    /// image XObject (DCTDecode-filtered, i.e. a raw embedded JPEG codestream). Offsets are
    /// computed from the actual written bytes rather than hand-calculated.
    /// </summary>
    private static byte[] CreatePdfWithEmbeddedJpegBytes()
    {
        // A tiny but structurally valid baseline JPEG: SOI, APP0/JFIF, DQT, SOF0 (1x1),
        // DHT, SOS, one scan byte, EOI. PdfPig only needs to walk the content stream's
        // XObject reference; it does not decode the JPEG codestream itself.
        byte[] jpegBytes =
        [
            0xFF, 0xD8, // SOI
            0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46, 0x00, 0x01, 0x01, 0x00, 0x00, 0x01, 0x00, 0x01, 0x00, 0x00, // APP0/JFIF
            0xFF, 0xD9 // EOI
        ];

        using var ms = new MemoryStream();
        var offsets = new long[5];

        void WriteAscii(string s)
        {
            var bytes = System.Text.Encoding.ASCII.GetBytes(s);
            ms.Write(bytes, 0, bytes.Length);
        }

        WriteAscii("%PDF-1.4\n");

        offsets[0] = ms.Position;
        WriteAscii("1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n");

        offsets[1] = ms.Position;
        WriteAscii("2 0 obj\n<< /Type /Pages /Kids [3 0 R] /Count 1 >>\nendobj\n");

        offsets[2] = ms.Position;
        WriteAscii("3 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 10 10] " +
                   "/Resources << /XObject << /Im0 5 0 R >> >> /Contents 4 0 R >>\nendobj\n");

        const string content = "q 10 0 0 10 0 0 cm /Im0 Do Q";
        offsets[3] = ms.Position;
        WriteAscii($"4 0 obj\n<< /Length {content.Length} >>\nstream\n{content}\nendstream\nendobj\n");

        offsets[4] = ms.Position;
        WriteAscii($"5 0 obj\n<< /Type /XObject /Subtype /Image /Width 1 /Height 1 " +
                   $"/ColorSpace /DeviceGray /BitsPerComponent 8 /Filter /DCTDecode /Length {jpegBytes.Length} >>\nstream\n");
        ms.Write(jpegBytes, 0, jpegBytes.Length);
        WriteAscii("\nendstream\nendobj\n");

        var xrefOffset = ms.Position;
        WriteAscii("xref\n0 6\n0000000000 65535 f \n");
        foreach (var offset in offsets)
        {
            WriteAscii($"{offset:D10} 00000 n \n");
        }
        WriteAscii($"trailer\n<< /Size 6 /Root 1 0 R >>\nstartxref\n{xrefOffset}\n%%EOF");

        return ms.ToArray();
    }

    [Fact]
    public async Task ExtractImagesAsync_Should_ExtractEmbeddedJpegImage()
    {
        // Arrange
        var pdfPath = Path.Combine(_tempDir, "with-image.pdf");
        await File.WriteAllBytesAsync(pdfPath, CreatePdfWithEmbeddedJpegBytes(), TestContext.Current.CancellationToken);
        var destDir = Path.Combine(_tempDir, "dest");

        // Act
        var result = await _service.ExtractImagesAsync(pdfPath, destDir, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.PageCount.Should().Be(1);
        result.Value.ExtractedImagePaths.Should().ContainSingle();
        var extractedPath = result.Value.ExtractedImagePaths[0];
        Path.GetFileName(extractedPath).Should().Be("page-001.jpg");
        File.Exists(extractedPath).Should().BeTrue();
    }

    // -------------------------------------------------------
    // DetermineExtension (private pure function, exercised via reflection
    // since PNG signatures aren't exercised by the embedded-JPEG fixture above)
    // -------------------------------------------------------

    [Fact]
    public void DetermineExtension_Should_ReturnJpg_ForJpegSignature()
        => PdfImageExtractorService.DetermineExtension([0xFF, 0xD8, 0xFF, 0xE0]).Should().Be(".jpg");

    [Fact]
    public void DetermineExtension_Should_ReturnPng_ForPngSignature()
        => PdfImageExtractorService.DetermineExtension([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A]).Should().Be(".png");

    [Fact]
    public void DetermineExtension_Should_DefaultToJpg_ForUnknownSignature()
        => PdfImageExtractorService.DetermineExtension([0x00, 0x01, 0x02, 0x03]).Should().Be(".jpg");

    [Fact]
    public void DetermineExtension_Should_DefaultToJpg_ForTooFewBytes()
        => PdfImageExtractorService.DetermineExtension([0xFF]).Should().Be(".jpg");
}
