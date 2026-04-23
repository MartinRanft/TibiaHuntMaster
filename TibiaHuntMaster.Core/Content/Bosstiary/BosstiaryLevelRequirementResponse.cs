#nullable enable
using System.Text.Json.Serialization;

namespace TibiaHuntMaster.Core.Content.Bosstiary
{
    public sealed class BosstiaryLevelRequirementResponse
    {
        [JsonPropertyName("level")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public int Level { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("killsRequired")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public int KillsRequired { get; set; }

        [JsonPropertyName("pointsAwarded")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public int PointsAwarded { get; set; }
    }
}
