using Microsoft.AspNetCore.Builder;
using System;
using System.Collections.Generic;
using System.Text;

namespace ImageThumbnail.AspNetCore.Middleware
{
    public static class ImageThumbnailMiddlewareExtensions
    {
        public static IApplicationBuilder UseImageThumbnail(
            this IApplicationBuilder builder,ImageThumbnailOptions options)
        {
            return builder.UseMiddleware<ImageThumbnailMiddleware>(options);
        }
    }
}
