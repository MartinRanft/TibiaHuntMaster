using System.Text.Json;

using TibiaHuntMaster.Core.Content.HuntingPlaces;
using TibiaHuntMaster.Infrastructure.Data.Mapper.Helpers;

namespace TibiaHuntMaster.Infrastructure.Services.Content.Mapping
{
    internal static class HuntingPlaceContentMapper
    {
        public static void Apply(HuntingPlaceEntity entity, HuntingPlaceDetailsResponse src, string contentHash)
        {
            entity.ContentId = src.Id;
            entity.Name = src.Name;
            entity.Title = src.Title;
            entity.TemplateType = src.StructuredData?.Template ?? "HuntingPlace";
            entity.City = src.City?.Trim() ?? string.Empty;
            entity.Vocation = src.Vocation?.Trim() ?? string.Empty;
            entity.Image = src.Image;
            entity.ImplementedVersion = src.Implemented;
            entity.Location = src.Location;
            entity.Summary = src.Summary;
            entity.PlainTextContent = src.PlainTextContent;
            entity.RawWikiText = src.RawWikiText;
            entity.StructuredDataJson = JsonSerializer.Serialize(src.StructuredData);
            entity.Map = src.Map;
            entity.Map2 = src.Map2;
            entity.Map3 = src.Map3;
            entity.Map4 = src.Map4;
            entity.Experience = WikiValueParser.ParseInt(src.Experience);
            entity.ExperienceStar = src.ExperienceStar;
            entity.LootValue = WikiValueParser.ParseInt(src.Loot);
            entity.LootStar = src.LootStar;
            entity.BestLoot = src.BestLoot;
            entity.BestLoot2 = src.BestLoot2;
            entity.BestLoot3 = src.BestLoot3;
            entity.BestLoot4 = src.BestLoot4;
            entity.BestLoot5 = src.BestLoot5;
            entity.LevelMages = WikiValueParser.ParseInt(src.LevelMages);
            entity.LevelKnights = WikiValueParser.ParseInt(src.LevelKnights);
            entity.LevelPaladins = WikiValueParser.ParseInt(src.LevelPaladins);
            entity.SkillMages = WikiValueParser.ParseInt(src.SkillMages);
            entity.SkillKnights = WikiValueParser.ParseInt(src.SkillKnights);
            entity.SkillPaladins = WikiValueParser.ParseInt(src.SkillPaladins);
            entity.DefenseMages = WikiValueParser.ParseInt(src.DefenseMages);
            entity.DefenseKnights = WikiValueParser.ParseInt(src.DefenseKnights);
            entity.DefensePaladins = WikiValueParser.ParseInt(src.DefensePaladins);
            entity.CreaturesJson = JsonSerializer.Serialize(src.Creatures);
            entity.CategoriesJson = JsonSerializer.Serialize(src.Categories);
            entity.WikiUrl = src.WikiUrl;
            entity.LastSeenAt = src.LastSeenAt;
            entity.SourceLastUpdatedAt = src.LastUpdated;
            entity.SourceJson = JsonSerializer.Serialize(src);
            entity.UpdatedAt = src.LastUpdated;

            ApplyLowerLevels(entity, src.LowerLevels);
            ApplyCreatures(entity, src.Creatures);

            entity.ContentHash = contentHash;
        }

        public static string ComputeContentHash(HuntingPlaceDetailsResponse src)
        {
            var lower = (src.LowerLevels ?? [])
                .Where(x => x is not null && !string.IsNullOrWhiteSpace(x.AreaName))
                .OrderBy(x => x.AreaName, StringComparer.OrdinalIgnoreCase)
                .Select(x => new
                {
                    AreaName = x.AreaName!.Trim(),
                    LevelMages = WikiValueParser.ParseInt(x.LevelMages),
                    LevelKnights = WikiValueParser.ParseInt(x.LevelKnights),
                    LevelPaladins = WikiValueParser.ParseInt(x.LevelPaladins),
                    SkillMages = WikiValueParser.ParseInt(x.SkillMages),
                    SkillKnights = WikiValueParser.ParseInt(x.SkillKnights),
                    SkillPaladins = WikiValueParser.ParseInt(x.SkillPaladins),
                    DefenseMages = WikiValueParser.ParseInt(x.DefenseMages),
                    DefenseKnights = WikiValueParser.ParseInt(x.DefenseKnights),
                    DefensePaladins = WikiValueParser.ParseInt(x.DefensePaladins)
                })
                .ToArray();

            var creatures = (src.Creatures ?? [])
                .Where(x => x is not null && !string.IsNullOrWhiteSpace(x.Name))
                .OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
                .ThenBy(x => x.CreatureId)
                .Select(x => new
                {
                    x.CreatureId,
                    Name = x.Name.Trim()
                })
                .ToArray();

            var categories = (src.Categories ?? [])
                .Where(x => x is not null && !string.IsNullOrWhiteSpace(x.Name))
                .OrderBy(x => x.GroupName, StringComparer.OrdinalIgnoreCase)
                .ThenBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
                .ThenBy(x => x.CategoryId)
                .Select(x => new
                {
                    x.CategoryId,
                    x.Slug,
                    x.Name,
                    x.GroupSlug,
                    x.GroupName
                })
                .ToArray();

            var payload = new
            {
                src.Id,
                src.Name,
                src.Title,
                src.Summary,
                src.PlainTextContent,
                src.RawWikiText,
                src.StructuredData,
                TemplateType = src.StructuredData?.Template ?? "HuntingPlace",
                City = src.City?.Trim() ?? string.Empty,
                Vocation = src.Vocation?.Trim() ?? string.Empty,
                src.Image,
                ImplementedVersion = src.Implemented,
                src.Location,
                src.Map,
                src.Map2,
                src.Map3,
                src.Map4,
                MapWidth = (int?)null,
                Map2Width = (int?)null,
                Experience = WikiValueParser.ParseInt(src.Experience),
                src.ExperienceStar,
                LootValue = WikiValueParser.ParseInt(src.Loot),
                src.LootStar,
                src.BestLoot,
                src.BestLoot2,
                src.BestLoot3,
                src.BestLoot4,
                src.BestLoot5,
                LevelMages = WikiValueParser.ParseInt(src.LevelMages),
                LevelKnights = WikiValueParser.ParseInt(src.LevelKnights),
                LevelPaladins = WikiValueParser.ParseInt(src.LevelPaladins),
                SkillMages = WikiValueParser.ParseInt(src.SkillMages),
                SkillKnights = WikiValueParser.ParseInt(src.SkillKnights),
                SkillPaladins = WikiValueParser.ParseInt(src.SkillPaladins),
                DefenseMages = WikiValueParser.ParseInt(src.DefenseMages),
                DefenseKnights = WikiValueParser.ParseInt(src.DefenseKnights),
                DefensePaladins = WikiValueParser.ParseInt(src.DefensePaladins),
                LowerLevels = lower,
                Creatures = creatures,
                Categories = categories,
                src.WikiUrl,
                src.LastSeenAt,
                src.LastUpdated
            };

            return HuntingPlaceFingerprint.ComputeFromPayload(payload);
        }

        private static void ApplyLowerLevels(HuntingPlaceEntity entity, IReadOnlyList<HuntingPlaceAreaRecommendationResponse> lowerLevels)
        {
            entity.LowerLevels.Clear();

            foreach(HuntingPlaceAreaRecommendationResponse level in lowerLevels)
            {
                if(string.IsNullOrWhiteSpace(level.AreaName))
                {
                    continue;
                }

                entity.LowerLevels.Add(new HuntingPlaceLevelEntity
                {
                    AreaName = level.AreaName.Trim(),
                    LevelMages = WikiValueParser.ParseInt(level.LevelMages),
                    LevelKnights = WikiValueParser.ParseInt(level.LevelKnights),
                    LevelPaladins = WikiValueParser.ParseInt(level.LevelPaladins),
                    SkillMages = WikiValueParser.ParseInt(level.SkillMages),
                    SkillKnights = WikiValueParser.ParseInt(level.SkillKnights),
                    SkillPaladins = WikiValueParser.ParseInt(level.SkillPaladins),
                    DefenseMages = WikiValueParser.ParseInt(level.DefenseMages),
                    DefenseKnights = WikiValueParser.ParseInt(level.DefenseKnights),
                    DefensePaladins = WikiValueParser.ParseInt(level.DefensePaladins)
                });
            }
        }

        private static void ApplyCreatures(HuntingPlaceEntity entity, IReadOnlyList<HuntingPlaceCreatureResponse> creatures)
        {
            entity.Creatures.Clear();

            foreach(HuntingPlaceCreatureResponse creature in creatures)
            {
                if(string.IsNullOrWhiteSpace(creature.Name))
                {
                    continue;
                }

                entity.Creatures.Add(new HuntingPlaceCreatureEntity
                {
                    CreatureId = creature.CreatureId,
                    CreatureName = creature.Name.Trim()
                });
            }
        }
    }
}
