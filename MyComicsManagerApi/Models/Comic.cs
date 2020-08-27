using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Microsoft.AspNetCore.Http;

namespace MyComicsManagerApi.Models
{
    public class Comic
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        public string Title { get; set; }

        public decimal Price { get; set; }

        public string Category { get; set; }

        public string Author { get; set; }

        [BsonIgnore]
        public IFormFile EBook { get; set; }
    }
}