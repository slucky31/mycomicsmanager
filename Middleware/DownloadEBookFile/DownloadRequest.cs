using Microsoft.AspNetCore.Http;

namespace MyComicsManagerWeb.Middleware.DownloadEBookFile
{
    public class DownloadRequest
    {
        public PathString RequestedPath { get; set; }

        public string SourcePath { get; set; }
    }
}