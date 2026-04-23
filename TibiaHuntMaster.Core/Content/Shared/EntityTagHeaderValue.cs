#nullable enable
using System.Text.Json.Serialization;

namespace TibiaHuntMaster.Core.Content.Shared
{
    public sealed class EntityTagHeaderValue
    {
        [JsonPropertyName("tag")]
        public StringSegment Tag { get; set; } = new();

        [JsonPropertyName("isWeak")]
        public bool IsWeak { get; set; }
    }
}
