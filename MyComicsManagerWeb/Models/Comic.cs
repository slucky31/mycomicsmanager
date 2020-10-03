using System.ComponentModel.DataAnnotations;

namespace MyComicsManagerWeb.Models
{
    public class Comic
    {
        public string Id { get; set; }

        [Required]
        public string Title { get; set; }

        public double Price { get; set; }

        public string Category { get; set; }

        public string Author { get; set; }

        [Required]
        public string EbookPath { get; set; }

        [Required]
        public string LibraryId { get; set; }

    }
}