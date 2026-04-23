using System.Globalization;
using System.Text.Json;

using TibiaHuntMaster.Core.Content.Creatures;
using TibiaHuntMaster.Core.Content.Loot;
using TibiaHuntMaster.Infrastructure.Data.Mapper.Helpers;

namespace TibiaHuntMaster.Infrastructure.Services.Content.Mapping
{
    internal static class CreatureContentMapper
    {
        public static void Apply(
            CreatureEntity creature,
            CreatureDetailsResponse src,
            string contentHash,
            LootStatisticDetailsResponse? lootDetails = null)
        {
            CreatureInfoboxResponse? infobox = src.StructuredData?.Infobox;
            CreatureCombatPropertiesResponse? combat = src.StructuredData?.CombatProperties;
            CreatureResistanceSummaryResponse? resistances = src.StructuredData?.ResistanceSummary;

            creature.ContentId = src.Id;
            creature.Name = src.Name;
            creature.ActualName = infobox?.ActualName ?? src.Name;
            creature.Plural = infobox?.Plural ?? string.Empty;
            creature.Article = infobox?.Article;
            creature.TemplateType = src.StructuredData?.Template;
            creature.PrimaryType = infobox?.PrimaryType;
            creature.SecondaryType = infobox?.SecondaryType;
            creature.CreatureClass = infobox?.CreatureClass;
            creature.IsBoss = combat?.IsBoss ?? WikiValueParser.ParseYesNo(infobox?.IsBoss);
            creature.Hp = src.Hitpoints;
            creature.Exp = src.Experience;
            creature.Armor = combat?.Armor ?? WikiValueParser.ParseInt(infobox?.Armor);
            creature.Mitigation = combat?.Mitigation ?? WikiValueParser.ParseDouble(infobox?.Mitigation);
            creature.MaxDmg = combat?.MaxDamage ?? WikiValueParser.ParseInt(infobox?.MaxDamage);
            creature.Abilities = infobox?.Abilities;
            creature.SummonMana = WikiValueParser.ParseInt(infobox?.Summon);
            creature.ConvinceMana = WikiValueParser.ParseInt(infobox?.Convince);
            creature.Illusionable = WikiValueParser.ParseYesNo(infobox?.Illusionable);
            creature.Pushable = combat?.Pushable ?? WikiValueParser.ParseYesNo(infobox?.Pushable);
            creature.PushObjects = combat?.PushObjects ?? WikiValueParser.ParseYesNo(infobox?.PushObjects);
            creature.WalksThrough = infobox?.WalksThrough;
            creature.WalksAround = combat?.WalksAround switch
            {
                true => "yes",
                false => "no",
                null => infobox?.WalksAround
            };
            creature.RunsAt = combat?.RunsAt ?? WikiValueParser.ParseInt(infobox?.RunsAt);
            creature.Speed = combat?.Speed ?? WikiValueParser.ParseInt(infobox?.Speed);
            creature.Behaviour = infobox?.Behaviour;
            creature.AttackType = infobox?.AttackType;
            creature.Location = infobox?.Location;
            creature.Strategy = infobox?.Strategy;
            creature.Notes = infobox?.Notes;
            creature.History = infobox?.History;
            creature.ImplementedVersion = infobox?.Implemented;
            creature.RaceId = infobox?.RaceId;
            creature.UsesSpells = combat?.UsesSpells ?? WikiValueParser.ParseYesNo(infobox?.UsesSpells);
            creature.SpawnType = infobox?.SpawnType;
            creature.BestiaryClass = infobox?.BestiaryClass;
            creature.BestiaryDifficulty = infobox?.BestiaryDifficulty;
            creature.BestiaryOccurrence = infobox?.BestiaryOccurrence;
            creature.BosstiaryCategory = infobox?.BosstiaryCategory;
            creature.StructuredDataJson = JsonSerializer.Serialize(src.StructuredData);
            creature.SourceJson = JsonSerializer.Serialize(src);
            creature.SourceLastUpdatedAt = src.LastUpdated;
            creature.UpdatedAt = src.LastUpdated;

            ApplyDamageModifiers(creature.Damage, infobox, resistances);
            ApplyImages(creature, src.Images);
            ApplySounds(creature, infobox?.Sounds);
            ApplyLoot(creature, lootDetails?.LootStatistics ?? src.LootStatistics);

            creature.ContentHash = contentHash;
        }

        public static string ComputeContentHash(CreatureDetailsResponse src, LootStatisticDetailsResponse? lootDetails = null)
        {
            CreatureInfoboxResponse? infobox = src.StructuredData?.Infobox;
            CreatureCombatPropertiesResponse? combat = src.StructuredData?.CombatProperties;
            CreatureResistanceSummaryResponse? resistances = src.StructuredData?.ResistanceSummary;

            List<string> sounds = ParseSounds(infobox?.Sounds);
            List<(string itemName, int? minAmount, int? maxAmount, string? rarity)> loot = BuildLootEntries(lootDetails?.LootStatistics ?? src.LootStatistics);

            var payload = new
            {
                src.Id,
                src.Name,
                ActualName = infobox?.ActualName ?? src.Name,
                Plural = infobox?.Plural ?? string.Empty,
                Article = infobox?.Article,
                TemplateType = src.StructuredData?.Template,
                PrimaryType = infobox?.PrimaryType,
                SecondaryType = infobox?.SecondaryType,
                CreatureClass = infobox?.CreatureClass,
                IsBoss = combat?.IsBoss ?? WikiValueParser.ParseYesNo(infobox?.IsBoss),
                Hp = src.Hitpoints,
                Exp = src.Experience,
                Armor = combat?.Armor ?? WikiValueParser.ParseInt(infobox?.Armor),
                Mitigation = combat?.Mitigation ?? WikiValueParser.ParseDouble(infobox?.Mitigation),
                MaxDmg = combat?.MaxDamage ?? WikiValueParser.ParseInt(infobox?.MaxDamage),
                Abilities = infobox?.Abilities,
                SummonMana = WikiValueParser.ParseInt(infobox?.Summon),
                ConvinceMana = WikiValueParser.ParseInt(infobox?.Convince),
                SenseInvis = (bool?)null,
                ParaImmune = (bool?)null,
                Illusionable = WikiValueParser.ParseYesNo(infobox?.Illusionable),
                Pushable = combat?.Pushable ?? WikiValueParser.ParseYesNo(infobox?.Pushable),
                PushObjects = combat?.PushObjects ?? WikiValueParser.ParseYesNo(infobox?.PushObjects),
                WalksThrough = infobox?.WalksThrough,
                WalksAround = combat?.WalksAround switch
                {
                    true => "yes",
                    false => "no",
                    null => infobox?.WalksAround
                },
                RunsAt = combat?.RunsAt ?? WikiValueParser.ParseInt(infobox?.RunsAt),
                Speed = combat?.Speed ?? WikiValueParser.ParseInt(infobox?.Speed),
                Behaviour = infobox?.Behaviour,
                AttackType = infobox?.AttackType,
                UsedElements = (string?)null,
                Location = infobox?.Location,
                Strategy = infobox?.Strategy,
                Notes = infobox?.Notes,
                History = infobox?.History,
                ImplementedVersion = infobox?.Implemented,
                RaceId = infobox?.RaceId,
                UsesSpells = combat?.UsesSpells ?? WikiValueParser.ParseYesNo(infobox?.UsesSpells),
                SpawnType = infobox?.SpawnType,
                BestiaryClass = infobox?.BestiaryClass,
                BestiaryDifficulty = infobox?.BestiaryDifficulty,
                BestiaryOccurrence = infobox?.BestiaryOccurrence,
                BosstiaryCategory = infobox?.BosstiaryCategory,
                Damage = new
                {
                    Physical = GetModifier(infobox?.PhysicalDamageModifier, resistances?.PhysicalPercent).factor,
                    Fire = GetModifier(infobox?.FireDamageModifier, resistances?.FirePercent).factor,
                    Ice = GetModifier(infobox?.IceDamageModifier, resistances?.IcePercent).factor,
                    Energy = GetModifier(infobox?.EnergyDamageModifier, resistances?.EnergyPercent).factor,
                    Earth = GetModifier(infobox?.EarthDamageModifier, resistances?.EarthPercent).factor,
                    Holy = GetModifier(infobox?.HolyDamageModifier, resistances?.HolyPercent).factor,
                    Death = GetModifier(infobox?.DeathDamageModifier, resistances?.DeathPercent).factor,
                    HpDrain = GetModifier(infobox?.LifeDrainDamageModifier, resistances?.LifeDrainPercent).factor,
                    Drown = GetModifier(infobox?.DrownDamageModifier, resistances?.DrownPercent).factor,
                    Heal = GetModifier(infobox?.HealingModifier, resistances?.HealingPercent).factor
                },
                Images = src.Images
                    .OrderBy(image => image.AssetId)
                    .ThenBy(image => image.StorageKey, StringComparer.Ordinal)
                    .Select(image => new
                    {
                        image.AssetId,
                        image.StorageKey,
                        image.FileName,
                        image.MimeType,
                        image.Width,
                        image.Height
                    }),
                Loot = loot,
                Sounds = sounds,
                src.LastUpdated
            };

            return CreatureFingerprint.ComputeFromPayload(payload);
        }

        private static void ApplyDamageModifiers(
            CreatureDamageModifiers damage,
            CreatureInfoboxResponse? infobox,
            CreatureResistanceSummaryResponse? resistances)
        {
            (damage.PhysicalFactor, damage.PhysicalRaw) = GetModifier(infobox?.PhysicalDamageModifier, resistances?.PhysicalPercent);
            (damage.FireFactor, damage.FireRaw) = GetModifier(infobox?.FireDamageModifier, resistances?.FirePercent);
            (damage.IceFactor, damage.IceRaw) = GetModifier(infobox?.IceDamageModifier, resistances?.IcePercent);
            (damage.EnergyFactor, damage.EnergyRaw) = GetModifier(infobox?.EnergyDamageModifier, resistances?.EnergyPercent);
            (damage.EarthFactor, damage.EarthRaw) = GetModifier(infobox?.EarthDamageModifier, resistances?.EarthPercent);
            (damage.HolyFactor, damage.HolyRaw) = GetModifier(infobox?.HolyDamageModifier, resistances?.HolyPercent);
            (damage.DeathFactor, damage.DeathRaw) = GetModifier(infobox?.DeathDamageModifier, resistances?.DeathPercent);
            (damage.HpDrainFactor, damage.HpDrainRaw) = GetModifier(infobox?.LifeDrainDamageModifier, resistances?.LifeDrainPercent);
            (damage.DrownFactor, damage.DrownRaw) = GetModifier(infobox?.DrownDamageModifier, resistances?.DrownPercent);
            (damage.HealFactor, damage.HealRaw) = GetModifier(infobox?.HealingModifier, resistances?.HealingPercent);
        }

        private static (decimal? factor, string? raw) GetModifier(string? rawValue, int? percentFallback)
        {
            if(!string.IsNullOrWhiteSpace(rawValue))
            {
                return WikiValueParser.ParsePercentToFactor(rawValue);
            }

            if(percentFallback.HasValue)
            {
                string raw = percentFallback.Value.ToString(CultureInfo.InvariantCulture);
                return WikiValueParser.ParsePercentToFactor(raw);
            }

            return (null, null);
        }

        private static void ApplyImages(CreatureEntity creature, IReadOnlyList<CreatureImageResponse> images)
        {
            creature.ImagesJson = "[]";
            creature.PrimaryAssetId = null;
            creature.PrimaryImageStorageKey = null;
            creature.PrimaryImageFileName = null;
            creature.PrimaryImageMimeType = null;
            creature.ImageUrl = null;

            if(images.Count == 0)
            {
                return;
            }

            CreatureImageResponse primary = images[0];
            creature.PrimaryAssetId = primary.AssetId;
            creature.PrimaryImageStorageKey = primary.StorageKey;
            creature.PrimaryImageFileName = primary.FileName;
            creature.PrimaryImageMimeType = primary.MimeType;
            creature.ImageUrl = primary.StorageKey;
            creature.ImagesJson = JsonSerializer.Serialize(images);
        }

        private static void ApplySounds(CreatureEntity creature, string? soundsRaw)
        {
            creature.Sounds.Clear();

            creature.Sounds = ParseSounds(soundsRaw)
                .Distinct(StringComparer.Ordinal)
                .Select(sound => new CreatureSoundEntity
                {
                    Text = sound
                })
                .ToList();
        }

        private static void ApplyLoot(CreatureEntity creature, IReadOnlyList<LootStatisticEntryResponse> lootEntries)
        {
            creature.Loot.Clear();

            foreach((string itemName, int? minAmount, int? maxAmount, string? rarity) loot in BuildLootEntries(lootEntries))
            {
                creature.Loot.Add(new CreatureLootEntity
                {
                    ItemName = loot.itemName,
                    MinAmount = loot.minAmount,
                    MaxAmount = loot.maxAmount,
                    AmountRaw = null,
                    Rarity = loot.rarity,
                    Chance = null,
                    Raw = null
                });
            }
        }

        private static List<string> ParseSounds(string? soundsRaw)
        {
            if(string.IsNullOrWhiteSpace(soundsRaw))
            {
                return [];
            }

            return soundsRaw
                .Split(['\n', '\r', ';', ','], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(sound => !string.IsNullOrWhiteSpace(sound))
                .ToList();
        }

        private static List<(string itemName, int? minAmount, int? maxAmount, string? rarity)> BuildLootEntries(IReadOnlyList<LootStatisticEntryResponse> lootEntries)
        {
            List<(string itemName, int? minAmount, int? maxAmount, string? rarity)> result = [];

            foreach(LootStatisticEntryResponse loot in lootEntries)
            {
                if(string.IsNullOrWhiteSpace(loot.ItemName))
                {
                    continue;
                }

                (int? min, int? max, _) = WikiValueParser.ParseRange(loot.Raw);
                result.Add((loot.ItemName, min, max, loot.Rarity));
            }

            return result
                .OrderBy(x => x.itemName, StringComparer.Ordinal)
                .ThenBy(x => x.minAmount)
                .ThenBy(x => x.maxAmount)
                .ThenBy(x => x.rarity, StringComparer.Ordinal)
                .ToList();
        }
    }
}
