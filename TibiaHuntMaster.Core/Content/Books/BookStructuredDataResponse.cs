#nullable enable
using System.Text.Json.Serialization;

namespace TibiaHuntMaster.Core.Content.Books
{
    public sealed class BookStructuredDataResponse
    {
        [JsonPropertyName("template")]
        public string? Template { get; set; }

        [JsonPropertyName("infobox")]
        public BookInfoboxResponse? Infobox { get; set; }

        [JsonPropertyName("pages")]
        public List<BookPageResponse> Pages { get; set; } = [];
    }
}
