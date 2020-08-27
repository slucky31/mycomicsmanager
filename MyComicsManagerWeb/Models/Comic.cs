using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace MyComicsManagerWeb.Models
{
    public class Comic
    {
        public string Id { get; set; }

        [Required]
        [StringLength(10, ErrorMessage = "Title is too long.")]
        public string Title { get; set; }

        public double Price { get; set; }

        public string Category { get; set; }

        public string Author { get; set; }

        public IFormFile EBook { get; set; }
    }
}