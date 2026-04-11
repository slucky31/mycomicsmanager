using Domain.Primitives;

namespace Domain.Errors;

public static class FileProcessingError
{
    public static readonly TError FileNotFound = new("FP404", "The specified file was not found.");
    public static readonly TError CorruptArchive = new("FP422", "The archive file is corrupt or cannot be read.");
    public static readonly TError EmptyDirectory = new("FP400", "The source directory contains no processable files.");
    public static readonly TError InvalidPath = new("FP401", "The specified path is invalid or does not exist.");
    public static readonly TError ProcessingFailed = new("FP500", "File processing failed.");
    public static readonly TError XmlReadError = new("FP501", "Failed to read the ComicInfo.xml file.");
    public static readonly TError XmlWriteError = new("FP502", "Failed to write the ComicInfo.xml file.");
}
