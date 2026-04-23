namespace TibiaHuntMaster.Core.Hunts
{
    /// <summary>
    ///     Aggregated analysis of a hunt including raw values (XP, loot, supplies, damage, etc.),
    ///     derived per-hour metrics, and small leaderboards (top drops and top creatures).
    /// </summary>
    public sealed class HuntStats
    {
        // --- Identity & metadata ---
        public string Id { get; set; } = Guid.NewGuid().ToString();

        public string CharacterName { get; set; } = string.Empty; // Optional for UI filtering

        public string World { get; set; } = string.Empty; // Optional for marketplace context

        public DateTimeOffset Start { get; set; }

        public DateTimeOffset End { get; set; }

        /// <summary>Duration of the hunt (End - Start, recomputed when needed).</summary>
        public TimeSpan Duration { get; set; }

        // --- Raw totals ---
        public long XpGain { get; set; }

        public long Loot { get; set; } // Estimated gold value of looted items

        public long Supplies { get; set; } // Spending on consumables, ammunition, imbuements, etc.

        public long Balance { get; set; } // Loot minus supplies

        public long Damage { get; set; }

        public long Healing { get; set; }

        // --- Derived per-hour metrics ---
        public double XpPerHour { get; private set; }

        public double ProfitPerHour { get; private set; } // Balance divided by hunt duration

        public double DamagePerHour { get; private set; }

        public double HealingPerHour { get; private set; }

        // --- Leaderboards / details ---
        public List<TopDrop> TopDrops { get; set; } = new();

        public List<TopMob> TopMobs { get; set; } = new();

        // --- Quality / provenance (optional) ---
        public string Source { get; set; } = "manual"; // e.g. "manual", "tibiapal", "clipboard"

        public string? RawReference { get; set; } // e.g. path or hash of the original log

        /// <summary>
        ///     Recomputes the derived per-hour metrics based on duration and raw values.
        /// </summary>
        public void RecomputeDerived()
        {
            if(Duration <= TimeSpan.Zero && End > Start)
            {
                Duration = End - Start;
            }

            double hours = Math.Max(Duration.TotalHours, 1e-9); // Guard against division by zero

            XpPerHour = XpGain / hours;
            ProfitPerHour = Balance / hours;
            DamagePerHour = Damage / hours;
            HealingPerHour = Healing / hours;
        }

        /// <summary>
        ///     Convenience helper to set the time window and recompute derived metrics.
        /// </summary>
        public void SetWindowAndRecompute(DateTimeOffset start, DateTimeOffset end)
        {
            Start = start;
            End = end;
            Duration = End > Start ? End - Start : TimeSpan.Zero;
            RecomputeDerived();
        }
    }

    /// <summary>
    ///     Represents a particularly valuable drop during the hunt.
    /// </summary>
    public sealed class TopDrop
    {
        public string ItemId { get; set; } = string.Empty; // Internal item identifier or name

        public string DisplayName { get; set; } = string.Empty; // Optional user-facing label

        public int Count { get; set; }

        public long EstimatedValueEach { get; set; }

        public long TotalValue => EstimatedValueEach * Count;
    }

    /// <summary>
    ///     Represents the most frequently defeated creature in the hunt.
    /// </summary>
    public sealed class TopMob
    {
        public string Creature { get; set; } = string.Empty;

        public int Count { get; set; }
    }
}