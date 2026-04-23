using TibiaHuntMaster.Infrastructure.Data.Entities.Hunts;

namespace TibiaHuntMaster.Infrastructure.Services.Analysis
{
    public sealed record ComparisonResult(
        double XpDiffPercent,
        double ProfitDiffPercent,
        double DamageDiffPercent,
        string WinnerXp, // "Session A" or "Session B"
        string WinnerProfit
    );

    public sealed class HuntComparisonService : IHuntComparisonService
    {
        public ComparisonResult Compare(HuntSessionEntity sessionA, HuntSessionEntity sessionB)
        {
            long xpA = sessionA.XpPerHour;
            long xpB = sessionB.XpPerHour;

            long profitA = sessionA.Balance; // Hier könnte man Balance/h berechnen für Fairness
            long profitB = sessionB.Balance;

            // Prozentuale Unterschiede
            double xpDiff = CalculatePercentDiff(xpA, xpB);
            double profitDiff = CalculatePercentDiff(profitA, profitB);
            double dmgDiff = CalculatePercentDiff(sessionA.Damage, sessionB.Damage);

            return new ComparisonResult(
                xpDiff,
                profitDiff,
                dmgDiff,
                xpA > xpB ? "Session A" : "Session B",
                profitA > profitB ? "Session A" : "Session B"
            );
        }

        private static double CalculatePercentDiff(long a, long b)
        {
            if(b == 0)
            {
                return 0;
            }
            return (double)(a - b) / b * 100.0;
        }
    }
}