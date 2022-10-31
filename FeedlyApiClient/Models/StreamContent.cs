namespace FeedlyApiClient.Models;

    public class StreamContent
    {
        public string id { get; set; }
        public long updated { get; set; }
        public List<Item> items { get; set; }
    }
    public class Alternate
    {
        public string type { get; set; }
        public string href { get; set; }
    }

    public class Category
    {
        public string id { get; set; }
        public string label { get; set; }
    }

    public class Item
    {
        public string fingerprint { get; set; }
        public string language { get; set; }
        public string id { get; set; }
        public List<string> keywords { get; set; }
        public string originId { get; set; }
        public Origin origin { get; set; }
        public string title { get; set; }
        public string author { get; set; }
        public object crawled { get; set; }
        public object published { get; set; }
        public Summary summary { get; set; }
        public List<Alternate> alternate { get; set; }
        public Visual visual { get; set; }
        public string canonicalUrl { get; set; }
        public bool unread { get; set; }
        public int readTime { get; set; }
        public List<Category> categories { get; set; }
        public List<Tag> tags { get; set; }
        public object actionTimestamp { get; set; }
    }

    public class Origin
    {
        public string streamId { get; set; }
        public string title { get; set; }
        public string htmlUrl { get; set; }
    }

public class Summary
    {
        public string content { get; set; }
        public string direction { get; set; }
    }

    public class Tag
    {
        public string id { get; set; }
        public string label { get; set; }
    }

    public class Visual
    {
        public string contentType { get; set; }
        public string processor { get; set; }
        public string url { get; set; }
        public int width { get; set; }
        public int height { get; set; }
        public object expirationDate { get; set; }
        public string edgeCacheUrl { get; set; }
    }