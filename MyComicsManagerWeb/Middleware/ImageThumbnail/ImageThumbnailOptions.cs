using SixLabors.ImageSharp;

namespace ImageThumbnail.AspNetCore.Middleware
{
    public class ImageThumbnailOptions
    {
        public ImageThumbnailOptions()
        {
            this.ThumbnailBackground = Color.White;
            this.ImageQuality = 90L;
            this.DefaultSize = new Size(256, 256);
        }




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
