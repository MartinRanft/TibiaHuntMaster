using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TibiaHuntMaster.Infrastructure.Data.Entities.Content
{
    [Table("MonsterImageAssets")]
    public sealed class MonsterImageAssetEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [MaxLength(256)]
        public string CanonicalSlug { get; set; } = string.Empty;

        [MaxLength(512)]
        public string AssetUri { get; set; } = string.Empty;

        [MaxLength(128)]
        public string ContentHash { get; set; } = string.Empty;

        public long FileSizeBytes { get; set; }

        public int? RemoteAssetId { get; set; }

        [MaxLength(512)]
        public string StorageKey { get; set; } = string.Empty;

        [MaxLength(256)]
        public string FileName { get; set; } = string.Empty;

        [MaxLength(128)]
        public string MimeType { get; set; } = string.Empty;

        public int? Width { get; set; }

        public int? Height { get; set; }

        public List<MonsterImageAliasEntity> Aliases { get; set; } = [];

        public List<CreatureMonsterImageLinkEntity> CreatureLinks { get; set; } = [];
    }

    [Table("MonsterImageAliases")]
    public sealed class MonsterImageAliasEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [MaxLength(256)]
        public string Slug { get; set; } = string.Empty;

        public int MonsterImageAssetId { get; set; }

        [ForeignKey(nameof(MonsterImageAssetId))]
        public MonsterImageAssetEntity MonsterImageAsset { get; set; } = null!;
    }

    [Table("CreatureMonsterImageLinks")]
    public sealed class CreatureMonsterImageLinkEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int CreatureId { get; set; }

        [ForeignKey(nameof(CreatureId))]
        public CreatureEntity Creature { get; set; } = null!;

        public int MonsterImageAssetId { get; set; }

        [ForeignKey(nameof(MonsterImageAssetId))]
        public MonsterImageAssetEntity MonsterImageAsset { get; set; } = null!;

        [MaxLength(256)]
        public string? MatchedBySlug { get; set; }
    }
}
