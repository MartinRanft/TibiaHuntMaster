#nullable enable
using System.Text.Json.Serialization;

using TibiaHuntMaster.Core.Content.Items;

namespace TibiaHuntMaster.Core.Content.Shared
{
    public sealed class PagedResponseOfItemListItemResponse
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
        public List<ItemListItemResponse> Items { get; set; } = [];
    }
}
