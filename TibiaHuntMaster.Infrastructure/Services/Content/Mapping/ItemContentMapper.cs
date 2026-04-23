using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

using TibiaHuntMaster.Core.Content.Items;
using TibiaHuntMaster.Infrastructure.Data.Mapper.Helpers;
using TibiaHuntMaster.Infrastructure.Services.Analysis;

namespace TibiaHuntMaster.Infrastructure.Services.Content.Mapping
{
    internal static class ItemContentMapper
    {
        private static readonly JsonSerializerOptions StableJson = new()
        {
            WriteIndented = false,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        public static void Apply(ItemEntity entity, ItemDetailsResponse detail, string contentHash)
        {
            Dictionary<string, string> attributes = ToAttributeDictionary(detail.AdditionalAttributes);
            ItemImageResponse? primaryImage = detail.Images.FirstOrDefault();

            entity.ContentId = detail.Id;
            entity.Name = detail.Name.Trim();
            entity.NormalizedName = NormalizeItemName(detail.Name);
            entity.ActualName = CleanString(detail.ActualName) ?? detail.Name.Trim();
            entity.Plural = CleanString(detail.Plural) ?? string.Empty;
            entity.Article = CleanString(detail.Article) ?? string.Empty;
            entity.Implemented = CleanString(detail.Implemented) ?? string.Empty;
            entity.Icon = primaryImage?.StorageKey ?? string.Empty;
            entity.ItemIdPrimary = ParseNullableInt(detail.ItemIds.FirstOrDefault());
            entity.ItemIdsCsv = JoinCsv(detail.ItemIds);
            entity.CategorySlug = CleanString(detail.CategorySlug) ?? string.Empty;
            entity.CategoryName = CleanString(detail.CategoryName) ?? string.Empty;
            entity.TemplateType = CleanString(detail.TemplateType) ?? string.Empty;
            entity.ObjectClass = CleanString(detail.ObjectClass) ?? string.Empty;
            entity.PrimaryType = CleanString(detail.PrimaryType) ?? string.Empty;
            entity.SecondaryType = CleanString(detail.SecondaryType) ?? string.Empty;
            entity.WeaponType = CleanString(detail.WeaponType) ?? string.Empty;
            entity.Hands = CleanString(detail.Hands) ?? string.Empty;
            entity.LevelRequired = WikiValueParser.ParseInt(detail.LevelRequired);
            entity.Attack = WikiValueParser.ParseInt(detail.Attack);
            entity.Defense = WikiValueParser.ParseInt(detail.Defense);
            entity.DefenseMod = WikiValueParser.ParseInt(detail.DefenseMod);
            entity.Armor = WikiValueParser.ParseInt(detail.Armor);
            entity.Range = WikiValueParser.ParseInt(detail.Range);
            entity.ImbueSlots = WikiValueParser.ParseInt(detail.ImbueSlots);
            entity.VocRequired = CleanString(detail.Vocation) ?? string.Empty;
            entity.DamageType = CleanString(detail.DamageType) ?? string.Empty;
            entity.DamageRange = CleanString(detail.DamageRange) ?? string.Empty;
            entity.ElementAttack = null;
            entity.EnergyAttack = WikiValueParser.ParseInt(detail.EnergyAttack);
            entity.FireAttack = WikiValueParser.ParseInt(detail.FireAttack);
            entity.EarthAttack = WikiValueParser.ParseInt(detail.EarthAttack);
            entity.IceAttack = WikiValueParser.ParseInt(detail.IceAttack);
            entity.DeathAttack = WikiValueParser.ParseInt(detail.DeathAttack);
            entity.HolyAttack = WikiValueParser.ParseInt(detail.HolyAttack);
            entity.ResistSummary = string.Empty;
            entity.Stackable = WikiValueParser.ParseYesNo(detail.Stackable);
            entity.Usable = WikiValueParser.ParseYesNo(detail.Usable);
            entity.Pickupable = WikiValueParser.ParseYesNo(GetAttribute(attributes, "pickupable"));
            entity.Marketable = WikiValueParser.ParseYesNo(detail.Marketable);
            entity.Walkable = WikiValueParser.ParseYesNo(detail.Walkable);
            entity.WeightOz = ParseNullableDecimal(detail.Weight);
            long? npcPrice = ParseNullableLong(detail.NpcPrice);
            long? npcValue = ParseNullableLong(detail.NpcValue);
            long? contentValue = ParseNullableLong(detail.ValueValue);

            entity.NpcPrice = npcPrice;
            entity.NpcValue = npcValue;
            entity.SellTo = GetAttribute(attributes, "sellTo") ?? string.Empty;
            entity.Value = ItemValueResolver.GetEffectiveValue(contentValue, npcValue, npcPrice);
            entity.Attrib = CleanString(detail.Attrib) ?? string.Empty;
            entity.UpgradeClass = CleanString(detail.UpgradeClass) ?? string.Empty;
            entity.DroppedByCsv = JoinCsv(detail.DroppedBy);
            entity.SoundsJson = JsonSerializer.Serialize(detail.Sounds, StableJson);
            entity.Notes = GetAttribute(attributes, "notes") ?? string.Empty;
            entity.FlavorText = GetAttribute(attributes, "flavorText") ?? string.Empty;
            entity.PrimaryAssetId = primaryImage?.AssetId > 0 ? primaryImage.AssetId : null;
            entity.PrimaryImageStorageKey = primaryImage?.StorageKey ?? string.Empty;
            entity.PrimaryImageFileName = primaryImage?.FileName ?? string.Empty;
            entity.PrimaryImageMimeType = primaryImage?.MimeType ?? string.Empty;
            entity.ImagesJson = JsonSerializer.Serialize(detail.Images, StableJson);
            entity.WikiUrl = CleanString(detail.WikiUrl) ?? string.Empty;
            entity.LastSeenAt = detail.LastSeenAt;
            entity.SourceLastUpdatedAt = detail.LastUpdated;
            entity.ExtrasJson = JsonSerializer.Serialize(detail.AdditionalAttributes?.Entries ?? [], StableJson);
            entity.SourceJson = JsonSerializer.Serialize(detail, StableJson);
            entity.ContentHash = contentHash;
            entity.UpdatedAtUtc = DateTimeOffset.UtcNow;
        }

        public static string ComputeContentHash(ItemDetailsResponse detail)
        {
            var payload = new
            {
                detail.Id,
                detail.Name,
                detail.ActualName,
                detail.Plural,
                detail.Article,
                detail.Implemented,
                detail.ItemIds,
                detail.DroppedBy,
                detail.Sounds,
                detail.CategorySlug,
                detail.CategoryName,
                detail.TemplateType,
                detail.ObjectClass,
                detail.PrimaryType,
                detail.SecondaryType,
                detail.WeaponType,
                detail.Hands,
                detail.Attack,
                detail.Defense,
                detail.DefenseMod,
                detail.Armor,
                detail.Range,
                detail.LevelRequired,
                detail.ImbueSlots,
                detail.Vocation,
                detail.DamageType,
                detail.DamageRange,
                detail.EnergyAttack,
                detail.FireAttack,
                detail.EarthAttack,
                detail.IceAttack,
                detail.DeathAttack,
                detail.HolyAttack,
                detail.Stackable,
                detail.Usable,
                detail.Marketable,
                detail.Walkable,
                detail.NpcPrice,
                detail.NpcValue,
                Value = detail.ValueValue,
                detail.Weight,
                detail.Attrib,
                detail.UpgradeClass,
                detail.WikiUrl,
                detail.LastUpdated,
                AdditionalAttributes = detail.AdditionalAttributes?.Entries
                    .OrderBy(entry => entry.Key, StringComparer.OrdinalIgnoreCase)
                    .Select(entry => new
                    {
                        entry.Key,
                        Value = entry.ValueValue
                    }),
                Images = detail.Images.Select(image => new
                {
                    image.AssetId,
                    image.StorageKey,
                    image.FileName,
                    image.MimeType,
                    image.Width,
                    image.Height
                })
            };

            string json = JsonSerializer.Serialize(payload, StableJson);
            using SHA256 sha = SHA256.Create();
            byte[] bytes = Encoding.UTF8.GetBytes(json);
            byte[] hash = sha.ComputeHash(bytes);
            return Convert.ToHexString(hash);
        }

        private static Dictionary<string, string> ToAttributeDictionary(ItemAdditionalAttributesResponse? attributes)
        {
            Dictionary<string, string> result = new(StringComparer.OrdinalIgnoreCase);

            if(attributes?.Entries is null)
            {
                return result;
            }

            foreach(ItemAttributeEntryResponse entry in attributes.Entries)
            {
                if(string.IsNullOrWhiteSpace(entry.Key))
                {
                    continue;
                }

                result[entry.Key.Trim()] = entry.ValueValue?.Trim() ?? string.Empty;
            }

            return result;
        }

        private static string? GetAttribute(IReadOnlyDictionary<string, string> attributes, string key)
        {
            return attributes.TryGetValue(key, out string? value) ? CleanString(value) : null;
        }

        private static string NormalizeItemName(string? name)
        {
            return name?.Trim().ToUpperInvariant() ?? string.Empty;
        }

        private static string JoinCsv(IEnumerable<string> values)
        {
            return string.Join(",", values.Where(value => !string.IsNullOrWhiteSpace(value)).Select(value => value.Trim()));
        }

        private static string? CleanString(string? value)
        {
            if(string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            string trimmed = value.Trim();
            return trimmed is "?" or "--" ? null : trimmed;
        }

        private static int? ParseNullableInt(string? value)
        {
            return WikiValueParser.ParseInt(CleanString(value));
        }

        private static long? ParseNullableLong(string? value)
        {
            string? cleaned = CleanString(value);
            if(cleaned is null)
            {
                return null;
            }

            string digits = new(cleaned.Where(ch => char.IsDigit(ch) || ch == '-').ToArray());
            if(string.IsNullOrWhiteSpace(digits))
            {
                return null;
            }

            return long.TryParse(digits, NumberStyles.Integer, CultureInfo.InvariantCulture, out long parsed)
                ? parsed
                : null;
        }

        private static decimal? ParseNullableDecimal(string? value)
        {
            string? cleaned = CleanString(value);
            if(cleaned is null)
            {
                return null;
            }

            string normalized = cleaned.Replace(",", "", StringComparison.Ordinal);
            return decimal.TryParse(normalized, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal parsed)
                ? parsed
                : null;
        }
    }
}
