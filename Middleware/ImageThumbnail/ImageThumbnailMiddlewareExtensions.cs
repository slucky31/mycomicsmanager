using ImageThumbnail.AspNetCore.Middleware;
using Microsoft.AspNetCore.Builder;

namespace MyComicsManagerWeb.Middleware.ImageThumbnail
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