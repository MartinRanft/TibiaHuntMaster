#nullable enable
using System.Text.Json.Serialization;

namespace TibiaHuntMaster.Core.Content.Search
{
    public sealed class SearchResponse
    {
        [JsonPropertyName("query")]
        public string Query { get; set; } = string.Empty;

        [JsonPropertyName("totalCount")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public int TotalCount { get; set; }

        [JsonPropertyName("items")]
        public List<SearchResultItemResponse> Items { get; set; } = [];
    }
}
