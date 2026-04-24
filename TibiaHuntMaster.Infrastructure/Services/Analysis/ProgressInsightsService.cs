using System.Text.Json;

using Microsoft.EntityFrameworkCore;

using TibiaHuntMaster.Infrastructure.Data;
using TibiaHuntMaster.Infrastructure.Data.Entities.Character;

namespace TibiaHuntMaster.Infrastructure.Services.Analysis
{
    public sealed class ProgressInsightsService(IDbContextFactory<AppDbContext> dbFactory) : IProgressInsightsService
    {
        private static readonly HashSet<string> NonDepotCurrencyItems = new(StringComparer.OrdinalIgnoreCase)
        {
            "GOLD COIN",
            "GOLD COINS",
            "PLATINUM COIN",
            "PLATINUM COINS",
            "CRYSTAL COIN",
            "CRYSTAL COINS"
        };

        private sealed record SoloInsightSessionRow(int SessionId, DateTimeOffset Timestamp, TimeSpan Duration, long XpPerHour, long Balance);
        private sealed record LootEntryProjection(int HuntSessionId, string ItemName, int Amount, int AmountKept);
        private sealed record InsightSession(DateTimeOffset Timestamp, TimeSpan Duration, long XpPerHour, long Balance, long DepotContribution);
        private sealed record SnapshotPoint(DateTimeOffset Timestamp, int Level);
        private sealed record DepotSaleEvent(int Id, DateTimeOffset Timestamp, long RealizedValue);
        private sealed record DepotEvent(DateTimeOffset Timestamp, DepotEventKind Kind, long Amount);

        private enum DepotEventKind
        {
            HuntLoot = 0,
            Sale = 1
        }

        public async Task<ProgressInsightsResult> GetInsightsAsync(int characterId, int currentLevel, InsightPeriod period, CancellationToken ct = default)
        {
            if (characterId <= 0)
            {
                return ProgressInsightsResult.Empty(period, currentLevel);
            }

            await using AppDbContext db = await dbFactory.CreateDbContextAsync(ct);

            List<InsightSession> sessions = await LoadSessionsAsync(db, characterId, ct);
            List<DepotSaleEvent> depotSales = await LoadDepotSalesAsync(db, characterId, ct);
            LevelTimelineData levelTimelineData = await BuildLevelTimelineDataAsync(db, characterId, currentLevel, period, ct);
            if (sessions.Count == 0)
            {
                return ProgressInsightsResult.Empty(period, currentLevel) with
                {
                    LevelProgress = levelTimelineData.Progress,
                    LevelTimeline = levelTimelineData.Timeline,
                    DepotProgress = BuildDepotValueInsight(Array.Empty<InsightSession>(), depotSales, period, DateTimeOffset.UtcNow)
                };
            }

            DateTimeOffset now = DateTimeOffset.UtcNow;
            DateTimeOffset? currentWindowStart = GetWindowStart(now, period);
            TimeSpan? comparisonWindowSize = GetWindowSize(period);

            List<InsightSession> currentWindowSessions = sessions
                .Where(session => IsInCurrentWindow(session.Timestamp, currentWindowStart, now))
                .ToList();

            List<InsightSession> previousWindowSessions = sessions
                .Where(session => IsInPreviousWindow(session.Timestamp, currentWindowStart, comparisonWindowSize))
                .ToList();

            TrendInsight xpTrend = BuildTrend(currentWindowSessions, previousWindowSessions, session => session.XpPerHour);
            TrendInsight balanceTrend = BuildTrend(currentWindowSessions, previousWindowSessions, session => session.Balance);
            DepotValueInsight depotProgress = BuildDepotValueInsight(sessions, depotSales, period, now);
            HuntCadenceInsight huntCadence = BuildHuntCadence(currentWindowSessions, currentWindowStart, now);

            return new ProgressInsightsResult(
                period,
                currentWindowSessions.Count,
                xpTrend,
                balanceTrend,
                levelTimelineData.Progress,
                levelTimelineData.Timeline,
                depotProgress,
                huntCadence,
                HasSkillData: false);
        }

        public async Task RecordDepotSaleAsync(int characterId, long realizedValue, DateTimeOffset soldAtUtc, CancellationToken ct = default)
        {
            if (characterId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(characterId));
            }

            if (realizedValue < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(realizedValue));
            }

            await using AppDbContext db = await dbFactory.CreateDbContextAsync(ct);
            CharacterDepotSaleEntity sale = new()
            {
                CharacterId = characterId,
                SoldAtUtc = soldAtUtc.ToUniversalTime(),
                RealizedValue = realizedValue,
                CreatedAtUtc = DateTimeOffset.UtcNow
            };

            db.CharacterDepotSales.Add(sale);
            await db.SaveChangesAsync(ct);
        }

        public async Task DeleteDepotSaleAsync(int characterId, int saleId, CancellationToken ct = default)
        {
            if (characterId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(characterId));
            }

            if (saleId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(saleId));
            }

            await using AppDbContext db = await dbFactory.CreateDbContextAsync(ct);
            CharacterDepotSaleEntity? sale = await db.CharacterDepotSales
                .FirstOrDefaultAsync(entry => entry.Id == saleId && entry.CharacterId == characterId, ct);

            if (sale is null)
            {
                return;
            }

            db.CharacterDepotSales.Remove(sale);
            await db.SaveChangesAsync(ct);
        }

        private static DateTimeOffset? GetWindowStart(DateTimeOffset now, InsightPeriod period)
        {
            return period switch
            {
                InsightPeriod.Last7Days => now.AddDays(-7),
                InsightPeriod.Last30Days => now.AddDays(-30),
                InsightPeriod.Last90Days => now.AddDays(-90),
                InsightPeriod.AllTime => null,
                _ => null
            };
        }

        private static TimeSpan? GetWindowSize(InsightPeriod period)
        {
            return period switch
            {
                InsightPeriod.Last7Days => TimeSpan.FromDays(7),
                InsightPeriod.Last30Days => TimeSpan.FromDays(30),
                InsightPeriod.Last90Days => TimeSpan.FromDays(90),
                InsightPeriod.AllTime => null,
                _ => null
            };
        }

        private static bool IsInCurrentWindow(DateTimeOffset timestamp, DateTimeOffset? currentWindowStart, DateTimeOffset now)
        {
            if (timestamp > now)
            {
                return false;
            }

            return currentWindowStart is null || timestamp >= currentWindowStart.Value;
        }

        private static bool IsInPreviousWindow(DateTimeOffset timestamp, DateTimeOffset? currentWindowStart, TimeSpan? comparisonWindowSize)
        {
            if (currentWindowStart is null || comparisonWindowSize is null)
            {
                return false;
            }

            DateTimeOffset previousStart = currentWindowStart.Value - comparisonWindowSize.Value;
            return timestamp >= previousStart && timestamp < currentWindowStart.Value;
        }

        private static TrendInsight BuildTrend(
            IReadOnlyCollection<InsightSession> currentWindowSessions,
            IReadOnlyCollection<InsightSession> previousWindowSessions,
            Func<InsightSession, long> selector)
        {
            long currentAverage = CalculateAverage(currentWindowSessions, selector);
            long previousAverage = CalculateAverage(previousWindowSessions, selector);

            if (currentWindowSessions.Count == 0 || previousWindowSessions.Count == 0)
            {
                return new TrendInsight(currentAverage, previousAverage, InsightTrendDirection.NoData, 0d, currentWindowSessions.Count, previousWindowSessions.Count);
            }

            long difference = currentAverage - previousAverage;
            double changePercent = previousAverage == 0
                ? 0d
                : difference / (double)Math.Abs(previousAverage) * 100d;

            InsightTrendDirection direction = Math.Abs(changePercent) < 1d || difference == 0
                ? InsightTrendDirection.Flat
                : difference > 0
                    ? InsightTrendDirection.Up
                    : InsightTrendDirection.Down;

            return new TrendInsight(currentAverage, previousAverage, direction, changePercent, currentWindowSessions.Count, previousWindowSessions.Count);
        }

        private static long CalculateAverage(IReadOnlyCollection<InsightSession> sessions, Func<InsightSession, long> selector)
        {
            if (sessions.Count == 0)
            {
                return 0;
            }

            return (long)Math.Round(sessions.Average(selector));
        }

        private static async Task<List<InsightSession>> LoadSessionsAsync(AppDbContext db, int characterId, CancellationToken ct)
        {
            List<InsightSession> sessions = new();

            List<SoloInsightSessionRow> soloRows = await db.HuntSessions
                .AsNoTracking()
                .Where(session => session.CharacterId == characterId)
                .Select(session => new SoloInsightSessionRow(session.Id, session.SessionStartTime, session.Duration, session.XpPerHour, session.Balance))
                .ToListAsync(ct);

            Dictionary<int, long> depotContributions = await LoadSoloDepotContributionsAsync(
                db,
                soloRows.Select(row => row.SessionId).ToArray(),
                ct);

            List<InsightSession> soloSessions = soloRows
                .Select(row => new InsightSession(
                    row.Timestamp,
                    row.Duration,
                    row.XpPerHour,
                    row.Balance,
                    depotContributions.TryGetValue(row.SessionId, out long contribution) ? contribution : 0))
                .ToList();

            List<InsightSession> teamSessions = await db.TeamHuntSessions
                .AsNoTracking()
                .Where(session => session.CharacterId == characterId)
                .Select(session => new InsightSession(session.SessionStartTime, session.Duration, session.XpPerHour, session.TotalBalance, 0))
                .ToListAsync(ct);

            sessions.AddRange(soloSessions.Where(session => session.Timestamp > DateTimeOffset.MinValue));
            sessions.AddRange(teamSessions.Where(session => session.Timestamp > DateTimeOffset.MinValue));
            sessions.Sort((left, right) => left.Timestamp.CompareTo(right.Timestamp));
            return sessions;
        }

        private static async Task<Dictionary<int, long>> LoadSoloDepotContributionsAsync(
            AppDbContext db,
            IReadOnlyCollection<int> sessionIds,
            CancellationToken ct)
        {
            if (sessionIds.Count == 0)
            {
                return new Dictionary<int, long>();
            }

            List<LootEntryProjection> lootEntries = await db.HuntLootEntries
                .AsNoTracking()
                .Where(entry => sessionIds.Contains(entry.HuntSessionId))
                .Select(entry => new LootEntryProjection(entry.HuntSessionId, entry.ItemName, entry.Amount, entry.AmountKept))
                .ToListAsync(ct);

            if (lootEntries.Count == 0)
            {
                return new Dictionary<int, long>();
            }

            List<string> normalizedNames = lootEntries
                .Select(entry => NormalizeItemName(entry.ItemName))
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Distinct(StringComparer.Ordinal)
                .ToList();

            List<(string NormalizedName, long Value, long? NpcValue, long? NpcPrice)> itemValueRows = await db.Items
                .AsNoTracking()
                .Where(item => normalizedNames.Contains(item.NormalizedName))
                .Select(item => new ValueTuple<string, long, long?, long?>(item.NormalizedName, item.Value, item.NpcValue, item.NpcPrice))
                .ToListAsync(ct);

            Dictionary<string, long> itemValues = itemValueRows
                .GroupBy(item => item.NormalizedName, StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.Max(item => ItemValueResolver.GetEffectiveValue(item.Value, item.NpcValue, item.NpcPrice)), StringComparer.Ordinal);

            Dictionary<int, long> totals = new();

            foreach (LootEntryProjection entry in lootEntries)
            {
                string normalizedName = NormalizeItemName(entry.ItemName);
                if (string.IsNullOrWhiteSpace(normalizedName) || NonDepotCurrencyItems.Contains(normalizedName))
                {
                    continue;
                }

                if (!itemValues.TryGetValue(normalizedName, out long itemValue) || itemValue <= 0)
                {
                    continue;
                }

                int amountToCount = entry.AmountKept > 0
                    ? Math.Max(0, entry.Amount - entry.AmountKept)
                    : entry.Amount;

                if (amountToCount <= 0)
                {
                    continue;
                }

                totals.TryGetValue(entry.HuntSessionId, out long currentValue);
                totals[entry.HuntSessionId] = currentValue + itemValue * amountToCount;
            }

            return totals;
        }

        private static HuntCadenceInsight BuildHuntCadence(
            IReadOnlyList<InsightSession> currentWindowSessions,
            DateTimeOffset? currentWindowStart,
            DateTimeOffset now)
        {
            List<InsightSession> sessionsWithDuration = currentWindowSessions
                .Where(session => session.Duration > TimeSpan.Zero)
                .ToList();

            if (sessionsWithDuration.Count == 0)
            {
                return new HuntCadenceInsight(0d, 0d, 0, currentWindowSessions.Count);
            }

            DateTimeOffset startTimestamp = currentWindowStart ?? sessionsWithDuration.Min(session => session.Timestamp);
            int observedDays = Math.Max(1, (int)Math.Ceiling((now - startTimestamp).TotalDays));

            double totalHuntHours = sessionsWithDuration.Sum(session => session.Duration.TotalHours);
            double averageHuntHoursPerDay = totalHuntHours / observedDays;
            double averageSessionHours = totalHuntHours / sessionsWithDuration.Count;

            return new HuntCadenceInsight(
                averageHuntHoursPerDay,
                averageSessionHours,
                observedDays,
                currentWindowSessions.Count);
        }

        private static async Task<List<DepotSaleEvent>> LoadDepotSalesAsync(AppDbContext db, int characterId, CancellationToken ct)
        {
            return await db.CharacterDepotSales
                .AsNoTracking()
                .Where(sale => sale.CharacterId == characterId)
                .OrderBy(sale => sale.SoldAtUtc)
                .Select(sale => new DepotSaleEvent(sale.Id, sale.SoldAtUtc, sale.RealizedValue))
                .ToListAsync(ct);
        }

        private static DepotValueInsight BuildDepotValueInsight(
            IReadOnlyList<InsightSession> allSessions,
            IReadOnlyList<DepotSaleEvent> allSales,
            InsightPeriod period,
            DateTimeOffset now)
        {
            DateTimeOffset? currentWindowStart = GetWindowStart(now, period);
            List<InsightSession> currentWindowSessions = allSessions
                .Where(session => IsInCurrentWindow(session.Timestamp, currentWindowStart, now))
                .ToList();

            List<DepotSaleEvent> currentWindowSales = allSales
                .Where(sale => IsInCurrentWindow(sale.Timestamp, currentWindowStart, now))
                .ToList();

            long currentOpenValue = CalculateCurrentOpenValue(allSessions, allSales, now);
            List<DepotTimelinePoint> timeline = BuildDepotTimeline(allSessions, allSales, currentWindowStart, now);
            long realizedValueInPeriod = currentWindowSales.Sum(sale => Math.Max(0, sale.RealizedValue));
            DateTimeOffset? lastSaleAtUtc = allSales.Count == 0 ? null : allSales.Max(sale => sale.Timestamp);
            int openSessionCount = allSessions.Count(session =>
                session.Timestamp <= now &&
                session.DepotContribution > 0 &&
                (!lastSaleAtUtc.HasValue || session.Timestamp > lastSaleAtUtc.Value));
            double? averageDaysBetweenSales = CalculateAverageDaysBetweenSales(currentWindowSales);
            IReadOnlyList<DepotSalePoint> recentSales = allSales
                .OrderByDescending(sale => sale.Timestamp)
                .Take(4)
                .OrderBy(sale => sale.Timestamp)
                .Select(sale => new DepotSalePoint(sale.Id, sale.Timestamp, sale.RealizedValue))
                .ToList();

            if (currentWindowSessions.Count == 0 && currentWindowSales.Count == 0 && currentOpenValue == 0)
            {
                return new DepotValueInsight(0, 0, Array.Empty<DepotTimelinePoint>(), 0, 0, lastSaleAtUtc, averageDaysBetweenSales, recentSales);
            }

            return new DepotValueInsight(
                currentOpenValue,
                openSessionCount,
                SampleDepotTimeline(timeline, maxPoints: 8),
                realizedValueInPeriod,
                currentWindowSales.Count,
                lastSaleAtUtc,
                averageDaysBetweenSales,
                recentSales);
        }

        private static long CalculateCurrentOpenValue(
            IReadOnlyList<InsightSession> allSessions,
            IReadOnlyList<DepotSaleEvent> allSales,
            DateTimeOffset now)
        {
            long openValue = 0;
            foreach (DepotEvent entry in BuildDepotEvents(allSessions, allSales).Where(entry => entry.Timestamp <= now))
            {
                openValue = entry.Kind == DepotEventKind.Sale
                    ? 0
                    : openValue + Math.Max(0, entry.Amount);
            }

            return openValue;
        }

        private static List<DepotTimelinePoint> BuildDepotTimeline(
            IReadOnlyList<InsightSession> allSessions,
            IReadOnlyList<DepotSaleEvent> allSales,
            DateTimeOffset? currentWindowStart,
            DateTimeOffset now)
        {
            List<DepotEvent> events = BuildDepotEvents(allSessions, allSales)
                .Where(entry => entry.Timestamp <= now)
                .ToList();

            long openValue = 0;
            List<DepotTimelinePoint> points = new();

            if (currentWindowStart.HasValue)
            {
                foreach (DepotEvent entry in events.Where(entry => entry.Timestamp < currentWindowStart.Value))
                {
                    openValue = entry.Kind == DepotEventKind.Sale
                        ? 0
                        : openValue + Math.Max(0, entry.Amount);
                }

                List<DepotEvent> windowEvents = events
                    .Where(entry => entry.Timestamp >= currentWindowStart.Value)
                    .ToList();

                if (openValue > 0 || windowEvents.Count > 0)
                {
                    points.Add(new DepotTimelinePoint(currentWindowStart.Value, openValue));
                }

                foreach (DepotEvent entry in windowEvents)
                {
                    openValue = entry.Kind == DepotEventKind.Sale
                        ? 0
                        : openValue + Math.Max(0, entry.Amount);
                    points.Add(new DepotTimelinePoint(entry.Timestamp, openValue));
                }

                if (points.Count == 1 && points[0].Timestamp < now)
                {
                    points.Add(new DepotTimelinePoint(now, openValue));
                }

                return points;
            }

            foreach (DepotEvent entry in events)
            {
                openValue = entry.Kind == DepotEventKind.Sale
                    ? 0
                    : openValue + Math.Max(0, entry.Amount);
                points.Add(new DepotTimelinePoint(entry.Timestamp, openValue));
            }

            return points;
        }

        private static List<DepotEvent> BuildDepotEvents(
            IReadOnlyList<InsightSession> allSessions,
            IReadOnlyList<DepotSaleEvent> allSales)
        {
            List<DepotEvent> events = new(allSessions.Count + allSales.Count);
            events.AddRange(
                allSessions
                    .Where(session => session.DepotContribution > 0)
                    .Select(session => new DepotEvent(session.Timestamp, DepotEventKind.HuntLoot, session.DepotContribution)));
            events.AddRange(allSales.Select(sale => new DepotEvent(sale.Timestamp, DepotEventKind.Sale, sale.RealizedValue)));
            events.Sort((left, right) =>
            {
                int timestampComparison = left.Timestamp.CompareTo(right.Timestamp);
                return timestampComparison != 0
                    ? timestampComparison
                    : left.Kind.CompareTo(right.Kind);
            });

            return events;
        }

        private static double? CalculateAverageDaysBetweenSales(IReadOnlyList<DepotSaleEvent> sales)
        {
            if (sales.Count < 2)
            {
                return null;
            }

            List<DepotSaleEvent> orderedSales = sales.OrderBy(sale => sale.Timestamp).ToList();
            double averageDays = orderedSales
                .Zip(orderedSales.Skip(1), (current, next) => (next.Timestamp - current.Timestamp).TotalDays)
                .Average();

            return Math.Round(averageDays, 1);
        }

        private sealed record LevelTimelineData(
            LevelProgressInsight Progress,
            IReadOnlyList<LevelTimelinePoint> Timeline);

        private static async Task<LevelTimelineData> BuildLevelTimelineDataAsync(
            AppDbContext db,
            int characterId,
            int currentLevel,
            InsightPeriod period,
            CancellationToken ct)
        {
            List<SnapshotPoint> snapshots = (await db.CharacterSnapshots
                .AsNoTracking()
                .Where(snapshot => snapshot.CharacterId == characterId)
                .OrderBy(snapshot => snapshot.FetchedAtUtc)
                .Select(snapshot => new { snapshot.FetchedAtUtc, snapshot.RawJson })
                .ToListAsync(ct))
                .Select(snapshot =>
                {
                    int? level = TryParseLevel(snapshot.RawJson);
                    return level.HasValue
                        ? new SnapshotPoint(snapshot.FetchedAtUtc, level.Value)
                        : null;
                })
                .Where(snapshot => snapshot != null)
                .Select(snapshot => snapshot!)
                .ToList();

            if (snapshots.Count == 0)
            {
                return new LevelTimelineData(
                    new LevelProgressInsight(currentLevel, currentLevel, 0, 0, false),
                    Array.Empty<LevelTimelinePoint>());
            }

            DateTimeOffset? currentWindowStart = GetWindowStart(DateTimeOffset.UtcNow, period);
            List<SnapshotPoint> currentWindowSnapshots = currentWindowStart is null
                ? snapshots
                : snapshots.Where(snapshot => snapshot.Timestamp >= currentWindowStart.Value).ToList();

            SnapshotPoint? baseline = currentWindowStart is null
                ? snapshots.FirstOrDefault()
                : snapshots.LastOrDefault(snapshot => snapshot.Timestamp < currentWindowStart.Value)
                  ?? currentWindowSnapshots.FirstOrDefault();

            if (baseline is null)
            {
                return new LevelTimelineData(
                    new LevelProgressInsight(currentLevel, currentLevel, 0, currentWindowSnapshots.Count, false),
                    Array.Empty<LevelTimelinePoint>());
            }

            LevelProgressInsight progress = new(
                baseline.Level,
                currentLevel,
                currentLevel - baseline.Level,
                currentWindowSnapshots.Count,
                true);

            List<LevelTimelinePoint> timeline = BuildTimelinePoints(baseline, currentWindowSnapshots, currentLevel);
            return new LevelTimelineData(progress, timeline);
        }

        private static List<LevelTimelinePoint> BuildTimelinePoints(SnapshotPoint baseline, IReadOnlyList<SnapshotPoint> currentWindowSnapshots, int currentLevel)
        {
            List<SnapshotPoint> orderedPoints = new() { baseline };
            orderedPoints.AddRange(currentWindowSnapshots.Where(snapshot => snapshot.Timestamp > baseline.Timestamp));

            List<SnapshotPoint> distinctPoints = orderedPoints
                .GroupBy(snapshot => snapshot.Timestamp)
                .Select(group => group.Last())
                .OrderBy(snapshot => snapshot.Timestamp)
                .ToList();

            List<LevelTimelinePoint> changedLevels = distinctPoints
                .Where((snapshot, index) => index == 0 || snapshot.Level != distinctPoints[index - 1].Level)
                .Select(snapshot => new LevelTimelinePoint(snapshot.Timestamp, snapshot.Level))
                .ToList();

            if (changedLevels.Count == 0)
            {
                changedLevels.Add(new LevelTimelinePoint(baseline.Timestamp, baseline.Level));
            }

            LevelTimelinePoint lastPoint = changedLevels[^1];
            if (lastPoint.Level != currentLevel)
            {
                changedLevels.Add(new LevelTimelinePoint(DateTimeOffset.UtcNow, currentLevel));
            }

            return SampleTimeline(changedLevels, maxPoints: 6);
        }

        private static List<LevelTimelinePoint> SampleTimeline(IReadOnlyList<LevelTimelinePoint> points, int maxPoints)
        {
            if (points.Count <= maxPoints)
            {
                return points.ToList();
            }

            HashSet<int> selectedIndices = new() { 0, points.Count - 1 };
            for (int i = 1; i < maxPoints - 1; i++)
            {
                int index = (int)Math.Round(i * (points.Count - 1d) / (maxPoints - 1d));
                selectedIndices.Add(index);
            }

            return selectedIndices
                .OrderBy(index => index)
                .Select(index => points[index])
                .ToList();
        }

        private static List<DepotTimelinePoint> SampleDepotTimeline(IReadOnlyList<DepotTimelinePoint> points, int maxPoints)
        {
            if (points.Count <= maxPoints)
            {
                return points.ToList();
            }

            HashSet<int> selectedIndices = new() { 0, points.Count - 1 };
            for (int i = 1; i < maxPoints - 1; i++)
            {
                int index = (int)Math.Round(i * (points.Count - 1d) / (maxPoints - 1d));
                selectedIndices.Add(index);
            }

            return selectedIndices
                .OrderBy(index => index)
                .Select(index => points[index])
                .ToList();
        }

        private static string NormalizeItemName(string? itemName)
        {
            return itemName?.Trim().ToUpperInvariant() ?? string.Empty;
        }

        private static int? TryParseLevel(string rawJson)
        {
            if (string.IsNullOrWhiteSpace(rawJson))
            {
                return null;
            }

            try
            {
                using JsonDocument document = JsonDocument.Parse(rawJson);
                if (!document.RootElement.TryGetProperty("character", out JsonElement characterContainer))
                {
                    return null;
                }

                if (!characterContainer.TryGetProperty("character", out JsonElement characterDetails))
                {
                    return null;
                }

                if (!characterDetails.TryGetProperty("level", out JsonElement levelElement))
                {
                    return null;
                }

                return levelElement.TryGetInt32(out int level) ? level : null;
            }
            catch (JsonException)
            {
                return null;
            }
        }
    }
}
