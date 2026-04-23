using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

using TibiaHuntMaster.Infrastructure.Data;
using TibiaHuntMaster.Infrastructure.Data.Entities.Character;
using TibiaHuntMaster.Infrastructure.Data.Entities.TibiaData;
using TibiaHuntMaster.Infrastructure.Services.Analysis;

namespace TibiaHuntMaster.Tests.Analysis
{
    public sealed class GoalServiceTests
    {
        [Fact]
        public async Task GetGoalsForCharacterAsync_LevelGoal_ShouldUseStartValueAsBaseline()
        {
            // Arrange
            (IDbContextFactory<AppDbContext> factory, SqliteConnection connection) = CreateFactory();
            int characterId = 0;

            try
            {
                await using (AppDbContext db = await factory.CreateDbContextAsync())
                {
                    CharacterEntity character = new()
                    {
                        Name = "Tester",
                        World = "Antica",
                        Vocation = "Knight",
                        Level = 550,
                        GuildName = string.Empty,
                        Residence = string.Empty,
                        Title = string.Empty,
                        AccountStatus = string.Empty,
                        AchievementPoints = 0,
                        Sex = "male",
                        LastUpdatedUtc = DateTimeOffset.UtcNow
                    };
                    db.Characters.Add(character);
                    await db.SaveChangesAsync();
                    characterId = character.Id;

                    CharacterGoalEntity goal = new()
                    {
                        CharacterId = characterId,
                        Title = "Level 600",
                        Type = GoalType.Level,
                        StartValue = 500,
                        TargetValue = 600,
                        IsActive = true
                    };
                    db.CharacterGoals.Add(goal);
                    await db.SaveChangesAsync();
                }

                GoalService sut = new(factory);

                // Act
                List<GoalProgressResult> result = await sut.GetGoalsForCharacterAsync(characterId, currentLevel: 550);

                // Assert
                result.Should().HaveCount(1);
                result[0].CurrentValue.Should().Be(550);
                result[0].Percentage.Should().BeApproximately(50.0, 0.001);
            }
            finally
            {
                connection.Dispose();
            }
        }

        [Fact]
        public async Task GetGoalsForCharacterAsync_LevelGoal_WhenCurrentFallsBelowBase_ShouldClampToZero()
        {
            // Arrange
            (IDbContextFactory<AppDbContext> factory, SqliteConnection connection) = CreateFactory();
            int characterId = 0;

            try
            {
                await using (AppDbContext db = await factory.CreateDbContextAsync())
                {
                    CharacterEntity character = new()
                    {
                        Name = "Tester2",
                        World = "Antica",
                        Vocation = "Druid",
                        Level = 480,
                        GuildName = string.Empty,
                        Residence = string.Empty,
                        Title = string.Empty,
                        AccountStatus = string.Empty,
                        AchievementPoints = 0,
                        Sex = "female",
                        LastUpdatedUtc = DateTimeOffset.UtcNow
                    };
                    db.Characters.Add(character);
                    await db.SaveChangesAsync();
                    characterId = character.Id;

                    CharacterGoalEntity goal = new()
                    {
                        CharacterId = characterId,
                        Title = "Back to 600",
                        Type = GoalType.Level,
                        StartValue = 500,
                        TargetValue = 600,
                        IsActive = true
                    };
                    db.CharacterGoals.Add(goal);
                    await db.SaveChangesAsync();
                }

                GoalService sut = new(factory);

                // Act
                List<GoalProgressResult> result = await sut.GetGoalsForCharacterAsync(characterId, currentLevel: 480);

                // Assert
                result.Should().HaveCount(1);
                result[0].CurrentValue.Should().Be(480);
                result[0].Percentage.Should().Be(0);
            }
            finally
            {
                connection.Dispose();
            }
        }

        private static (IDbContextFactory<AppDbContext> Factory, SqliteConnection Connection) CreateFactory()
        {
            SqliteConnection connection = new("DataSource=:memory:");
            connection.Open();

            DbContextOptions<AppDbContext> options = new DbContextOptionsBuilder<AppDbContext>()
                                                     .UseSqlite(connection)
                                                     .Options;

            using (AppDbContext setup = new(options))
            {
                setup.Database.EnsureCreated();
            }

            return (new TestDbContextFactory(options), connection);
        }

        private sealed class TestDbContextFactory(DbContextOptions<AppDbContext> options) : IDbContextFactory<AppDbContext>
        {
            public AppDbContext CreateDbContext()
            {
                return new AppDbContext(options);
            }

            public Task<AppDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
            {
                return Task.FromResult(new AppDbContext(options));
            }
        }
    }
}
