namespace TibiaHuntMaster.Infrastructure.Data.Entities.Hunts
{
    public sealed class HuntMonsterEntry
    {
        public int Id { get; set; }

        public int HuntSessionId { get; set; }

        public string MonsterName { get; set; } = string.Empty;

        public int Amount { get; set; }
    }
}