using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;

namespace ImageThumbnail.AspNetCore.Middleware
{
    public class ImageThumbnailOptions
    {
        public ImageThumbnailOptions(string imagesDirectory, string cacheDirectoryName)
        {
            this.ImagesDirectory = imagesDirectory;
            this.CacheDirectoryName = cacheDirectoryName;
            this.ThumbnailBackground = Color.White;
            this.ImageQuality = 90L;
            this.DefaultSize = new Nullable<Size>(new Size(256, 256));
        }
        /// <summary>
        /// Gets or sets Images directory. 
        /// </summary>
        public string ImagesDirectory { get; set; }

        /// <summary>
        /// Gets or sets thumbnail cache directory. 
        /// </summary>
        public string CacheDirectoryName { get; set; }

        /// <summary>
        /// Background color. Default : White
        /// </summary>
        public Color ThumbnailBackground { get; set; }

        /// <summary>
        /// Thumbnail image quality. Default : 90L
        /// </summary>
        public long ImageQuality { get; set; }

        /// <summary>
        /// Thumbnail image size. Default : 256x256
        /// </summary>
        public Size? DefaultSize { get; set; }
    }
}
