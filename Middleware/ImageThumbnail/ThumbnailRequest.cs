using Microsoft.AspNetCore.Http;
using SixLabors.ImageSharp;

namespace ImageThumbnail.AspNetCore.Middleware
{
    public class ThumbnailRequest
    {
        public PathString RequestedPath { get; set; }

        public string SourceImagePath { get; set; }

        public string ThumbnailImagePath { get; set; }

        public Size? ThumbnailSize { get; set; }

    }
}
