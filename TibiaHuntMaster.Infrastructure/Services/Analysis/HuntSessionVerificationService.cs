using Microsoft.EntityFrameworkCore;

using TibiaHuntMaster.Infrastructure.Data;
using TibiaHuntMaster.Infrastructure.Data.Entities.Hunts;

namespace TibiaHuntMaster.Infrastructure.Services.Analysis
{
    public sealed class HuntSessionVerificationService(IDbContextFactory<AppDbContext> dbFactory) : IHuntSessionVerificationService
    {
        private const int DefaultSoloXpRatePercent = 150;
        private const long WarningThreshold = 10_000;

        public async Task<HuntSessionVerificationResult> VerifyAsync(HuntSessionEntity session, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(session);

            await using AppDbContext db = await dbFactory.CreateDbContextAsync(ct);

            LootVerification loot = await VerifyLootAsync(db, session, ct);
            CreatureVerification creatures = await VerifyCreaturesAsync(db, session, ct);

            long calculatedXpGain = CalculateDisplayedXpGain(session, creatures.CalculatedRawXpGain);
            long calculatedXpPerHour = CalculateXpPerHour(calculatedXpGain, session.Duration);

            long lootDelta = loot.CalculatedLoot - session.Loot;
            bool hasLootMismatch = Math.Abs(lootDelta) > WarningThreshold || loot.UnmatchedItems.Count > 0;
            bool canApplyLootCorrection = Math.Abs(lootDelta) > WarningThreshold && loot.UnmatchedItems.Count == 0;

            long? rawXpDelta = session.RawXpGain.HasValue
                ? creatures.CalculatedRawXpGain - session.RawXpGain.Value
                : null;
            bool hasRawXpMismatch = rawXpDelta.HasValue && Math.Abs(rawXpDelta.Value) > WarningThreshold;

            long xpDelta = calculatedXpGain - session.XpGain;
            bool hasXpMismatch = Math.Abs(xpDelta) > WarningThreshold || creatures.UnmatchedCreatures.Count > 0;
            bool canApplyXpCorrection =
                creatures.UnmatchedCreatures.Count == 0 &&
                (hasRawXpMismatch || Math.Abs(xpDelta) > WarningThreshold);

            return new HuntSessionVerificationResult(
                session.Loot,
                loot.CalculatedLoot,
                lootDelta,
                hasLootMismatch,
                canApplyLootCorrection,
                session.RawXpGain,
                creatures.CalculatedRawXpGain,
                rawXpDelta,
                hasRawXpMismatch,
                session.XpGain,
                calculatedXpGain,
                xpDelta,
                session.XpPerHour,
                calculatedXpPerHour,
                hasXpMismatch,
                !session.RawXpGain.HasValue,
                canApplyXpCorrection,
                loot.UnmatchedItems,
                creatures.UnmatchedCreatures);
        }

        private static async Task<LootVerification> VerifyLootAsync(AppDbContext db, HuntSessionEntity session, CancellationToken ct)
        {
            if(session.LootItems.Count == 0)
            {
                return new LootVerification(0, []);
            }

            List<string> normalizedNames = session.LootItems
                .Select(entry => NormalizeItemName(entry.ItemName))
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Distinct(StringComparer.Ordinal)
                .ToList();

            List<ItemValueData> itemRows = await db.Items
                .AsNoTracking()
                .Where(item => normalizedNames.Contains(item.NormalizedName))
                .Select(item => new ItemValueData(item.NormalizedName, item.Value, item.NpcValue, item.NpcPrice))
                .ToListAsync(ct);

            Dictionary<string, ItemValueData> itemLookup = itemRows
                .GroupBy(item => item.NormalizedName, StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);

            long calculatedLoot = 0;
            HashSet<string> unmatchedItems = new(StringComparer.OrdinalIgnoreCase);

            foreach(HuntLootEntry entry in session.LootItems)
            {
                string normalizedName = NormalizeItemName(entry.ItemName);
                if(!itemLookup.TryGetValue(normalizedName, out ItemValueData? itemData))
                {
                    unmatchedItems.Add(entry.ItemName);
                    continue;
                }

                long itemValue = ItemValueResolver.GetEffectiveValue(itemData.Value, itemData.NpcValue, itemData.NpcPrice);
                calculatedLoot += itemValue * entry.Amount;
            }

            return new LootVerification(calculatedLoot, unmatchedItems.OrderBy(name => name, StringComparer.CurrentCultureIgnoreCase).ToList());
        }

        private static async Task<CreatureVerification> VerifyCreaturesAsync(AppDbContext db, HuntSessionEntity session, CancellationToken ct)
        {
            if(session.KilledMonsters.Count == 0)
            {
                return new CreatureVerification(0, []);
            }

            List<CreatureExpData> creatures = await db.Creatures
                .AsNoTracking()
                .Select(creature => new CreatureExpData(creature.Name, creature.ActualName, creature.Exp))
                .ToListAsync(ct);

            Dictionary<string, long> expByName = new(StringComparer.Ordinal);
            foreach(CreatureExpData creature in creatures)
            {
                if(!creature.Exp.HasValue || creature.Exp.Value <= 0)
                {
                    continue;
                }

                AddCreatureLookup(expByName, NormalizeMonsterName(creature.Name), creature.Exp.Value);
                AddCreatureLookup(expByName, NormalizeMonsterName(creature.ActualName), creature.Exp.Value);
            }

            long calculatedRawXp = 0;
            HashSet<string> unmatchedCreatures = new(StringComparer.OrdinalIgnoreCase);

            foreach(HuntMonsterEntry entry in session.KilledMonsters)
            {
                string normalizedName = NormalizeMonsterName(entry.MonsterName);
                if(!expByName.TryGetValue(normalizedName, out long exp))
                {
                    unmatchedCreatures.Add(entry.MonsterName);
                    continue;
                }

                calculatedRawXp += exp * entry.Amount;
            }

            return new CreatureVerification(calculatedRawXp, unmatchedCreatures.OrderBy(name => name, StringComparer.CurrentCultureIgnoreCase).ToList());
        }

        private static long CalculateDisplayedXpGain(HuntSessionEntity session, long calculatedRawXp)
        {
            if(calculatedRawXp <= 0)
            {
                return 0;
            }

            double displayedFactor = ResolveDisplayedXpFactor(
                session.Duration,
                session.IsDoubleXp,
                session.XpBoostPercent,
                session.XpBoostActiveMinutes,
                session.CustomXpRatePercent);

            if(displayedFactor <= 0.01d)
            {
                return 0;
            }

            return (long)Math.Round(calculatedRawXp * displayedFactor, MidpointRounding.AwayFromZero);
        }

        private static long CalculateXpPerHour(long xpGain, TimeSpan duration)
        {
            if(xpGain <= 0 || duration.TotalHours <= 0.01d)
            {
                return 0;
            }

            return (long)Math.Round(xpGain / duration.TotalHours, MidpointRounding.AwayFromZero);
        }

        private static double ResolveDisplayedXpFactor(
            TimeSpan duration,
            bool isDoubleXp,
            int? xpBoostPercent,
            int? xpBoostActiveMinutes,
            int? customXpRatePercent)
        {
            double baseFactor = (customXpRatePercent.HasValue && customXpRatePercent.Value > 0
                ? customXpRatePercent.Value
                : DefaultSoloXpRatePercent) / 100d;

            if(isDoubleXp)
            {
                baseFactor *= 2d;
            }

            if(!xpBoostPercent.HasValue || xpBoostPercent.Value <= 0)
            {
                return baseFactor;
            }

            double effectiveMinutes = duration.TotalMinutes;
            if(effectiveMinutes <= 0.01d)
            {
                return baseFactor;
            }

            double boostedFactor = baseFactor + (xpBoostPercent.Value / 100d);
            double boostedMinutes = xpBoostActiveMinutes.HasValue && xpBoostActiveMinutes.Value > 0
                ? Math.Min(effectiveMinutes, xpBoostActiveMinutes.Value)
                : effectiveMinutes;
            double normalMinutes = Math.Max(0d, effectiveMinutes - boostedMinutes);

            return ((boostedMinutes * boostedFactor) + (normalMinutes * baseFactor)) / effectiveMinutes;
        }

        private static void AddCreatureLookup(Dictionary<string, long> lookup, string normalizedName, long exp)
        {
            if(string.IsNullOrWhiteSpace(normalizedName) || lookup.ContainsKey(normalizedName))
            {
                return;
            }

            lookup[normalizedName] = exp;
        }

        private static string NormalizeItemName(string? rawName)
        {
            return rawName?.Trim().ToUpperInvariant() ?? string.Empty;
        }

        private static string NormalizeMonsterName(string? rawName)
        {
            if(string.IsNullOrWhiteSpace(rawName))
            {
                return string.Empty;
            }

            string normalized = rawName.Trim()
                .Replace('’', '\'')
                .Replace('-', ' ')
                .ToLowerInvariant();

            if(normalized.StartsWith("a ", StringComparison.Ordinal))
            {
                normalized = normalized[2..];
            }
            else if(normalized.StartsWith("an ", StringComparison.Ordinal))
            {
                normalized = normalized[3..];
            }
            else if(normalized.StartsWith("the ", StringComparison.Ordinal))
            {
                normalized = normalized[4..];
            }

            string[] parts = normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return string.Join(' ', parts);
        }

        private sealed record ItemValueData(string NormalizedName, long Value, long? NpcValue, long? NpcPrice);

        private sealed record CreatureExpData(string Name, string ActualName, long? Exp);

        private sealed record LootVerification(long CalculatedLoot, IReadOnlyList<string> UnmatchedItems);

        private sealed record CreatureVerification(long CalculatedRawXpGain, IReadOnlyList<string> UnmatchedCreatures);
    }
}
