using System.Net;
using Application.ComicInfoSearch;
using Application.Interfaces;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Domain.Books;
using Domain.Extensions;
using Domain.Primitives;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Persistence.Services;

public class CloudinaryService : ICloudinaryService
{
    private static readonly HashSet<string> s_allowedCoverHosts =
        new(["www.bedetheque.com", "books.google.com", "books.googleusercontent.com", "covers.openlibrary.org", "assets.bubblebd.com"],
            StringComparer.OrdinalIgnoreCase);

    // The covers are stored at most this wide (#1225): they are shown at 480 px at most.
    internal const int MaxCoverWidth = 960;

    // The Admin API lists 500 images and deletes 100 of them per call.
    private const int ListPageSize = 500;
    private const int DeleteBatchSize = 100;

    private readonly Cloudinary _cloudinary;
    private readonly string _cloudName;
    private readonly ILogger<CloudinaryService> _logger;

    public CloudinaryService(IOptions<CloudinarySettings> settings, ILogger<CloudinaryService> logger)
    {
        _logger = logger;
        var config = settings.Value;
        var account = new Account(config.CloudName, config.ApiKey, config.ApiSecret);
        _cloudName = config.CloudName;
        _cloudinary = new Cloudinary(account);
        _cloudinary.Api.Secure = true;
    }

    public async Task<CloudinaryUploadResult> UploadImageFromUrlAsync(
        Uri sourceUrl,
        string folder,
        string publicId,
        CancellationToken cancellationToken = default)
    {
        if (!sourceUrl.IsAllowedHttpsHost(s_allowedCoverHosts) && !IsOwnImage(sourceUrl))
        {
            _logger.LogWarning("SSRF guard blocked Cloudinary upload from {SourceUrl}", sourceUrl);
            return new CloudinaryUploadResult(null, null, false,
                $"URL host '{sourceUrl.Host}' is not in the allowed list.");
        }

        _logger.LogInformation("Uploading image to Cloudinary from {SourceUrl} to folder {Folder}", sourceUrl, folder);

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
        _logger.LogInformation("Uploading image to Cloudinary from file {FilePath} to folder {Folder}", filePath, folder);

        FileStream? stream = null;
        try
        {
            stream = File.OpenRead(filePath);

            var uploadParams = CreateUploadParams(
                new FileDescription(Path.GetFileName(filePath), stream), folder, publicId);

            return await ExecuteUploadAsync(uploadParams, filePath, cancellationToken);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _logger.LogError(ex, "Could not read file for Cloudinary upload: {FilePath}", filePath);
            return new CloudinaryUploadResult(null, null, false, ex.Message);
        }
        finally
        {
            if (stream != null)
            {
                await stream.DisposeAsync();
            }
        }
    }

    public async Task<CloudinaryUploadResult> UploadImageFromStreamAsync(
        Stream imageStream,
        string fileName,
        string folder,
        string publicId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Uploading image to Cloudinary from stream {FileName} to folder {Folder}", fileName, folder);

        var uploadParams = CreateUploadParams(
            new FileDescription(fileName, imageStream), folder, publicId);

        return await ExecuteUploadAsync(uploadParams, fileName, cancellationToken);
    }

    public async Task<Result<IReadOnlyList<CloudinaryImage>>> ListImagesAsync(string folder, CancellationToken cancellationToken = default)
    {
        var images = new List<CloudinaryImage>();
        string? cursor = null;
        try
        {
            do
            {
                var page = await _cloudinary.ListResourcesAsync(new ListResourcesByPrefixParams
                {
                    Prefix = folder + "/",
                    Type = "upload",
                    ResourceType = ResourceType.Image,
                    MaxResults = ListPageSize,
                    NextCursor = cursor
                }, cancellationToken);

                if (page.Error != null)
                {
                    _logger.LogError("Cloudinary images of {Folder} not listed: {Error}", folder, page.Error.Message);
                    return BooksError.CoversNotListed;
                }

                images.AddRange(page.Resources.Select(r => new CloudinaryImage(r.PublicId, r.Bytes)));
                cursor = page.NextCursor;
            }
            while (!string.IsNullOrEmpty(cursor));
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException && !cancellationToken.IsCancellationRequested)
        {
            _logger.LogError(ex, "Cloudinary images of {Folder} not listed", folder);
            return BooksError.CoversNotListed;
        }

        return images;
    }

    public async Task<Result<int>> DeleteImagesAsync(IReadOnlyList<string> publicIds, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(publicIds);

        var deleted = 0;
        try
        {
            foreach (var batch in publicIds.Chunk(DeleteBatchSize))
            {
                var result = await _cloudinary.DeleteResourcesAsync(new DelResParams
                {
                    PublicIds = [.. batch],
                    Type = "upload",
                    ResourceType = ResourceType.Image
                }, cancellationToken);

                if (result.Error != null)
                {
                    _logger.LogError("Cloudinary images not deleted: {Error}", result.Error.Message);
                    return BooksError.CoversNotDeleted;
                }

                deleted += result.Deleted?.Count(d => d.Value == "deleted") ?? 0;
            }
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException && !cancellationToken.IsCancellationRequested)
        {
            _logger.LogError(ex, "Cloudinary images not deleted");
            return BooksError.CoversNotDeleted;
        }

        return deleted;
    }

    public async Task<bool> PingAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _cloudinary.GetUsageAsync(cancellationToken);
            return result.StatusCode == HttpStatusCode.OK;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException && !cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning(ex, "Cloudinary connectivity check failed");
            return false;
        }
    }

    // An image of this Cloudinary account: a cover shared by several books is copied to each of them (#1225).
    private bool IsOwnImage(Uri sourceUrl) =>
        sourceUrl.Scheme == Uri.UriSchemeHttps
        && string.Equals(sourceUrl.Host, "res.cloudinary.com", StringComparison.OrdinalIgnoreCase)
        && sourceUrl.AbsolutePath.StartsWith($"/{_cloudName}/", StringComparison.Ordinal);

    // Cloudinary reduces the image before storing it, and never enlarges a smaller one.
    private static ImageUploadParams CreateUploadParams(FileDescription file, string folder, string publicId) =>
        new()
        {
            File = file,
            Folder = folder,
            PublicId = publicId,
            Overwrite = true,
            UniqueFilename = false,
            Transformation = new Transformation().Width(MaxCoverWidth).Crop("limit")
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
                _logger.LogError("Cloudinary upload failed: {Error}", uploadResult.Error.Message);
                return new CloudinaryUploadResult(null, null, false, uploadResult.Error.Message);
            }

            _logger.LogInformation("Image uploaded successfully to Cloudinary: {Url}", uploadResult.SecureUrl);
            return new CloudinaryUploadResult(uploadResult.SecureUrl, uploadResult.PublicId, true, null);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error uploading to Cloudinary from {Source}", source);
            return new CloudinaryUploadResult(null, null, false, ex.Message);
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogError(ex, "Timeout uploading to Cloudinary from {Source}", source);
            return new CloudinaryUploadResult(null, null, false, "Upload timeout");
        }
    }
}
