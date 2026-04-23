namespace TibiaHuntMaster.Infrastructure.Data.Entities.Hunts
{
    public sealed class HuntLootEntry
    {
        public int Id { get; set; }

        public int HuntSessionId { get; set; }
        // Kein Navigation Property nötig, Config regelt das über die Session

        public string ItemName { get; set; } = string.Empty;

        public int Amount { get; set; }

        public int AmountKept { get; set; }
    }
}