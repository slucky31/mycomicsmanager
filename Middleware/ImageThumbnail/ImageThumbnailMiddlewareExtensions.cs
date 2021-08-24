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
            
            // Lien entre /covers et le directory sur le disque
            //builder.UseStaticFiles(new StaticFileOptions
            //{
            //    FileProvider = new PhysicalFileProvider(coversDirRootPath),
            //    RequestPath = "/covers"
            //});

            return builder.UseMiddleware<ImageThumbnailMiddleware>(options);
        }
    }
}