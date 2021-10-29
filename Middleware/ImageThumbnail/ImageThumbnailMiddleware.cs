using System;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using ImageThumbnail.AspNetCore.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using MyComicsManagerWeb.Models;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;

namespace MyComicsManagerWeb.Middleware.ImageThumbnail
{
    /// <summary>
    /// Middleware to serve image thumbnails
    /// </summary>
    public class ImageThumbnailMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ImageThumbnailOptions _options;
        private readonly IWebserviceSettings _settings;

        public ImageThumbnailMiddleware(RequestDelegate next, ImageThumbnailOptions options, IWebserviceSettings settings)
        {
            _next = next;
            _options = options;
            _settings = settings;
            CreateThumbnailCacheDir();
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var isValid = context.Request.Path.StartsWithSegments("/" + _options.ImagesDirectory);

            if (isValid)
            {
                var thumbnailRequest = ParseRequest(context.Request);

                if (IsSourceImageExists(thumbnailRequest))
                {
                    if (!thumbnailRequest.ThumbnailSize.HasValue)
                    {
                        //Original image requested
                        await WriteFromSource(thumbnailRequest, context.Response.Body).ConfigureAwait(false);
                    }
                    else if (IsThumbnailExists(thumbnailRequest))
                    {
                        //Thumbnail already exists. Send it from cache.
                        await WriteFromCache(thumbnailRequest, context.Response.Body).ConfigureAwait(false);
                    }
                    else
                    {
                        //Generate, cache and send.
                        await GenerateThumbnail(thumbnailRequest, context.Response.Body).ConfigureAwait(false);
                    }
                }
                else
                {
                    await _next(context);
                }
            }
            else
            {
                // Call the next delegate/middleware in the pipeline
                await _next(context);

            }
        }

        /// <summary>
        /// Parse request details from relative path
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        private ThumbnailRequest ParseRequest(HttpRequest request)
        {
            var req = new ThumbnailRequest
            {
                RequestedPath = request.Path,
                ThumbnailSize = ParseSize(request.Query["size"]),
                SourceImagePath = GetPhysicalPath(request.Path)
            };
            req.ThumbnailImagePath = GenerateThumbnailFilePath(request.Path, req.ThumbnailSize);

            return req;
        }

        /// <summary>
        /// Generates thumbnail image, cache and write to output stream
        /// </summary>
        /// <param name="request"></param>
        /// <param name="stream"></param>
        /// <returns></returns>
        private async Task GenerateThumbnail(ThumbnailRequest request, Stream stream)
        {
            if (File.Exists(request.SourceImagePath))
            {
                
                await using (var webPFileStream = new FileStream(request.ThumbnailImagePath, FileMode.Create))
                {
                    using (var image = await Image.LoadAsync(request.SourceImagePath))
                    {
                        if (request.ThumbnailSize != null)
                        {
                            image.Mutate(x => x.Resize(request.ThumbnailSize.Value.Width, 0));
                            await image.SaveAsync(webPFileStream, new WebpEncoder());
                        }
                    }
                    webPFileStream.Close();
                }

                await using (var fs = new FileStream(request.ThumbnailImagePath, FileMode.Open))
                {
                    await fs.CopyToAsync(stream);
                }

            }
        }

        /// <summary>
        /// Parse input size string. Ex. sizes : 128x128, 120, 1, 512x512
        /// </summary>
        /// <param name="size"></param>
        /// <returns></returns>
        private Size? ParseSize(string size)
        {
            if (_options.DefaultSize != null)
            {
                var parseSize = _options.DefaultSize.Value;

                if (!string.IsNullOrEmpty(size))
                {
                    size = size.ToLower(CultureInfo.InvariantCulture);
                    if (size.Contains("x"))
                    {
                        var parts = size.Split('x');
                        parseSize.Width = int.Parse(parts[0]);
                        parseSize.Height = int.Parse(parts[1]);
                    }
                    else if (size == "full")
                    {
                        return new Nullable<Size>();
                    }
                    else
                    {
                        parseSize.Width = int.Parse(size);
                        parseSize.Height = int.Parse(size);
                    }
                }

                return parseSize;
            }

            return null;
        }

        private bool IsSourceImageExists(ThumbnailRequest request)
        {
            if (File.Exists(request.SourceImagePath))
            {
                return true;
            }

            return false;
        }
        private bool IsThumbnailExists(ThumbnailRequest request)
        {
            if (File.Exists(request.ThumbnailImagePath))
            {
                return true;
            }

            return false;
        }

        private string GetPhysicalPath(string path)
        {
            var provider = new PhysicalFileProvider(Path.Combine(_settings.CoversDirRootPath));
            var fileInfo = provider.GetFileInfo(path);

            return fileInfo.PhysicalPath;
        }

        private string GenerateThumbnailFilePath(string path, Size? size)
        {
            if (!size.HasValue)
            {
                return path;
            }

            var fileName = Path.GetFileNameWithoutExtension(path);
            var ext = ".webp";

            //ex : sample.jpg -> sample_256x256.jpg
            fileName = string.Format("{0}_{1}x{2}{3}", fileName, size.Value.Width, size.Value.Height, ext);

            var provider = new PhysicalFileProvider(Path.Combine(_settings.CoversDirRootPath, _options.ImagesDirectory, _options.CacheDirectoryName));
            var fileInfo = provider.GetFileInfo(fileName);

            return fileInfo.PhysicalPath;
        }
        private void CreateThumbnailCacheDir()
        {
            if (!string.IsNullOrEmpty(_options.CacheDirectoryName))
            {
                Directory.CreateDirectory(Path.Combine(_settings.CoversDirRootPath, _options.ImagesDirectory, _options.CacheDirectoryName));
            }
        }

        private async Task WriteFromCache(ThumbnailRequest request, Stream stream)
        {
            using (var fs = new FileStream(request.ThumbnailImagePath, FileMode.Open))
            {
                await fs.CopyToAsync(stream).ConfigureAwait(false);
            }
        }

        private async Task WriteFromSource(ThumbnailRequest request, Stream stream)
        {
            using (var fs = new FileStream(request.SourceImagePath, FileMode.Open))
            {
                await fs.CopyToAsync(stream).ConfigureAwait(false);
            }
        }
    }
}
