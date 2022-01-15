using System.Collections.Generic;

namespace MyComicsManagerWeb.Models
{
    public class Book
    {
        public string Id { get; set; }

        public string Serie { get; set; }
        
        public string Title { get; set; }

        public string Isbn { get; set; }

        public int Volume { get; set; }
        
        public List<ComicReview> Reviews { get; set; }

    }
}