#nullable enable
using System.Text.Json.Serialization;

namespace TibiaHuntMaster.Core.Content.Books
{
    public sealed class BookPageResponse
    {
        [JsonPropertyName("index")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public int Index { get; set; }

        [JsonPropertyName("text")]
        public string Text { get; set; } = string.Empty;

        [JsonPropertyName("returnPage")]
        public string? ReturnPage { get; set; }

        [JsonPropertyName("bookType")]
        public string? BookType { get; set; }

        [JsonPropertyName("location")]
        public string? Location { get; set; }
    }
}
