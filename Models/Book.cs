using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MyComicsManagerWeb.Models
{
    public class Book
    {
        public string Id { get; set; }

        public string Serie { get; set; }
        
        public string Title { get; set; }

        [Required]
        public string Isbn { get; set; }

        public int Volume { get; set; }
        
        public DateTime Added { get; set; }
        
        [Range(1, 5, ErrorMessage = "The REview should be between 1 and 5")]
        public int Review { get; set; }

    }
}