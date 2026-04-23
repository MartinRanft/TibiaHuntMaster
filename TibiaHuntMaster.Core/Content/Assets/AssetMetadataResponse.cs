#nullable enable
using System.Text.Json.Serialization;

namespace TibiaHuntMaster.Core.Content.Assets
{
    public sealed class AssetMetadataResponse
    {
        [JsonPropertyName("assetId")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public int AssetId { get; set; }

        [JsonPropertyName("fileName")]
        public string FileName { get; set; } = string.Empty;

        [JsonPropertyName("mimeType")]
        public string MimeType { get; set; } = string.Empty;

        [JsonPropertyName("size")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public long Size { get; set; }

        [JsonPropertyName("width")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public int? Width { get; set; }

        [JsonPropertyName("height")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public int? Height { get; set; }

        [JsonPropertyName("hash")]
        public string? Hash { get; set; }
    }
}
