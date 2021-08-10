using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

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
