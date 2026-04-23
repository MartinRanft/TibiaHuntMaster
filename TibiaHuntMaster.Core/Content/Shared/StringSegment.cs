#nullable enable
using System.Text.Json.Serialization;

namespace TibiaHuntMaster.Core.Content.Shared
{
    public sealed class StringSegment
    {
        [JsonPropertyName("buffer")]
        public string? Buffer { get; set; }

        [JsonPropertyName("offset")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public int Offset { get; set; }

        [JsonPropertyName("length")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public int Length { get; set; }

        [JsonPropertyName("value")]
        public string? ValueValue { get; set; }

        [JsonPropertyName("hasValue")]
        public bool HasValue { get; set; }
    }
}
