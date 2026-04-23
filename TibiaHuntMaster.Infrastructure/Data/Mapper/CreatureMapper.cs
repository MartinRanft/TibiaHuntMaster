using System.Text.Json;

using TibiaHuntMaster.Core.Content.Creatures;
using TibiaHuntMaster.Core.Content.Loot;
using TibiaHuntMaster.Infrastructure.Data.Mapper.Helpers;

namespace TibiaHuntMaster.Infrastructure.Data.Mapper
{
    public static class CreatureMapper
    {
        public static CreatureEntity Map(CreatureDetailsResponse src, LootStatisticDetailsResponse? lootDetails = null)
        {
            CreatureInfoboxResponse? infobox = src.StructuredData?.Infobox;
            CreatureCombatPropertiesResponse? combat = src.StructuredData?.CombatProperties;
            CreatureResistanceSummaryResponse? resistances = src.StructuredData?.ResistanceSummary;

            CreatureEntity creature = new()
            {
                ContentId = src.Id,
                Name = src.Name,
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
                Illusionable = WikiValueParser.ParseYesNo(infobox?.Illusionable),
                Pushable = combat?.Pushable ?? WikiValueParser.ParseYesNo(infobox?.Pushable),
                PushObjects = combat?.PushObjects ?? WikiValueParser.ParseYesNo(infobox?.PushObjects),
                WalksThrough = infobox?.WalksThrough,
                WalksAround = infobox?.WalksAround,
                RunsAt = combat?.RunsAt ?? WikiValueParser.ParseInt(infobox?.RunsAt),
                Speed = combat?.Speed ?? WikiValueParser.ParseInt(infobox?.Speed),
                Behaviour = infobox?.Behaviour,
                AttackType = infobox?.AttackType,
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
                StructuredDataJson = JsonSerializer.Serialize(src.StructuredData),
                SourceJson = JsonSerializer.Serialize(src),
                LastSeenAt = null,
                UpdatedAt = src.LastUpdated
            };

            ApplyDamageModifiers(creature.Damage, infobox, resistances);
            ApplyImages(creature, src.Images);
            ApplySounds(creature, infobox?.Sounds);
            ApplyLoot(creature, lootDetails?.LootStatistics ?? src.LootStatistics);

            return creature;
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
                string raw = percentFallback.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
                return WikiValueParser.ParsePercentToFactor(raw);
            }

            return (null, null);
        }

        private static void ApplyImages(CreatureEntity creature, IReadOnlyList<CreatureImageResponse> images)
        {
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
            if(string.IsNullOrWhiteSpace(soundsRaw))
            {
                return;
            }

            IEnumerable<string> sounds = soundsRaw
                .Split(['\n', '\r', ';', ','], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(sound => !string.IsNullOrWhiteSpace(sound));

            creature.Sounds = sounds
                .Distinct(StringComparer.Ordinal)
                .Select(sound => new CreatureSoundEntity
                {
                    Text = sound
                })
                .ToList();
        }

        private static void ApplyLoot(CreatureEntity creature, IReadOnlyList<LootStatisticEntryResponse> lootEntries)
        {
            foreach(LootStatisticEntryResponse loot in lootEntries)
            {
                if(string.IsNullOrWhiteSpace(loot.ItemName))
                {
                    continue;
                }

                (int? min, int? max, string rawAmount) = WikiValueParser.ParseRange(loot.Raw);

                creature.Loot.Add(new CreatureLootEntity
                {
                    ItemName = loot.ItemName,
                    MinAmount = min,
                    MaxAmount = max,
                    AmountRaw = string.IsNullOrWhiteSpace(rawAmount) ? loot.Raw : rawAmount,
                    Rarity = loot.Rarity,
                    Chance = loot.Chance,
                    Raw = loot.Raw
                });
            }
        }
    }
}
