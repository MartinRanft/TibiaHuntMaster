using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using Microsoft.EntityFrameworkCore;

namespace TibiaHuntMaster.Infrastructure.Data.Entities.Content
{
    [Index(nameof(ContentId), IsUnique = true)]
    [Table("HuntingPlaces")]
    public sealed class HuntingPlaceEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int ContentId { get; set; }

        [MaxLength(256)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(256)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(64)]
        public string TemplateType { get; set; } = "HuntingPlace";

        [MaxLength(128)]
        public string City { get; set; } = string.Empty;

        [MaxLength(128)]
        public string Vocation { get; set; } = string.Empty;

        [MaxLength(512)]
        public string? Image { get; set; }

        [MaxLength(64)]
        public string? ImplementedVersion { get; set; }

        [MaxLength(512)]
        public string? Location { get; set; }

        public string? Summary { get; set; }

        public string? PlainTextContent { get; set; }

        public string? RawWikiText { get; set; }

        public string? StructuredDataJson { get; set; }

        [MaxLength(512)]
        public string? Map { get; set; }

        [MaxLength(512)]
        public string? Map2 { get; set; }

        [MaxLength(512)]
        public string? Map3 { get; set; }

        [MaxLength(512)]
        public string? Map4 { get; set; }

        public int? MapWidth { get; set; }

        public int? Map2Width { get; set; }

        public int? Experience { get; set; }

        [MaxLength(16)]
        public string? ExperienceStar { get; set; }

        public int? LootValue { get; set; }

        [MaxLength(16)]
        public string? LootStar { get; set; }

        [MaxLength(256)]
        public string? BestLoot { get; set; }

        [MaxLength(256)]
        public string? BestLoot2 { get; set; }

        [MaxLength(256)]
        public string? BestLoot3 { get; set; }

        [MaxLength(256)]
        public string? BestLoot4 { get; set; }

        [MaxLength(256)]
        public string? BestLoot5 { get; set; }

        public int? LevelMages { get; set; }

        public int? LevelKnights { get; set; }

        public int? LevelPaladins { get; set; }

        public int? SkillMages { get; set; }

        public int? SkillKnights { get; set; }

        public int? SkillPaladins { get; set; }

        public int? DefenseMages { get; set; }

        public int? DefenseKnights { get; set; }

        public int? DefensePaladins { get; set; }

        public string CreaturesJson { get; set; } = "[]";

        public string CategoriesJson { get; set; } = "[]";

        [MaxLength(512)]
        public string? WikiUrl { get; set; }

        public DateTimeOffset? LastSeenAt { get; set; }

        public DateTimeOffset SourceLastUpdatedAt { get; set; }

        [MaxLength(128)]
        public string? ContentHash { get; set; }

        public string? SourceJson { get; set; }

        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

        public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

        public List<HuntingPlaceLevelEntity> LowerLevels { get; set; } = [];

        public List<HuntingPlaceCreatureEntity> Creatures { get; set; } = [];
    }

    [Table("HuntingPlaceLevels")]
    public sealed class HuntingPlaceLevelEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int HuntingPlaceId { get; set; }

        [ForeignKey(nameof(HuntingPlaceId))]
        public HuntingPlaceEntity? HuntingPlace { get; set; }

        [MaxLength(256)]
        public string AreaName { get; set; } = string.Empty;

        public int? LevelMages { get; set; }

        public int? LevelKnights { get; set; }

        public int? LevelPaladins { get; set; }

        public int? SkillMages { get; set; }

        public int? SkillKnights { get; set; }

        public int? SkillPaladins { get; set; }

        public int? DefenseMages { get; set; }

        public int? DefenseKnights { get; set; }

        public int? DefensePaladins { get; set; }
    }

    [Table("HuntingPlaceCreatures")]
    public sealed class HuntingPlaceCreatureEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int HuntingPlaceId { get; set; }

        [ForeignKey(nameof(HuntingPlaceId))]
        public HuntingPlaceEntity HuntingPlace { get; set; } = null!;

        public int? CreatureId { get; set; }

        [ForeignKey(nameof(CreatureId))]
        public CreatureEntity? Creature { get; set; }

        [MaxLength(256)]
        public string CreatureName { get; set; } = string.Empty;
    }
}
