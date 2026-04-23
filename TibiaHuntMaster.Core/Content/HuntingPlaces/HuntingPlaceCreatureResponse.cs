#nullable enable
using System.Text.Json.Serialization;

namespace TibiaHuntMaster.Core.Content.HuntingPlaces
{
    public sealed class HuntingPlaceCreatureResponse
    {
        [JsonPropertyName("creatureId")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public int? CreatureId { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
    }
}
