using Domain.Primitives;

namespace Domain.Errors;

public static class OcrError
{
    public static readonly TError Unavailable = new("OCR503", "The OCR engine is not available.");
    public static readonly TError Failed = new("OCR500", "The OCR engine failed to read the page.");
    public static readonly TError Timeout = new("OCR504", "The OCR engine took too long to read the page.");
    public static readonly TError UnreadableImage = new("OCR422", "The page image cannot be read.");
}
