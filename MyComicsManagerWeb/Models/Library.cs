using System.ComponentModel.DataAnnotations;

namespace MyComicsManagerWeb.Models
{
    public class Library
    {

        public string Id { get; set; }

        [Required]
        public string Name { get; set; }

        [Required]
        [RegularExpression(@"^[a-zA-Z0-9\s]{1,40}$")]
        public string RelPath { get; set; }

        public string TmpPath { get; set; }

    }
}