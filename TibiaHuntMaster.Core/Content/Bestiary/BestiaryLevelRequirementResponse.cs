#nullable enable
using System.Text.Json.Serialization;

namespace TibiaHuntMaster.Core.Content.Bestiary
{
    public sealed class BestiaryLevelRequirementResponse
    {
        [JsonPropertyName("level")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public int Level { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("killsRequired")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public int KillsRequired { get; set; }
    }
}
