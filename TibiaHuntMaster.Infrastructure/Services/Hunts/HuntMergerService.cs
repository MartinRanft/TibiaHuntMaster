using TibiaHuntMaster.Infrastructure.Data.Entities.Hunts;

namespace TibiaHuntMaster.Infrastructure.Services.Hunts
{
    public sealed class HuntMergerService
    {
        public HuntSessionEntity MergeSessions(List<HuntSessionEntity> sessions)
        {
            if(sessions == null || sessions.Count == 0)
            {
                throw new ArgumentException("No sessions to merge");
            }

            // Wir erstellen eine neue, temporäre Session (ID 0, nicht in DB)
            HuntSessionEntity merged = new()
            {
                // Datum = Das des allerersten Hunts in der Auswahl
                ImportedAt = sessions.Min(s => s.ImportedAt),
                CharacterId = sessions.First().CharacterId,
                RawInput = "Merged Session (Calculated)"
            };

            foreach(HuntSessionEntity s in sessions)
            {
                // Scalars summieren
                merged.Duration += s.Duration;
                merged.XpGain += s.XpGain;
                merged.Loot += s.Loot;
                merged.Supplies += s.Supplies;
                merged.Balance += s.Balance;
                merged.Damage += s.Damage;
                merged.Healing += s.Healing;
                merged.SessionStartTime = s.SessionStartTime;

                // Flags: Wenn einer es hatte, hat es das Merge-Ergebnis auch
                if(s.IsDoubleXp)
                {
                    merged.IsDoubleXp = true;
                }
                if(s.IsDoubleLoot)
                {
                    merged.IsDoubleLoot = true;
                }
                if(s.IsRapidRespawn)
                {
                    merged.IsRapidRespawn = true;
                }

                // Notizen zusammenfügen
                if(!string.IsNullOrWhiteSpace(s.Notes))
                {
                    merged.Notes += $"[{s.ImportedAt:t}]: {s.Notes}\n";
                }

                // Adjustments kopieren
                foreach(HuntSupplyAdjustment adj in s.SupplyAdjustments)
                {
                    merged.SupplyAdjustments.Add(new HuntSupplyAdjustment
                    {
                        Name = adj.Name,
                        Value = adj.Value,
                        Type = adj.Type
                    });
                }
            }

            // Listen mergen (Monster & Loot)
            // Wir müssen gleiche Einträge summieren (z.B. 2x Falcon Knight + 5x Falcon Knight = 7x)

            IEnumerable<HuntMonsterEntry> allMonsters = sessions.SelectMany(s => s.KilledMonsters);
            foreach(IGrouping<string, HuntMonsterEntry> group in allMonsters.GroupBy(m => m.MonsterName))
            {
                merged.KilledMonsters.Add(new HuntMonsterEntry
                {
                    MonsterName = group.Key,
                    Amount = group.Sum(x => x.Amount)
                });
            }

            IEnumerable<HuntLootEntry> allLoot = sessions.SelectMany(s => s.LootItems);
            foreach(IGrouping<string, HuntLootEntry> group in allLoot.GroupBy(l => l.ItemName))
            {
                merged.LootItems.Add(new HuntLootEntry
                {
                    ItemName = group.Key,
                    Amount = group.Sum(x => x.Amount),
                    AmountKept = group.Sum(x => x.AmountKept)
                });
            }

            // XP/h neu berechnen
            // Formel: Total XP / Total Hours
            if(merged.Duration.TotalHours > 0)
            {
                merged.XpPerHour = (long)(merged.XpGain / merged.Duration.TotalHours);
            }
            else
            {
                merged.XpPerHour = 0;
            }

            return merged;
        }
    }
}