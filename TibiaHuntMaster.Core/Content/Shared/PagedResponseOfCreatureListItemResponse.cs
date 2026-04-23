#nullable enable
using System.Text.Json.Serialization;

using TibiaHuntMaster.Core.Content.Creatures;

namespace TibiaHuntMaster.Core.Content.Shared
{
    public sealed class PagedResponseOfCreatureListItemResponse
    {
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
        public List<CreatureListItemResponse> Creatures { get; set; } = [];
    }
}
