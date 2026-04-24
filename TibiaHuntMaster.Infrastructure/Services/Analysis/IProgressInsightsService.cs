namespace TibiaHuntMaster.Infrastructure.Services.Analysis
{
    public enum InsightPeriod
    {
        Last7Days,
        Last30Days,
        Last90Days,
        AllTime
    }

    public enum InsightTrendDirection
    {
        Up,
        Down,
        Flat,
        NoData
    }

    public sealed record TrendInsight(
        long CurrentValue,
        long PreviousValue,
        InsightTrendDirection Direction,
        double ChangePercent,
        int CurrentSamples,
        int PreviousSamples);

    public sealed record LevelProgressInsight(
        int StartLevel,
        int CurrentLevel,
        int Delta,
        int SnapshotCount,
        bool HasBaseline);

    public sealed record LevelTimelinePoint(
        DateTimeOffset Timestamp,
        int Level);

    public sealed record DepotTimelinePoint(
        DateTimeOffset Timestamp,
        long Value);

    public sealed record DepotSalePoint(
        int Id,
        DateTimeOffset Timestamp,
        long RealizedValue);

    public sealed record DepotValueInsight(
        long CurrentOpenValue,
        int ContributingSessions,
        IReadOnlyList<DepotTimelinePoint> Timeline,
        long RealizedValueInPeriod,
        int SaleCountInPeriod,
        DateTimeOffset? LastSaleAtUtc,
        double? AverageDaysBetweenSales,
        IReadOnlyList<DepotSalePoint> RecentSales);

    public sealed record HuntCadenceInsight(
        double AverageHuntHoursPerDay,
        double AverageSessionHours,
        int ObservedDays,
        int SessionCount);

    public sealed record ProgressInsightsResult(
        InsightPeriod Period,
        int SessionCount,
        TrendInsight XpPerHour,
        TrendInsight BalancePerHunt,
        LevelProgressInsight LevelProgress,
        IReadOnlyList<LevelTimelinePoint> LevelTimeline,
        DepotValueInsight DepotProgress,
        HuntCadenceInsight HuntCadence,
        bool HasSkillData)
    {
        public static ProgressInsightsResult Empty(InsightPeriod period, int currentLevel)
        {
            TrendInsight emptyTrend = new(0, 0, InsightTrendDirection.NoData, 0d, 0, 0);
            LevelProgressInsight level = new(currentLevel, currentLevel, 0, 0, false);
            DepotValueInsight depot = new(0, 0, Array.Empty<DepotTimelinePoint>(), 0, 0, null, null, Array.Empty<DepotSalePoint>());
            HuntCadenceInsight cadence = new(0d, 0d, 0, 0);
            return new ProgressInsightsResult(period, 0, emptyTrend, emptyTrend, level, Array.Empty<LevelTimelinePoint>(), depot, cadence, false);
        }
    }

    public interface IProgressInsightsService
    {
        Task<ProgressInsightsResult> GetInsightsAsync(int characterId, int currentLevel, InsightPeriod period, CancellationToken ct = default);

        Task RecordDepotSaleAsync(int characterId, long realizedValue, DateTimeOffset soldAtUtc, CancellationToken ct = default);

        Task DeleteDepotSaleAsync(int characterId, int saleId, CancellationToken ct = default);
    }
}
