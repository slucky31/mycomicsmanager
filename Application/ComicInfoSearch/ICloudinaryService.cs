namespace Application.ComicInfoSearch;

public record CloudinaryUploadResult(
    Uri? Url,
    string? PublicId,
    bool Success,
    string? Error
);

public interface ICloudinaryService
{
    Task<CloudinaryUploadResult> UploadImageFromUrlAsync(
        Uri sourceUrl,
        string folder,
        string publicId,
        CancellationToken cancellationToken = default);
}
