using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MyComicsManagerWeb.Models
{
    public class Comic
    {

        // Technical data

        public string Id { get; set; }

        [Required]
        public string EbookName { get; set; }

        [Required]
        public string LibraryId { get; set; }
        
        [Required]
        public string EbookPath { get; set; }

        public string CoverPath { get; set; }

        // Book info

        public string Serie { get; set; }

        [Required]
        public string Title { get; set; }

        public string Isbn { get; set; }

        public int Volume { get; set; }

        public string Summary { get; set; }

        public double Price { get; set; }

        public string Category { get; set; }

        public DateTime Published { get; set; }

        public string Writer { get; set; }

        public string Penciller { get; set; }

        public string Colorist { get; set; }

        public string Editor { get; set; }

        public string LanguageIso { get; set; }
        
        public int PageCount { get; set; }

        public double Review { get; set; }
        
        public List<ComicReview> ComicReviews { get; set; }

        public string FicheUrl { get; set; }

    }

    public class ComicReview
    {
        public DateTime? Reviewed { get; set; }
        public int Note { get; set; }
    }
}