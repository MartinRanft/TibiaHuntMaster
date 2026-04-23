using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TibiaHuntMaster.Infrastructure.Data.Entities.Content
{
    [Table("MonsterSpawnCoordinates")]
    public sealed class MonsterSpawnCoordinateEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int X { get; set; }

        public int Y { get; set; }

        public byte Z { get; set; }

        public List<MonsterSpawnCreatureLinkEntity> CreatureLinks { get; set; } = [];
    }

    [Table("MonsterSpawnCreatureLinks")]
    public sealed class MonsterSpawnCreatureLinkEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int MonsterSpawnCoordinateId { get; set; }

        [ForeignKey(nameof(MonsterSpawnCoordinateId))]
        public MonsterSpawnCoordinateEntity Coordinate { get; set; } = null!;

        public int? CreatureId { get; set; }

        [ForeignKey(nameof(CreatureId))]
        public CreatureEntity? Creature { get; set; }

        [MaxLength(256)]
        public string MonsterName { get; set; } = string.Empty;

        public int? SpawnTimeSeconds { get; set; }

        public byte? Direction { get; set; }
    }
}
