using Application.ComicInfoSearch;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.Extensions.Options;

namespace Persistence.Services;

public class CloudinaryService : ICloudinaryService
{
    private static Serilog.ILogger Log => Serilog.Log.ForContext<CloudinaryService>();

    private readonly Cloudinary _cloudinary;

    public CloudinaryService(IOptions<CloudinarySettings> settings)
    {
        var config = settings.Value;
        var account = new Account(config.CloudName, config.ApiKey, config.ApiSecret);
        _cloudinary = new Cloudinary(account);
        _cloudinary.Api.Secure = true;
    }

    public async Task<CloudinaryUploadResult> UploadImageFromUrlAsync(
        Uri sourceUrl,
        string folder,
        string publicId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            Log.Information("Uploading image to Cloudinary from {SourceUrl} to folder {Folder}", sourceUrl, folder);

            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription(sourceUrl.ToString()),
                Folder = folder,
                PublicId = publicId,
                Overwrite = true,
                UniqueFilename = false
            };

            var uploadResult = await _cloudinary.UploadAsync(uploadParams, cancellationToken);

            if (uploadResult.Error != null)
            {
                Log.Error("Cloudinary upload failed: {Error}", uploadResult.Error.Message);
                return new CloudinaryUploadResult(
                    Url: null,
                    PublicId: null,
                    Success: false,
                    Error: uploadResult.Error.Message
                );
            }

            Log.Information("Image uploaded successfully to Cloudinary: {Url}", uploadResult.SecureUrl);

            return new CloudinaryUploadResult(
                Url: uploadResult.SecureUrl,
                PublicId: uploadResult.PublicId,
                Success: true,
                Error: null
            );
        }
        catch (HttpRequestException ex)
        {
            Log.Error(ex, "HTTP error uploading to Cloudinary from {SourceUrl}", sourceUrl);
            return new CloudinaryUploadResult(null, null, false, ex.Message);
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            Log.Error(ex, "Timeout uploading to Cloudinary from {SourceUrl}", sourceUrl);
            return new CloudinaryUploadResult(null, null, false, "Upload timeout");
        }
    }
}
