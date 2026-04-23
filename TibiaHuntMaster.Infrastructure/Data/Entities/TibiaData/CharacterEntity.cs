namespace TibiaHuntMaster.Infrastructure.Data.Entities.TibiaData
{
// A) Kern
    public sealed class CharacterEntity
    {
        public int Id { get; set; }

        public string Name { get; set; } = "";

        public string World { get; set; } = "";

        public string Vocation { get; set; } = "";

        public int Level { get; set; }

        public string GuildName { get; set; } = "";

        public string Residence { get; set; } = "";

        public string Title { get; set; } = "";

        public string AccountStatus { get; set; } = "";

        public int AchievementPoints { get; set; }

        public string Sex { get; set; } = "";

        public DateTimeOffset? LastLogin { get; set; }

        public DateTimeOffset LastUpdatedUtc { get; set; }

        public CharacterAccountEntity? Account { get; set; }

        public List<CharacterBadgeEntity> Badges { get; set; } = new();

        public List<CharacterAchievementEntity> Achievements { get; set; } = new();

        public List<CharacterHouseEntity> Houses { get; set; } = new();

        public List<CharacterDeathEntity> Deaths { get; set; } = new();

        public List<CharacterSnapshotEntity> Snapshots { get; set; } = new();
    }

// B) Relationen
    public sealed class CharacterAccountEntity
    {
        public int Id { get; set; }

        public int CharacterId { get; set; }

        public CharacterEntity Character { get; set; } = null!;

        public string Created { get; set; } = "";

        public string LoyaltyTitle { get; set; } = "";

        public string Position { get; set; } = "";
    }

    public sealed class CharacterBadgeEntity
    {
        public int Id { get; set; }

        public int CharacterId { get; set; }

        public CharacterEntity Character { get; set; } = null!;

        public string Name { get; set; } = "";

        public string Description { get; set; } = "";

        public string IconUrl { get; set; } = "";
    }

    public sealed class CharacterAchievementEntity
    {
        public int Id { get; set; }

        public int CharacterId { get; set; }

        public CharacterEntity Character { get; set; } = null!;

        public string Name { get; set; } = "";

        public int Grade { get; set; }

        public bool Secret { get; set; }
    }

    public sealed class CharacterHouseEntity
    {
        public int Id { get; set; }

        public int CharacterId { get; set; }

        public CharacterEntity Character { get; set; } = null!;

        public int HouseId { get; set; }

        public string Name { get; set; } = "";

        public string Town { get; set; } = "";

        public string Paid { get; set; } = "";
    }

    public sealed class CharacterDeathEntity
    {
        public int Id { get; set; }

        public int CharacterId { get; set; }

        public CharacterEntity Character { get; set; } = null!;

        public DateTimeOffset TimeUtc { get; set; }

        public int Level { get; set; }

        public string Reason { get; set; } = "";

        public string KillersJson { get; set; } = "[]"; // Assists/Killers als JSON
    }

// C) Snapshot
    public sealed class CharacterSnapshotEntity
    {
        public int Id { get; set; }

        public int CharacterId { get; set; }

        public CharacterEntity Character { get; set; } = null!;

        public DateTimeOffset FetchedAtUtc { get; set; }

        public string RawJson { get; set; } = ""; // kompletter API-Body
    }
}