using Microsoft.EntityFrameworkCore;

using TibiaHuntMaster.Infrastructure.Data;
using TibiaHuntMaster.Infrastructure.Data.Entities.Hunts;

namespace TibiaHuntMaster.Infrastructure.Services.Hunts
{
    public sealed class HuntGroupingService(IDbContextFactory<AppDbContext> dbFactory)
    {
        public async Task<HuntGroupEntity> CreateGroupAsync(string name, List<int> sessionIds)
        {
            await using AppDbContext db = await dbFactory.CreateDbContextAsync();

            HuntGroupEntity group = new()
            {
                Name = name,
                CreatedAt = DateTimeOffset.UtcNow
            };

            db.HuntGroups.Add(group);
            await db.SaveChangesAsync(); // ID generieren

            // Sessions updaten
            List<HuntSessionEntity> sessions = await db.HuntSessions.Where(s => sessionIds.Contains(s.Id)).ToListAsync();
            foreach(HuntSessionEntity s in sessions)
            {
                s.HuntGroupId = group.Id;
            }

            await db.SaveChangesAsync();
            return group;
        }

        public async Task RemoveSessionFromGroupAsync(int sessionId)
        {
            await using AppDbContext db = await dbFactory.CreateDbContextAsync();

            // 1. Session laden (inkl. Gruppe um Nachbarn zu prüfen)
            HuntSessionEntity? session = await db.HuntSessions
                                                 .Include(s => s.Group)
                                                 .ThenInclude(g => g!.Sessions)
                                                 .FirstOrDefaultAsync(s => s.Id == sessionId);

            if(session?.HuntGroupId == null || session.Group == null)
            {
                return;
            }

            HuntGroupEntity group = session.Group;

            // 2. Session aus Gruppe entfernen
            session.HuntGroupId = null;

            // 3. Prüfen: Wie viele bleiben übrig?
            // Da wir die Session noch im Speicher in der Liste haben, ist Count noch der alte Wert.
            // Wir zählen die, die NICHT die aktuelle ID haben.
            int remainingCount = group.Sessions.Count(s => s.Id != sessionId);

            if(remainingCount <= 1)
            {
                // Gruppe auflösen!
                // Die verbleibende Session (falls > 0) auch "befreien"
                foreach(HuntSessionEntity remaining in group.Sessions)
                {
                    remaining.HuntGroupId = null;
                }

                // Gruppe löschen
                db.HuntGroups.Remove(group);
            }

            await db.SaveChangesAsync();
        }
    }
}