using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

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

        public string EbookPath { get; set; }

        public string LibraryId { get; set; }

    }
}