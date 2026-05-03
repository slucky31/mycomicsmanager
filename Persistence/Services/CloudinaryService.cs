using Application.ComicInfoSearch;
using Application.Interfaces;
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
        Log.Information("Uploading image to Cloudinary from {SourceUrl} to folder {Folder}", sourceUrl, folder);

        var uploadParams = CreateUploadParams(
            new FileDescription(sourceUrl.ToString()), folder, publicId);

        return await ExecuteUploadAsync(uploadParams, sourceUrl.ToString(), cancellationToken);
    }

    public async Task<CloudinaryUploadResult> UploadImageFromFileAsync(
        string filePath,
        string folder,
        string publicId,
        CancellationToken cancellationToken = default)
    {
        Log.Information("Uploading image to Cloudinary from file {FilePath} to folder {Folder}", filePath, folder);

        await using var stream = File.OpenRead(filePath);
        var uploadParams = CreateUploadParams(
            new FileDescription(Path.GetFileName(filePath), stream), folder, publicId);

        return await ExecuteUploadAsync(uploadParams, filePath, cancellationToken);
    }

    public async Task<CloudinaryUploadResult> UploadImageFromStreamAsync(
        Stream imageStream,
        string fileName,
        string folder,
        string publicId,
        CancellationToken cancellationToken = default)
    {
        Log.Information("Uploading image to Cloudinary from stream {FileName} to folder {Folder}", fileName, folder);

        var uploadParams = CreateUploadParams(
            new FileDescription(fileName, imageStream), folder, publicId);

        return await ExecuteUploadAsync(uploadParams, fileName, cancellationToken);
    }

    private static ImageUploadParams CreateUploadParams(FileDescription file, string folder, string publicId) =>
        new()
        {
            File = file,
            Folder = folder,
            PublicId = publicId,
            Overwrite = true,
            UniqueFilename = false
        };

    private async Task<CloudinaryUploadResult> ExecuteUploadAsync(
        ImageUploadParams uploadParams,
        string source,
        CancellationToken cancellationToken)
    {
        try
        {
            var uploadResult = await _cloudinary.UploadAsync(uploadParams, cancellationToken);

            if (uploadResult.Error != null)
            {
                Log.Error("Cloudinary upload failed: {Error}", uploadResult.Error.Message);
                return new CloudinaryUploadResult(null, null, false, uploadResult.Error.Message);
            }

            Log.Information("Image uploaded successfully to Cloudinary: {Url}", uploadResult.SecureUrl);
            return new CloudinaryUploadResult(uploadResult.SecureUrl, uploadResult.PublicId, true, null);
        }
        catch (HttpRequestException ex)
        {
            Log.Error(ex, "HTTP error uploading to Cloudinary from {Source}", source);
            return new CloudinaryUploadResult(null, null, false, ex.Message);
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            Log.Error(ex, "Timeout uploading to Cloudinary from {Source}", source);
            return new CloudinaryUploadResult(null, null, false, "Upload timeout");
        }
    }
}
