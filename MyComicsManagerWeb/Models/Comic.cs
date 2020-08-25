using System.ComponentModel.DataAnnotations;

namespace MyComicsManagerWeb.Models
{
    public class Comic
    {
        public string Id { get; set; }

        [Required]
        [StringLength(10, ErrorMessage = "Title is too long.")]
        public string Title { get; set; }

        public decimal Price { get; set; }

        public string Category { get; set; }

        public string Author { get; set; }
    }
}