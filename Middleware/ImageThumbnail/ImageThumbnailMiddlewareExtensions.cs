using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.FileProviders;
using MyComicsManagerWeb.Models;

namespace ImageThumbnail.AspNetCore.Middleware
{
    public static class ImageThumbnailMiddlewareExtensions
    {
        public static IApplicationBuilder UseImageThumbnail(
            this IApplicationBuilder builder, string coversDirRootPath, ImageThumbnailOptions options)
        {           
            return builder.UseMiddleware<ImageThumbnailMiddleware>(options);
        }
    }
}