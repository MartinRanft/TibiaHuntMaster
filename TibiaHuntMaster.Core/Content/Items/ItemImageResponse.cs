#nullable enable
using System.Text.Json.Serialization;

namespace TibiaHuntMaster.Core.Content.Items
{
    public sealed class ItemImageResponse
    {
        [JsonPropertyName("assetId")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public int AssetId { get; set; }

        [JsonPropertyName("storageKey")]
        public string StorageKey { get; set; } = string.Empty;

        [JsonPropertyName("fileName")]
        public string FileName { get; set; } = string.Empty;

        [JsonPropertyName("mimeType")]
        public string? MimeType { get; set; }

        [JsonPropertyName("width")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public int? Width { get; set; }

        [JsonPropertyName("height")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public int? Height { get; set; }
    }
}
