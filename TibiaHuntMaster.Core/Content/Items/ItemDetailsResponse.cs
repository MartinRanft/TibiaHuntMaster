#nullable enable
using System.Text.Json.Serialization;

namespace TibiaHuntMaster.Core.Content.Items
{
    public sealed class ItemDetailsResponse
    {
        [JsonPropertyName("id")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("actualName")]
        public string? ActualName { get; set; }

        [JsonPropertyName("plural")]
        public string? Plural { get; set; }

        [JsonPropertyName("article")]
        public string? Article { get; set; }

        [JsonPropertyName("implemented")]
        public string? Implemented { get; set; }

        [JsonPropertyName("itemIds")]
        public List<string> ItemIds { get; set; } = [];

        [JsonPropertyName("droppedBy")]
        public List<string> DroppedBy { get; set; } = [];

        [JsonPropertyName("sounds")]
        public List<string> Sounds { get; set; } = [];

        [JsonPropertyName("categorySlug")]
        public string? CategorySlug { get; set; }

        [JsonPropertyName("categoryName")]
        public string? CategoryName { get; set; }

        [JsonPropertyName("templateType")]
        public string? TemplateType { get; set; }

        [JsonPropertyName("objectClass")]
        public string? ObjectClass { get; set; }

        [JsonPropertyName("primaryType")]
        public string? PrimaryType { get; set; }

        [JsonPropertyName("secondaryType")]
        public string? SecondaryType { get; set; }

        [JsonPropertyName("weaponType")]
        public string? WeaponType { get; set; }

        [JsonPropertyName("hands")]
        public string? Hands { get; set; }

        [JsonPropertyName("attack")]
        public string? Attack { get; set; }

        [JsonPropertyName("defense")]
        public string? Defense { get; set; }

        [JsonPropertyName("defenseMod")]
        public string? DefenseMod { get; set; }

        [JsonPropertyName("armor")]
        public string? Armor { get; set; }

        [JsonPropertyName("range")]
        public string? Range { get; set; }

        [JsonPropertyName("levelRequired")]
        public string? LevelRequired { get; set; }

        [JsonPropertyName("imbueSlots")]
        public string? ImbueSlots { get; set; }

        [JsonPropertyName("vocation")]
        public string? Vocation { get; set; }

        [JsonPropertyName("damageType")]
        public string? DamageType { get; set; }

        [JsonPropertyName("damageRange")]
        public string? DamageRange { get; set; }

        [JsonPropertyName("energyAttack")]
        public string? EnergyAttack { get; set; }

        [JsonPropertyName("fireAttack")]
        public string? FireAttack { get; set; }

        [JsonPropertyName("earthAttack")]
        public string? EarthAttack { get; set; }

        [JsonPropertyName("iceAttack")]
        public string? IceAttack { get; set; }

        [JsonPropertyName("deathAttack")]
        public string? DeathAttack { get; set; }

        [JsonPropertyName("holyAttack")]
        public string? HolyAttack { get; set; }

        [JsonPropertyName("stackable")]
        public string? Stackable { get; set; }

        [JsonPropertyName("usable")]
        public string? Usable { get; set; }

        [JsonPropertyName("marketable")]
        public string? Marketable { get; set; }

        [JsonPropertyName("walkable")]
        public string? Walkable { get; set; }

        [JsonPropertyName("npcPrice")]
        public string? NpcPrice { get; set; }

        [JsonPropertyName("npcValue")]
        public string? NpcValue { get; set; }

        [JsonPropertyName("value")]
        public string? ValueValue { get; set; }

        [JsonPropertyName("weight")]
        public string? Weight { get; set; }

        [JsonPropertyName("attrib")]
        public string? Attrib { get; set; }

        [JsonPropertyName("upgradeClass")]
        public string? UpgradeClass { get; set; }

        [JsonPropertyName("wikiUrl")]
        public string? WikiUrl { get; set; }

        [JsonPropertyName("additionalAttributes")]
        public ItemAdditionalAttributesResponse? AdditionalAttributes { get; set; }

        [JsonPropertyName("lastSeenAt")]
        public DateTimeOffset? LastSeenAt { get; set; }

        [JsonPropertyName("lastUpdated")]
        public DateTimeOffset LastUpdated { get; set; }

        [JsonPropertyName("images")]
        public List<ItemImageResponse> Images { get; set; } = [];
    }
}
