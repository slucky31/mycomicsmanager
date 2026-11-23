using Domain.Primitives;

namespace Application.Interfaces;

public record CloudinaryUploadResult(
    Uri? Url,
    string? PublicId,
    bool Success,
    string? Error
);

// An image stored in Cloudinary, with its size in bytes.
public record CloudinaryImage(string PublicId, long Bytes);

public interface ICloudinaryService
{
    Task<CloudinaryUploadResult> UploadImageFromUrlAsync(
        Uri sourceUrl,
        string folder,
        string publicId,
        CancellationToken cancellationToken = default);

    Task<CloudinaryUploadResult> UploadImageFromFileAsync(
        string filePath,
        string folder,
        string publicId,
        CancellationToken cancellationToken = default);

    Task<CloudinaryUploadResult> UploadImageFromStreamAsync(
        Stream imageStream,
        string fileName,
        string folder,
        string publicId,
        CancellationToken cancellationToken = default);

    // Every image whose public id starts with the folder, followed by a slash.
    Task<Result<IReadOnlyList<CloudinaryImage>>> ListImagesAsync(string folder, CancellationToken cancellationToken = default);

    // Deletes the images; the result is the number of images deleted.
    Task<Result<int>> DeleteImagesAsync(IReadOnlyList<string> publicIds, CancellationToken cancellationToken = default);

    Task<bool> PingAsync(CancellationToken cancellationToken = default);
}
