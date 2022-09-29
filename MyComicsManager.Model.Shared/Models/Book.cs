using System.ComponentModel.DataAnnotations;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MyComicsManager.Model.Shared.Models
{
    public class Book
    {
        
        // Technical data
        
        [BsonId]
        [BsonIgnoreIfDefault]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        // Book info

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