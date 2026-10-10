using Application.Books.IsbnScan;
using Domain.Errors;
using Microsoft.Extensions.Options;
using Persistence.Services;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Application.UnitTests.Services;

// Tesseract is replaced by shell scripts, so these tests check how the command line is driven.
public sealed class TesseractPageTextRecognizerTests : IDisposable
{
    private readonly string _workDir;

    public TesseractPageTextRecognizerTests()
    {
        Assert.SkipWhen(OperatingSystem.IsWindows(), "The fake Tesseract commands are shell scripts.");
        _workDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_workDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_workDir))
        {
            Directory.Delete(_workDir, true);
        }
        GC.SuppressFinalize(this);
    }

    private string CreateFakeTesseract(string body)
    {
        var path = Path.Combine(_workDir, "tesseract");
        File.WriteAllText(path, "#!/bin/sh\n" + body + "\n");
        if (!OperatingSystem.IsWindows())
        {
            File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        }
        return path;
    }

    private static TesseractPageTextRecognizer CreateRecognizer(string tesseractPath, int timeoutSeconds = 30) =>
        new(Options.Create(new IsbnOcrSettings { TesseractPath = tesseractPath, PageTimeoutSeconds = timeoutSeconds }),
            NullLogger<TesseractPageTextRecognizer>.Instance);

    private static byte[] CreatePage(int width, int height)
    {
        using var image = new Image<Rgb24>(width, height);
        using var buffer = new MemoryStream();
        image.SaveAsPng(buffer);
        return buffer.ToArray();
    }

    [Fact]
    public async Task RecognizeAsync_Should_ReturnTheTextOfTesseract_WhenItReadsThePageFromItsInput()
    {
        var tesseract = CreateFakeTesseract("cat > /dev/null\necho \"ISBN 978-2-8001-1234-3\"\necho \"args: $*\"");

        var result = await CreateRecognizer(tesseract).RecognizeAsync(CreatePage(100, 150), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Contain("ISBN 978-2-8001-1234-3").And.Contain("args: stdin stdout -l eng");
    }

    [Theory]
    [InlineData(100, 150, 200, 300)]
    [InlineData(100, 3000, 137, 4096)]
    [InlineData(100, 4096, 100, 4096)]
    public async Task RecognizeAsync_Should_EnlargeThePageInGrayscale_UpToTheSizeLimit(int width, int height, int expectedWidth, int expectedHeight)
    {
        var received = Path.Combine(_workDir, "received.png");
        var tesseract = CreateFakeTesseract($"cat > \"{received}\"");

        await CreateRecognizer(tesseract).RecognizeAsync(CreatePage(width, height), TestContext.Current.CancellationToken);

        var info = await Image.IdentifyAsync(received, TestContext.Current.CancellationToken);
        info.Width.Should().Be(expectedWidth);
        info.Height.Should().Be(expectedHeight);
        info.PixelType.BitsPerPixel.Should().Be(8);
    }

    [Fact]
    public async Task RecognizeAsync_Should_ReturnUnavailable_WhenTesseractIsNotInstalled()
    {
        var result = await CreateRecognizer(Path.Combine(_workDir, "missing")).RecognizeAsync(CreatePage(10, 10), TestContext.Current.CancellationToken);

        result.Error.Should().Be(OcrError.Unavailable);
    }

    [Fact]
    public async Task RecognizeAsync_Should_ReturnFailed_WhenTesseractExitsWithAnError()
    {
        var tesseract = CreateFakeTesseract("cat > /dev/null\necho \"Error in pixReadStream\" >&2\nexit 1");

        var result = await CreateRecognizer(tesseract).RecognizeAsync(CreatePage(10, 10), TestContext.Current.CancellationToken);

        result.Error.Should().Be(OcrError.Failed);
    }

    [Fact]
    public async Task RecognizeAsync_Should_ReturnTimeout_WhenTesseractTakesTooLong()
    {
        var tesseract = CreateFakeTesseract("cat > /dev/null\nsleep 10");

        var result = await CreateRecognizer(tesseract, timeoutSeconds: 1).RecognizeAsync(CreatePage(10, 10), TestContext.Current.CancellationToken);

        result.Error.Should().Be(OcrError.Timeout);
    }

    [Fact]
    public async Task RecognizeAsync_Should_ReturnUnreadableImage_WhenThePageIsNotAnImage()
    {
        var tesseract = CreateFakeTesseract("echo never called");

        var result = await CreateRecognizer(tesseract).RecognizeAsync("not an image"u8.ToArray(), TestContext.Current.CancellationToken);

        result.Error.Should().Be(OcrError.UnreadableImage);
    }
}
