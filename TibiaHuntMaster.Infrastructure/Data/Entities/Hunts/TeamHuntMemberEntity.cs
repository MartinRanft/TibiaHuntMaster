namespace TibiaHuntMaster.Infrastructure.Data.Entities.Hunts
{
    public sealed class TeamHuntMemberEntity
    {
        public int Id { get; set; }

        public int TeamHuntSessionId { get; set; }

        public TeamHuntSessionEntity Session { get; set; } = null!;

        public string Name { get; set; } = string.Empty;

        public bool IsLeader { get; set; }

        public long Loot { get; set; }

        public long Supplies { get; set; }

        public long Balance { get; set; }

        public long Damage { get; set; }

        public long Healing { get; set; }
    }
}