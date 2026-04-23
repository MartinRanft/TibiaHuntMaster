using Microsoft.EntityFrameworkCore;

using TibiaHuntMaster.Infrastructure.Data;
using TibiaHuntMaster.Infrastructure.Data.Entities.Hunts;

namespace TibiaHuntMaster.Infrastructure.Services.Analysis
{
    public sealed record LootGroup(string Vendor, long TotalGold, double TotalWeight, List<LootItemView> Items);

    public sealed record LootItemView(string Name, int Amount, long TotalValue, double TotalWeight, string SellTo);

    public sealed class LootAnalysisService(IDbContextFactory<AppDbContext> dbFactory) : ILootAnalysisService
    {
        public async Task<List<LootGroup>> AnalyzeSessionLootAsync(int sessionId, CancellationToken ct = default)
        {
            await using AppDbContext db = await dbFactory.CreateDbContextAsync(ct);
            List<HuntLootEntry> lootEntries = await db.HuntLootEntries
                                                      .Where(x => x.HuntSessionId == sessionId)
                                                      .ToListAsync(ct);

            return await AnalyzeInternalAsync(lootEntries, db, ct);
        }

        public async Task<List<LootGroup>> AnalyzeLootListAsync(List<HuntLootEntry> lootEntries, CancellationToken ct = default)
        {
            await using AppDbContext db = await dbFactory.CreateDbContextAsync(ct);
            return await AnalyzeInternalAsync(lootEntries, db, ct);
        }

        private async Task<List<LootGroup>> AnalyzeInternalAsync(List<HuntLootEntry> lootEntries, AppDbContext db, CancellationToken ct)
        {
            if(lootEntries.Count == 0)
            {
                return [];
            }

            // 1. Namen normalisieren (UPPER CASE)
            List<string> logItemNames = lootEntries
                                        .Select(x => x.ItemName.Trim().ToUpperInvariant())
                                        .Distinct()
                                        .ToList();

            // 2. PERFORMANCE FIX: Nur auf NormalizedName suchen!
            // Das nutzt den Datenbank-Index. Das "OR ActualName.ToUpper()" war der Performance-Killer.
            List<ItemData> wikiItems = await db.Items
                                               .Where(i => logItemNames.Contains(i.NormalizedName))
                                               .Select(i => new ItemData(i.NormalizedName, i.ActualName, i.SellTo, i.Value, i.NpcValue, i.NpcPrice, i.WeightOz))
                                               .ToListAsync(ct);

            // 3. Mapping Dictionary
            Dictionary<string, ItemData> itemMap = new();

            foreach(ItemData item in wikiItems.Where(item => !itemMap.ContainsKey(item.NormalizedName)))
            {
                itemMap[item.NormalizedName] = item;
            }

            // 4. Gruppierung
            Dictionary<string, List<LootItemView>> grouped = new();

            foreach(HuntLootEntry entry in lootEntries)
            {
                string vendor = "Unknown/Market";
                long valueEach = 0;
                double weightEach = 0;

                // Lookup Key muss UPPERCASE sein
                string lookupKey = entry.ItemName.Trim().ToUpperInvariant();

                if(itemMap.TryGetValue(lookupKey, out ItemData? wikiItem))
                {
                    vendor = DetermineBestVendor(wikiItem.SellTo);
                    valueEach = ItemValueResolver.GetEffectiveValue(wikiItem.Value, wikiItem.NpcValue, wikiItem.NpcPrice);
                    weightEach = (double)(wikiItem.WeightOz ?? 0);
                }

                if(!grouped.ContainsKey(vendor))
                {
                    grouped[vendor] = new List<LootItemView>();
                }

                grouped[vendor].Add(new LootItemView(
                    entry.ItemName,
                    entry.Amount,
                    valueEach * entry.Amount,
                    weightEach * entry.Amount,
                    vendor
                ));
            }

            return grouped.Select(g => new LootGroup(
                g.Key,
                g.Value.Sum(x => x.TotalValue),
                g.Value.Sum(x => x.TotalWeight),
                g.Value.OrderByDescending(x => x.TotalValue).ToList()
            )).OrderByDescending(g => g.TotalGold).ToList();
        }

        private static string DetermineBestVendor(string? sellTo)
        {
            if(string.IsNullOrWhiteSpace(sellTo) || sellTo == "--")
            {
                return "Unknown/Market";
            }

            string raw = sellTo.ToLowerInvariant();

            if(raw.Contains("yasir"))
            {
                return "Yasir";
            }
            if(raw.Contains("rashid"))
            {
                return "Rashid";
            }
            if(raw.Contains("green djinn") || raw.Contains("alesar"))
            {
                return "Green Djinn";
            }
            if(raw.Contains("blue djinn") || raw.Contains("nah'bob") || raw.Contains("haroun"))
            {
                return "Blue Djinn";
            }
            if(raw.Contains("esrik"))
            {
                return "Esrik (Farmine)";
            }
            if(raw.Contains("gnomission"))
            {
                return "Gnomission";
            }
            if(raw.Contains("grizzly adams"))
            {
                return "Grizzly Adams";
            }

            string[] parts = sellTo.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            return parts.Length > 0 ? $"{parts[0]} (City)" : "Unknown/Market";
        }

        // DTO
        private sealed record ItemData(string NormalizedName, string ActualName, string SellTo, long Value, long? NpcValue, long? NpcPrice, decimal? WeightOz);
    }
}
