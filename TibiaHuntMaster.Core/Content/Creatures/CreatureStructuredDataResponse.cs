#nullable enable
using System.Text.Json.Serialization;

namespace TibiaHuntMaster.Core.Content.Creatures
{
    public sealed class CreatureStructuredDataResponse
    {
        [JsonPropertyName("template")]
        public string? Template { get; set; }

        [JsonPropertyName("infobox")]
        public CreatureInfoboxResponse? Infobox { get; set; }

        [JsonPropertyName("resistanceSummary")]
        public CreatureResistanceSummaryResponse? ResistanceSummary { get; set; }

        [JsonPropertyName("combatProperties")]
        public CreatureCombatPropertiesResponse? CombatProperties { get; set; }
    }
}
