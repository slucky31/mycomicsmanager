using System;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using ImageThumbnail.AspNetCore.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using MyComicsManagerWeb.Models;
using SixLabors.ImageSharp.Formats.Webp;

namespace MyComicsManagerWeb.Middleware.DownloadEBookFile
{
    public class DownloadEbookFileMiddleWare
    {
        private readonly RequestDelegate _next;

        public DownloadEbookFileMiddleWare(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var isValid = context.Request.Path.StartsWithSegments("/download");

            if (isValid)
            {
                var downloadRequest = ParseRequest(context.Request);

                if (IsSourceExists(downloadRequest))
                {
                    await WriteFromSource(downloadRequest, context.Response.Body).ConfigureAwait(false);
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
        private DownloadRequest ParseRequest(HttpRequest request)
        {
            var req = new DownloadRequest
            {
                RequestedPath = request.Path,
            };
            return req;
        }
        
        private bool IsSourceExists(DownloadRequest request)
        {
            if (File.Exists(request.SourcePath))
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
        
        private async Task WriteFromSource(DownloadRequest request, Stream stream)
        {
            await using var fs = new FileStream(request.SourcePath, FileMode.Open);
            await fs.CopyToAsync(stream).ConfigureAwait(false);
        }
    }
}