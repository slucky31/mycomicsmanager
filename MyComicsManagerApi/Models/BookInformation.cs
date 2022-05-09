using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace MyComicsManagerApi.Models
{
    
    public class IndustryIdentifier
    {
        [JsonPropertyName("type")] public string Type { get; set; }

        [JsonPropertyName("identifier")] public string Identifier { get; set; }
    }

    public class ReadingModes
    {
        [JsonPropertyName("text")] public bool Text { get; set; }

        [JsonPropertyName("image")] public bool Image { get; set; }
    }

    public class VolumeInfo
    {
        [JsonPropertyName("title")] public string Title { get; set; }

        [JsonPropertyName("authors")] public List<string> Authors { get; set; }

        [JsonPropertyName("publisher")] public string Publisher { get; set; }

        [JsonPropertyName("publishedDate")] public string PublishedDate { get; set; }

        [JsonPropertyName("description")] public string Description { get; set; }

        [JsonPropertyName("industryIdentifiers")]
        public List<IndustryIdentifier> IndustryIdentifiers { get; set; }

        [JsonPropertyName("readingModes")] public ReadingModes ReadingModes { get; set; }

        [JsonPropertyName("pageCount")] public int PageCount { get; set; }

        [JsonPropertyName("printType")] public string PrintType { get; set; }

        [JsonPropertyName("maturityRating")] public string MaturityRating { get; set; }

        [JsonPropertyName("allowAnonLogging")] public bool AllowAnonLogging { get; set; }

        [JsonPropertyName("contentVersion")] public string ContentVersion { get; set; }

        [JsonPropertyName("language")] public string Language { get; set; }

        [JsonPropertyName("previewLink")] public string PreviewLink { get; set; }

        [JsonPropertyName("infoLink")] public string InfoLink { get; set; }

        [JsonPropertyName("canonicalVolumeLink")]
        public string CanonicalVolumeLink { get; set; }
    }

    public class SaleInfo
    {
        [JsonPropertyName("country")] public string Country { get; set; }

        [JsonPropertyName("saleability")] public string Saleability { get; set; }

        [JsonPropertyName("isEbook")] public bool IsEbook { get; set; }
    }

    public class Epub
    {
        [JsonPropertyName("isAvailable")] public bool IsAvailable { get; set; }
    }

    public class Pdf
    {
        [JsonPropertyName("isAvailable")] public bool IsAvailable { get; set; }
    }

    public class AccessInfo
    {
        [JsonPropertyName("country")] public string Country { get; set; }

        [JsonPropertyName("viewability")] public string Viewability { get; set; }

        [JsonPropertyName("embeddable")] public bool Embeddable { get; set; }

        [JsonPropertyName("publicDomain")] public bool PublicDomain { get; set; }

        [JsonPropertyName("textToSpeechPermission")]
        public string TextToSpeechPermission { get; set; }

        [JsonPropertyName("epub")] public Epub Epub { get; set; }

        [JsonPropertyName("pdf")] public Pdf Pdf { get; set; }

        [JsonPropertyName("webReaderLink")] public string WebReaderLink { get; set; }

        [JsonPropertyName("accessViewStatus")] public string AccessViewStatus { get; set; }

        [JsonPropertyName("quoteSharingAllowed")]
        public bool QuoteSharingAllowed { get; set; }
    }

    public class SearchInfo
    {
        [JsonPropertyName("textSnippet")] public string TextSnippet { get; set; }
    }

    public class Item
    {
        [JsonPropertyName("kind")] public string Kind { get; set; }

        [JsonPropertyName("id")] public string Id { get; set; }

        [JsonPropertyName("etag")] public string Etag { get; set; }

        [JsonPropertyName("selfLink")] public string SelfLink { get; set; }

        [JsonPropertyName("volumeInfo")] public VolumeInfo VolumeInfo { get; set; }

        [JsonPropertyName("saleInfo")] public SaleInfo SaleInfo { get; set; }

        [JsonPropertyName("accessInfo")] public AccessInfo AccessInfo { get; set; }

        [JsonPropertyName("searchInfo")] public SearchInfo SearchInfo { get; set; }
    }

    public class BookInformation
    {
        [JsonPropertyName("kind")] public string Kind { get; set; }

        [JsonPropertyName("totalItems")] public int TotalItems { get; set; }

        [JsonPropertyName("items")] public List<Item> Items { get; set; }
    }
}

