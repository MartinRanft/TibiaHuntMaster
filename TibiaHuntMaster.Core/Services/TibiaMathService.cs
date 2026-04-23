namespace TibiaHuntMaster.Core.Services
{
    public static class TibiaMathService
    {
        /// <summary>
        ///     Berechnet die XP für ein bestimmtes Level.
        ///     Formel: (50/3) * (L^3 - 6L^2 + 17L - 12)
        /// </summary>
        public static long ExperienceForLevel(int level)
        {
            if(level <= 1)
            {
                return 0;
            }
            double l = level;
            // Offizielle Tibia Formel
            return (long)Math.Floor(50.0 / 3.0 * (Math.Pow(l, 3) - 6 * Math.Pow(l, 2) + 17 * l - 12));
        }

        /// <summary>
        ///     Berechnet die benötigte Zeit bis zum Ziel-Level basierend auf XP/h.
        /// </summary>
        public static TimePrediction CalculateTimeToLevel(int currentLevel, int targetLevel, long xpPerHour, TimeSpan avgHuntDuration)
        {
            if(targetLevel <= currentLevel)
            {
                return new TimePrediction(TimeSpan.Zero, 0);
            }
            if(xpPerHour <= 0)
            {
                return new TimePrediction(TimeSpan.MaxValue, 0);
            }

            long currentTotalXp = ExperienceForLevel(currentLevel);
            long targetTotalXp = ExperienceForLevel(targetLevel);
            long neededXp = targetTotalXp - currentTotalXp;

            double hoursNeeded = (double)neededXp / xpPerHour;

            // Wie viele Sessions sind das? (aufgerundet)
            int huntsNeeded = avgHuntDuration.TotalHours > 0
            ? (int)Math.Ceiling(hoursNeeded / avgHuntDuration.TotalHours)
            : 0;

            return new TimePrediction(TimeSpan.FromHours(hoursNeeded), huntsNeeded);
        }
    }

    public sealed record TimePrediction(TimeSpan TimeNeeded, int TotalHuntsNeeded);
}