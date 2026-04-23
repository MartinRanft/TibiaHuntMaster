using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace TibiaHuntMaster.Infrastructure.Data.Mapper.Helpers
{
    public static class HuntingPlaceFingerprint
    {
        private static readonly JsonSerializerOptions StableJson = new()
        {
            WriteIndented = false,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        public static string Compute(HuntingPlaceEntity e)
        {
            var lower = (e.LowerLevels ?? new List<HuntingPlaceLevelEntity>())
                        .Where(x => x is not null)
                        .OrderBy(x => x.AreaName, StringComparer.OrdinalIgnoreCase)
                        .Select(x => new
                        {
                            x.AreaName,
                            x.LevelMages,
                            x.LevelKnights,
                            x.LevelPaladins,
                            x.SkillMages,
                            x.SkillKnights,
                            x.SkillPaladins,
                            x.DefenseMages,
                            x.DefenseKnights,
                            x.DefensePaladins
                        })
                        .ToArray();

            var dto = new
            {
                e.Name,
                e.TemplateType,
                e.City,
                e.Vocation,
                e.Image,
                e.ImplementedVersion,
                e.Location,
                e.Map,
                e.Map2,
                e.Map3,
                e.MapWidth,
                e.Map2Width,
                e.Experience,
                e.ExperienceStar,
                e.LootValue,
                e.LootStar,
                e.BestLoot,
                e.BestLoot2,
                e.BestLoot3,
                e.BestLoot4,
                e.BestLoot5,
                e.LevelMages,
                e.LevelKnights,
                e.LevelPaladins,
                e.SkillMages,
                e.SkillKnights,
                e.SkillPaladins,
                e.DefenseMages,
                e.DefenseKnights,
                e.DefensePaladins,
                LowerLevels = lower
            };

            return ComputeFromPayload(dto);
        }

        public static string ComputeFromPayload(object payload)
        {
            string json = JsonSerializer.Serialize(payload, StableJson);
            using SHA256 sha = SHA256.Create();
            byte[] hash = sha.ComputeHash(Encoding.UTF8.GetBytes(json));
            return Convert.ToHexString(hash);
        }
    }
}
