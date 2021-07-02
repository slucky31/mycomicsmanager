using System;
using System.ComponentModel.DataAnnotations;

namespace MyComicsManagerWeb.Models
{
    public class Comic
    {

        // Technical data

        public string Id { get; set; }

        [Required]
        public string EbookPath { get; set; }

        [Required]
        public string EbookName { get; set; }

        [Required]
        public string LibraryId { get; set; }

        public string CoverPath { get; set; }

        // Book info

        public string Serie { get; set; }

        [Required]
        public string Title { get; set; }

        public string ISBN { get; set; }

        public string Volume { get; set; }

        public string Summary { get; set; }

        public decimal Price { get; set; }

        public string Category { get; set; }

        public DateTime Published { get; set; }

        public string Writer { get; set; }

        public string Penciller { get; set; }

        public string Colorist { get; set; }

        public string Editor { get; set; }

        public string LanguageISO { get; set; }
        
        public int PageCount { get; set; }

        public int Review { get; set; }

        public string FicheUrl { get; set; }

    }
}