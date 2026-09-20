using System.Net;
using Application.ComicInfoSearch;
using Application.Interfaces;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Domain.Extensions;
using Microsoft.Extensions.Options;

namespace Persistence.Services;

public class CloudinaryService : ICloudinaryService
{
    private static Serilog.ILogger Log => Serilog.Log.ForContext<CloudinaryService>();

    private static readonly HashSet<string> s_allowedCoverHosts =
        new(["www.bedetheque.com", "books.google.com", "books.googleusercontent.com", "covers.openlibrary.org"],
            StringComparer.OrdinalIgnoreCase);

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
        if (!sourceUrl.IsAllowedHttpsHost(s_allowedCoverHosts))
        {
            Log.Warning("SSRF guard blocked Cloudinary upload from {SourceUrl}", sourceUrl);
            return new CloudinaryUploadResult(null, null, false,
                $"URL host '{sourceUrl.Host}' is not in the allowed list.");
        }

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
            Log.Error(ex, "Could not read file for Cloudinary upload: {FilePath}", filePath);
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
        Log.Information("Uploading image to Cloudinary from stream {FileName} to folder {Folder}", fileName, folder);

        var uploadParams = CreateUploadParams(
            new FileDescription(fileName, imageStream), folder, publicId);

        return await ExecuteUploadAsync(uploadParams, fileName, cancellationToken);
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
            Log.Warning(ex, "Cloudinary connectivity check failed");
            return false;
        }
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
