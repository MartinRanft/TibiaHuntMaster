using TibiaHuntMaster.Core.Hunts;

namespace TibiaHuntMaster.Core.Characters
{
    /// <summary>
    ///     Internal character representation used by TibiaHuntMaster.
    ///     Created via mapping from TibiaDataCharacterResponse and persisted via EF.
    /// </summary>
    public sealed class Character
    {
        // --- Keys & identity ---
        public int Id { get; set; } // Local database primary key

        // --- Natural key ---
        public string Name { get; set; } = string.Empty;

        public string World { get; set; } = string.Empty;

        // --- Scalar (profile) ---
        public string Vocation { get; set; } = string.Empty;

        public int Level { get; set; }

        public string GuildName { get; set; } = string.Empty;

        public string Residence { get; set; } = string.Empty;

        public string Title { get; set; } = string.Empty;

        public string AccountStatus { get; set; } = string.Empty;

        public int AchievementPoints { get; set; }

        public string Sex { get; set; } = string.Empty;

        /// <summary>
        ///     Last login time as reported by the API (UTC recommended).
        /// </summary>
        public DateTimeOffset? LastLogin { get; set; }

        /// <summary>
        ///     Timestamp of the last local refresh (e.g. when we imported or saved locally).
        /// </summary>
        public DateTimeOffset LastUpdated { get; set; } = DateTimeOffset.UtcNow;

        // --- Aggregates / children (persisted) ---
        public List<AccountBadge> Badges { get; set; } = new();

        public List<Achievement> Achievements { get; set; } = new();

        public List<House> Houses { get; set; } = new();

        public List<Death> Deaths { get; set; } = new();

        /// <summary>
        ///     Optional 1:1 account info if available.
        /// </summary>
        public AccountInfo? Account { get; set; }

        // --- App-calculated / local only ---
        public SkillSet Skills { get; set; } = new();

        public List<EquipmentSet> EquipmentPresets { get; set; } = new();

        public List<HuntStats> HuntHistory { get; set; } = new();
    }
}