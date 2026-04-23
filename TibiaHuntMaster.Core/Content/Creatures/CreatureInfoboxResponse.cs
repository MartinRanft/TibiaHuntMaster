#nullable enable
using System.Text.Json.Serialization;

namespace TibiaHuntMaster.Core.Content.Creatures
{
    public sealed class CreatureInfoboxResponse
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("actualName")]
        public string? ActualName { get; set; }

        [JsonPropertyName("plural")]
        public string? Plural { get; set; }

        [JsonPropertyName("article")]
        public string? Article { get; set; }

        [JsonPropertyName("armor")]
        public string? Armor { get; set; }

        [JsonPropertyName("mitigation")]
        public string? Mitigation { get; set; }

        [JsonPropertyName("summon")]
        public string? Summon { get; set; }

        [JsonPropertyName("convince")]
        public string? Convince { get; set; }

        [JsonPropertyName("illusionable")]
        public string? Illusionable { get; set; }

        [JsonPropertyName("isBoss")]
        public string? IsBoss { get; set; }

        [JsonPropertyName("creatureClass")]
        public string? CreatureClass { get; set; }

        [JsonPropertyName("primaryType")]
        public string? PrimaryType { get; set; }

        [JsonPropertyName("secondaryType")]
        public string? SecondaryType { get; set; }

        [JsonPropertyName("abilities")]
        public string? Abilities { get; set; }

        [JsonPropertyName("maxDamage")]
        public string? MaxDamage { get; set; }

        [JsonPropertyName("pushable")]
        public string? Pushable { get; set; }

        [JsonPropertyName("pushObjects")]
        public string? PushObjects { get; set; }

        [JsonPropertyName("walksAround")]
        public string? WalksAround { get; set; }

        [JsonPropertyName("walksThrough")]
        public string? WalksThrough { get; set; }

        [JsonPropertyName("physicalDamageModifier")]
        public string? PhysicalDamageModifier { get; set; }

        [JsonPropertyName("earthDamageModifier")]
        public string? EarthDamageModifier { get; set; }

        [JsonPropertyName("fireDamageModifier")]
        public string? FireDamageModifier { get; set; }

        [JsonPropertyName("deathDamageModifier")]
        public string? DeathDamageModifier { get; set; }

        [JsonPropertyName("energyDamageModifier")]
        public string? EnergyDamageModifier { get; set; }

        [JsonPropertyName("holyDamageModifier")]
        public string? HolyDamageModifier { get; set; }

        [JsonPropertyName("iceDamageModifier")]
        public string? IceDamageModifier { get; set; }

        [JsonPropertyName("lifeDrainDamageModifier")]
        public string? LifeDrainDamageModifier { get; set; }

        [JsonPropertyName("drownDamageModifier")]
        public string? DrownDamageModifier { get; set; }

        [JsonPropertyName("healingModifier")]
        public string? HealingModifier { get; set; }

        [JsonPropertyName("sounds")]
        public string? Sounds { get; set; }

        [JsonPropertyName("implemented")]
        public string? Implemented { get; set; }

        [JsonPropertyName("raceId")]
        public string? RaceId { get; set; }

        [JsonPropertyName("notes")]
        public string? Notes { get; set; }

        [JsonPropertyName("behaviour")]
        public string? Behaviour { get; set; }

        [JsonPropertyName("runsAt")]
        public string? RunsAt { get; set; }

        [JsonPropertyName("speed")]
        public string? Speed { get; set; }

        [JsonPropertyName("strategy")]
        public string? Strategy { get; set; }

        [JsonPropertyName("location")]
        public string? Location { get; set; }

        [JsonPropertyName("history")]
        public string? History { get; set; }

        [JsonPropertyName("usesSpells")]
        public string? UsesSpells { get; set; }

        [JsonPropertyName("attackType")]
        public string? AttackType { get; set; }

        [JsonPropertyName("spawnType")]
        public string? SpawnType { get; set; }

        [JsonPropertyName("bestiaryClass")]
        public string? BestiaryClass { get; set; }

        [JsonPropertyName("bestiaryDifficulty")]
        public string? BestiaryDifficulty { get; set; }

        [JsonPropertyName("bestiaryOccurrence")]
        public string? BestiaryOccurrence { get; set; }

        [JsonPropertyName("bosstiaryCategory")]
        public string? BosstiaryCategory { get; set; }

        [JsonPropertyName("fields")]
        public Dictionary<string, string>? Fields { get; set; }
    }
}
