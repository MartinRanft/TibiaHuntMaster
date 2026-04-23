namespace TibiaHuntMaster.Infrastructure.Data.Entities.Hunts
{
    public sealed class HuntGroupEntity
    {
        public int Id { get; set; }

        public string Name { get; set; } = "Merged Session";

        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

        // Optional: Verknüpfung zu einem Ziel (Goal)
        public int? CharacterGoalId { get; set; }
        // public CharacterGoalEntity? Goal { get; set; } // Navigation wenn nötig

        // Die Sessions in dieser Gruppe
        public List<HuntSessionEntity> Sessions { get; set; } = new();
    }
}