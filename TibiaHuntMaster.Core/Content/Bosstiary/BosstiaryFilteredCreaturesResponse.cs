#nullable enable
using System.Text.Json.Serialization;

namespace TibiaHuntMaster.Core.Content.Bosstiary
{
    public sealed class BosstiaryFilteredCreaturesResponse
    {
        [JsonPropertyName("categorySlug")]
        public string? CategorySlug { get; set; }

        [JsonPropertyName("totalPoints")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public int? TotalPoints { get; set; }

        [JsonPropertyName("search")]
        public string? Search { get; set; }

        [JsonPropertyName("sortBy")]
        public string? SortBy { get; set; }

        [JsonPropertyName("descending")]
        public bool DescendingValue { get; set; }

        [JsonPropertyName("page")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public int Page { get; set; }

        [JsonPropertyName("pageSize")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public int PageSize { get; set; }

        [JsonPropertyName("totalCount")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public int TotalCount { get; set; }

        [JsonPropertyName("items")]
        public List<BosstiaryCreatureListItemResponse> Items { get; set; } = [];
    }
}
