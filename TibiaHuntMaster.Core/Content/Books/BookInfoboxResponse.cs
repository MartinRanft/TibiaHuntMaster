#nullable enable
using System.Text.Json.Serialization;

namespace TibiaHuntMaster.Core.Content.Books
{
    public sealed class BookInfoboxResponse
    {
        [JsonPropertyName("bookType")]
        public string? BookType { get; set; }

        [JsonPropertyName("bookType2")]
        public string? BookType2 { get; set; }

        [JsonPropertyName("title")]
        public string? Title { get; set; }

        [JsonPropertyName("pageName")]
        public string? PageName { get; set; }

        [JsonPropertyName("location")]
        public string? Location { get; set; }

        [JsonPropertyName("blurb")]
        public string? Blurb { get; set; }

        [JsonPropertyName("author")]
        public string? Author { get; set; }

        [JsonPropertyName("returnPage")]
        public string? ReturnPage { get; set; }

        [JsonPropertyName("returnPage2")]
        public string? ReturnPage2 { get; set; }

        [JsonPropertyName("previousBook")]
        public string? PreviousBook { get; set; }

        [JsonPropertyName("nextBook")]
        public string? NextBook { get; set; }

        [JsonPropertyName("relatedPages")]
        public string? RelatedPages { get; set; }

        [JsonPropertyName("text")]
        public string? Text { get; set; }

        [JsonPropertyName("implemented")]
        public string? Implemented { get; set; }

        [JsonPropertyName("fields")]
        public Dictionary<string, string>? Fields { get; set; }
    }
}
