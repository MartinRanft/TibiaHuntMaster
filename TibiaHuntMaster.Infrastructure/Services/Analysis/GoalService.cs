using Microsoft.EntityFrameworkCore;

using TibiaHuntMaster.Infrastructure.Data;
using TibiaHuntMaster.Infrastructure.Data.Entities.Character;
using TibiaHuntMaster.Infrastructure.Data.Entities.TibiaData;

namespace TibiaHuntMaster.Infrastructure.Services.Analysis
{
    public sealed record GoalProgressResult(
        CharacterGoalEntity Goal,
        long CurrentValue,
        double Percentage
    );

    public sealed class GoalService(IDbContextFactory<AppDbContext> dbFactory) : IGoalService
    {
        public async Task<List<GoalProgressResult>> GetGoalsForCharacterAsync(int characterId, int currentLevel)
        {
            await using AppDbContext db = await dbFactory.CreateDbContextAsync();

            List<CharacterGoalEntity> goals = await db.CharacterGoals
                                                      .Where(g => g.CharacterId == characterId && g.IsActive)
                                                      .OrderBy(g => g.IsCompleted)
                                                      .ThenByDescending(g => g.CreatedAt)
                                                      .ToListAsync();

            List<GoalProgressResult> results = new();
            bool hasChanges = false;

            foreach(CharacterGoalEntity goal in goals)
            {
                long current = 0;

                if(goal.Type == GoalType.Level)
                {
                    // Bei Level nehmen wir das aktuelle Char-Level aus der DB (wird beim Start aktualisiert)
                    current = currentLevel;
                }
                else if(goal.Type == GoalType.Gold)
                {
                    // Bei Gold summieren wir Profit seit Erstellung
                    long huntProfit = await db.HuntSessions
                                              .AsNoTracking()
                                              .Where(s => s.CharacterId == characterId && s.ImportedAt >= goal.CreatedAt)
                                              .SumAsync(s => s.Balance);

                    current = goal.ManualProgressOffset + huntProfit;
                }

                // --- BERECHNUNG LOGIK FIX ---
                double percentage = 0;

                if(goal.Type == GoalType.Level)
                {
                    // Level berechnen wir relativ zum Base-Level (StartValue).
                    // Beispiel: Start 500, Ziel 600 => 0% bei 500, 50% bei 550.
                    // Fällt der Charakter durch Death unter das Base-Level, wird unten auf 0% geklemmt.
                    long totalNeeded = goal.TargetValue - goal.StartValue;
                    long totalDone = current - goal.StartValue;

                    if(totalNeeded > 0)
                    {
                        percentage = (double)totalDone / totalNeeded * 100.0;
                    }
                    else if(current >= goal.TargetValue)
                    {
                        percentage = 100;
                    }
                }
                else
                {
                    // Gold berechnen wir RELATIV zum Start (meist 0).
                    // Wenn man 50kk farmen will, fängt man bei 0% an, egal wie viel man auf der Bank hat.
                    long totalNeeded = goal.TargetValue - goal.StartValue;
                    long totalDone = current - goal.StartValue;

                    if(totalNeeded > 0)
                    {
                        percentage = (double)totalDone / totalNeeded * 100.0;
                    }
                    else if(current >= goal.TargetValue)
                    {
                        percentage = 100;
                    }
                }

                // Clamp 0-100
                if(percentage < 0)
                {
                    percentage = 0;
                }
                // Optional: Bei Level nicht cappen, falls man drüber ist? Nein, 100% ist max für UI.
                if(percentage > 100)
                {
                    percentage = 100;
                }

                // Auto-complete goals that have reached 100%
                if(!goal.IsCompleted && current >= goal.TargetValue)
                {
                    goal.IsCompleted = true;
                    hasChanges = true;
                }

                results.Add(new GoalProgressResult(goal, current, percentage));
            }

            // Save changes if any goals were auto-completed
            if(hasChanges)
            {
                await db.SaveChangesAsync();
            }

            return results;
        }

        public async Task AddGoalAsync(CharacterGoalEntity goal)
        {
            await using AppDbContext db = await dbFactory.CreateDbContextAsync();
            db.CharacterGoals.Add(goal);
            await db.SaveChangesAsync();
        }

        public async Task UpdateGoalAsync(CharacterGoalEntity goal)
        {
            await using AppDbContext db = await dbFactory.CreateDbContextAsync();
            db.CharacterGoals.Update(goal);
            await db.SaveChangesAsync();
        }

        public async Task DeleteGoalAsync(int goalId)
        {
            await using AppDbContext db = await dbFactory.CreateDbContextAsync();
            CharacterGoalEntity? goal = await db.CharacterGoals.FindAsync(goalId);
            if(goal != null)
            {
                db.CharacterGoals.Remove(goal);
                await db.SaveChangesAsync();
            }
        }

        public async Task ProcessHuntProgressAsync(int charId, int? soloSessionId, int? teamSessionId, long balanceChange, long xpChange)
        {
            await using AppDbContext db = await dbFactory.CreateDbContextAsync();

            List<CharacterGoalEntity> activeGoals = await db.CharacterGoals
                                                            .Where(g => g.CharacterId == charId && g.IsActive)
                                                            .ToListAsync();

            if(activeGoals.Count == 0)
            {
                return;
            }

            foreach(CharacterGoalEntity goal in activeGoals)
            {
                bool contributes = false;

                switch (goal.Type)
                {
                    case GoalType.Gold when balanceChange != 0:
                    case GoalType.Level when xpChange != 0:
                        contributes = true;
                        break;
                }

                if(contributes)
                {
                    HuntGoalConnectionEntity connection = new()
                    {
                        CharacterGoalId = goal.Id,
                        HuntSessionId = soloSessionId,
                        TeamHuntSessionId = teamSessionId,
                        IsFinisher = false
                    };

                    long currentProgress = await CalculateCurrentProgress(db, goal, charId);

                    if(currentProgress >= goal.TargetValue)
                    {
                        goal.IsCompleted = true;
                        connection.IsFinisher = true;
                    }

                    db.HuntGoalConnections.Add(connection);
                    db.CharacterGoals.Update(goal);
                }
            }
            await db.SaveChangesAsync();
        }

        private async Task<long> CalculateCurrentProgress(AppDbContext db, CharacterGoalEntity goal, int charId)
        {
            if(goal.Type == GoalType.Level)
            {
                // Level holen wir direkt vom Char
                CharacterEntity? c = await db.Characters.FindAsync(charId);
                return c?.Level ?? 0;
            }

            long huntSum = await db.HuntSessions
                                   .Where(s => s.CharacterId == charId && s.ImportedAt >= goal.CreatedAt)
                                   .SumAsync(s => s.Balance);

            // wird noch nicht einberechnet da hier noch überlegt werden muss wie wir das machen am besten.
            long teamSum = await db.TeamHuntSessions
                                   .Where(s => s.CharacterId == charId && s.ImportedAt >= goal.CreatedAt)
                                   .SumAsync(s => s.TotalBalance);

            return goal.ManualProgressOffset + huntSum;
        }

        // Helper für die History Filter
        public async Task<List<CharacterGoalEntity>> GetAllGoalsAsync(int charId)
        {
            await using AppDbContext db = await dbFactory.CreateDbContextAsync();
            return await db.CharacterGoals.AsNoTracking().Where(g => g.CharacterId == charId).ToListAsync();
        }
    }
}
