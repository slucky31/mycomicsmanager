using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MyComicsManagerApi.Models
{
    public class Comic
    {
        
        // Technical data
        
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        public string EbookPath { get; set; }

        public string EbookName { get; set; }

        public string LibraryId { get; set; }

        // Book info

        public string Series { get; set; }
        
        public string Title { get; set; }

        public string Vomume { get; set; }

        public string Summary { get; set; }

        public decimal Price { get; set; }

        public string Category { get; set; }

        public DateTime Published { get; set; }
        
        public string Author { get; set; }

        public string Writer { get; set; }

        public string Penciller { get; set; }

        public string Colorist { get; set; }

        public string Dditor { get; set; }

        public string languageISO { get; set; }
        
        public int pageCount { get; set; }

        public int review { get; set; }

    }
}