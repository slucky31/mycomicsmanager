using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MyComicsManager.Model.Shared
{
    public class Comic
    {

        // Technical data

        [BsonId]
        [BsonIgnoreIfDefault]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [Required]
        public string EbookName { get; set; }

        [Required]
        public string LibraryId { get; set; }

        [Required]
        public string EbookPath { get; set; }

        public string CoverPath { get; set; }

        public CoverType CoverType { get; set; }
        
        public long Size { get; set; }
        
        [Required]
        public ImportStatus ImportStatus { get; set; }

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

        public DateTime Added { get; set; }

        public DateTime Edited { get; set; }

        public string Writer { get; set; }

        public string Penciller { get; set; }

        public string Colorist { get; set; }

        public string Editor { get; set; }

        public string LanguageIso { get; set; }

        public int PageCount { get; set; }

        public double Review { get; set; }

        public bool WebPFormated { get; set; }

        public List<ComicReview> ComicReviews { get; set; }

        public string FicheUrl { get; set; }

        public override string ToString()
        {
            return "Comic : " + Serie?.Replace(Environment.NewLine, "") + " - " + Title?.Replace(Environment.NewLine, "") + " - " + Volume + " - " + EbookPath?.Replace(Environment.NewLine, "");
        }
    }

    public class ComicReview
    {
        public DateTime? Reviewed { get; set; }
        public int Note { get; set; }
    }
    
    public enum CoverType
    {
        PORTRAIT,
        LANDSCAPE_LEFT,
        LANDSCAPE_RIGHT
    }
    
    public enum ImportStatus
    {
        CREATED = 0,
        CBZ_CONVERTED = 1,
        MOVED_TO_LIB = 2,
        NB_IMAGES_SET = 3,
        COVER_GENERATED = 4,
        IMPORTED = 200,
        ERROR = 500
    }
}