using Domain.Primitives;

namespace Persistence.Services;

public interface IPageTextRecognizer
{
    /// <summary>Reads the text printed on a page image (OCR).</summary>
    Task<Result<string>> RecognizeAsync(ReadOnlyMemory<byte> image, CancellationToken ct = default);
}
