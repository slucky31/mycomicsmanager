using Microsoft.AspNetCore.Builder;

namespace MyComicsManagerWeb.Middleware.DownloadEBookFile
{
    public static class DownloadEbookFileMiddlewareExtensions
    {
        public static IApplicationBuilder UseDownloadEbookFile(
            this IApplicationBuilder builder)
        {           
            return builder.UseMiddleware<DownloadEbookFileMiddleWare>();
        }
    }
}