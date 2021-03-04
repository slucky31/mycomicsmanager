using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Threading.Tasks;

namespace ImageThumbnail.AspNetCore.Middleware
{
    /// <summary>
    /// Middleware to serve image thumbnails
    /// </summary>
    public class ImageThumbnailMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ImageThumbnailOptions _options;

        public ImageThumbnailMiddleware(RequestDelegate next, ImageThumbnailOptions options)
        {
            _next = next;
            _options = options;
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
                        await WriteFromSource(thumbnailRequest, context.Response.Body);
                    }
                    else if (IsThumbnailExists(thumbnailRequest) && thumbnailRequest.ThumbnailSize.HasValue)
                    {
                        //Thumbnail already exists. Send it from cache.
                        await WriteFromCache(thumbnailRequest, context.Response.Body);
                    }
                    else
                    {
                        //Generate, cache and send.
                        await GenerateThumbnail(thumbnailRequest, context.Response.Body);
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
            var req = new ThumbnailRequest();
            req.RequestedPath = request.Path;
            req.ThumbnailSize = ParseSize(request.Query["size"]);
            req.SourceImagePath = GetPhysicalPath(request.Path);
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
                Image image = Image.FromFile(request.SourceImagePath);

                System.Drawing.Image thumbnail =
                    new Bitmap(request.ThumbnailSize.Value.Width, request.ThumbnailSize.Value.Height);
                System.Drawing.Graphics graphic =
                             System.Drawing.Graphics.FromImage(thumbnail);

                graphic.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphic.SmoothingMode = SmoothingMode.HighQuality;
                graphic.PixelOffsetMode = PixelOffsetMode.HighQuality;
                graphic.CompositingQuality = CompositingQuality.HighQuality;

                double ratioX = (double)request.ThumbnailSize.Value.Width / (double)image.Width;
                double ratioY = (double)request.ThumbnailSize.Value.Height / (double)image.Height;
                double ratio = ratioX < ratioY ? ratioX : ratioY;

                int newHeight = Convert.ToInt32(image.Height * ratio);
                int newWidth = Convert.ToInt32(image.Width * ratio);

                int posX = Convert.ToInt32((request.ThumbnailSize.Value.Width - (image.Width * ratio)) / 2);
                int posY = Convert.ToInt32((request.ThumbnailSize.Value.Height - (image.Height * ratio)) / 2);

                graphic.Clear(_options.ThumbnailBackground);
                graphic.DrawImage(image, posX, posY, newWidth, newHeight);


                System.Drawing.Imaging.ImageCodecInfo[] info =
                                 ImageCodecInfo.GetImageEncoders();
                EncoderParameters encoderParameters;
                encoderParameters = new EncoderParameters(1);
                encoderParameters.Param[0] = new EncoderParameter(Encoder.Quality,
                                 _options.ImageQuality);


                thumbnail.Save(request.ThumbnailImagePath);
                image.Dispose();

                using (var fs = new FileStream(request.ThumbnailImagePath, FileMode.Open))
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
            var _size = _options.DefaultSize.Value;

            try
            {
                if (!string.IsNullOrEmpty(size))
                {
                    size = size.ToLower();
                    if (size.Contains("x"))
                    {
                        var parts = size.Split('x');
                        _size.Width = int.Parse(parts[0]);
                        _size.Height = int.Parse(parts[1]);
                    }
                    else if (size == "full")
                    {
                        return new Nullable<Size>();
                    }
                    else
                    {
                        _size.Width = int.Parse(size);
                        _size.Height = int.Parse(size);
                    }
                }
            }
            catch (ArgumentException ex)
            {
                throw ex;
            }


            return _size;
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
            var provider = new PhysicalFileProvider(Path.Combine(Directory.GetCurrentDirectory()));
            var fileInfo = provider.GetFileInfo(path);

            return fileInfo.PhysicalPath;
        }

        private string GenerateThumbnailFilePath(string path, Size? size)
        {
            if (!size.HasValue)
                return path;
            var fileName = Path.GetFileNameWithoutExtension(path);
            var ext = Path.GetExtension(path);

            //ex : sample.jpg -> sample_256x256.jpg
            fileName = string.Format("{0}_{1}x{2}{3}", fileName, size.Value.Width, size.Value.Height, ext);

            var provider = new PhysicalFileProvider(Path.Combine(Directory.GetCurrentDirectory(), _options.ImagesDirectory, _options.CacheDirectoryName));
            var fileInfo = provider.GetFileInfo(fileName);

            return fileInfo.PhysicalPath;
        }
        private void CreateThumbnailCacheDir()
        {
            if (!string.IsNullOrEmpty(_options.CacheDirectoryName))
            {
                Directory.CreateDirectory(Path.Combine(Directory.GetCurrentDirectory(), _options.ImagesDirectory, _options.CacheDirectoryName));
            }
        }

        private async Task WriteFromCache(ThumbnailRequest request, Stream stream)
        {
            using (var fs = new FileStream(request.ThumbnailImagePath, FileMode.Open))
            {
                await fs.CopyToAsync(stream);
            }
        }

        private async Task WriteFromSource(ThumbnailRequest request, Stream stream)
        {
            using (var fs = new FileStream(request.SourceImagePath, FileMode.Open))
            {
                await fs.CopyToAsync(stream);
            }
        }
    }
}
