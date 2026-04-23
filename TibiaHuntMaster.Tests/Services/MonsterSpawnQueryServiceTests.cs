using FluentAssertions;

using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

using TibiaHuntMaster.Infrastructure.Data;
using TibiaHuntMaster.Infrastructure.Data.Entities.Content;
using TibiaHuntMaster.Infrastructure.Services.Map;

namespace TibiaHuntMaster.Tests.Services
{
    public sealed class MonsterSpawnQueryServiceTests
    {
        [Fact]
        public async Task GetSpawnsInBoundsAsync_ShouldReturnOnlyVisibleZAndBounds()
        {
            await using SqliteConnection connection = new("DataSource=:memory:");
            await connection.OpenAsync();

            DbContextOptions<AppDbContext> options = new DbContextOptionsBuilder<AppDbContext>()
                                                     .UseSqlite(connection)
                                                     .Options;

            TestDbContextFactory factory = new(options);

            await using (AppDbContext db = await factory.CreateDbContextAsync())
            {
                await db.Database.EnsureCreatedAsync();

                MonsterSpawnCoordinateEntity visible = new()
                {
                    X = 32010,
                    Y = 32020,
                    Z = 7
                };

                MonsterSpawnCoordinateEntity hiddenByBounds = new()
                {
                    X = 33000,
                    Y = 33000,
                    Z = 7
                };

                MonsterSpawnCoordinateEntity hiddenByZ = new()
                {
                    X = 32015,
                    Y = 32025,
                    Z = 8
                };

                db.MonsterSpawnCoordinates.AddRange(visible, hiddenByBounds, hiddenByZ);
                db.MonsterSpawnCreatureLinks.AddRange(
                    new MonsterSpawnCreatureLinkEntity
                    {
                        Coordinate = visible,
                        MonsterName = "Dragon",
                        SpawnTimeSeconds = 90
                    },
                    new MonsterSpawnCreatureLinkEntity
                    {
                        Coordinate = hiddenByBounds,
                        MonsterName = "Hydra",
                        SpawnTimeSeconds = 60
                    },
                    new MonsterSpawnCreatureLinkEntity
                    {
                        Coordinate = hiddenByZ,
                        MonsterName = "Ghoul",
                        SpawnTimeSeconds = 30
                    });

                await db.SaveChangesAsync();
            }

            MonsterSpawnQueryService service = new(factory);

            IReadOnlyList<Core.Map.Map.MonsterSpawnMarker> result = await service.GetSpawnsInBoundsAsync(
                minX: 32000,
                minY: 32000,
                maxX: 32100,
                maxY: 32100,
                z: 7);

            result.Should().HaveCount(1);
            result[0].MonsterName.Should().Be("Dragon");
            result[0].X.Should().Be(32010);
            result[0].Y.Should().Be(32020);
            result[0].Z.Should().Be(7);
        }

        [Fact]
        public async Task GetSpawnsInBoundsAsync_ShouldApplyMonsterNameFilter()
        {
            await using SqliteConnection connection = new("DataSource=:memory:");
            await connection.OpenAsync();

            DbContextOptions<AppDbContext> options = new DbContextOptionsBuilder<AppDbContext>()
                                                     .UseSqlite(connection)
                                                     .Options;

            TestDbContextFactory factory = new(options);

            await using (AppDbContext db = await factory.CreateDbContextAsync())
            {
                await db.Database.EnsureCreatedAsync();

                MonsterSpawnCoordinateEntity coordinate = new()
                {
                    X = 32010,
                    Y = 32020,
                    Z = 7
                };

                db.MonsterSpawnCoordinates.Add(coordinate);
                db.MonsterSpawnCreatureLinks.AddRange(
                    new MonsterSpawnCreatureLinkEntity
                    {
                        Coordinate = coordinate,
                        MonsterName = "Dragon"
                    },
                    new MonsterSpawnCreatureLinkEntity
                    {
                        Coordinate = coordinate,
                        MonsterName = "Hydra"
                    });

                await db.SaveChangesAsync();
            }

            MonsterSpawnQueryService service = new(factory);

            IReadOnlyList<Core.Map.Map.MonsterSpawnMarker> filtered = await service.GetSpawnsInBoundsAsync(
                minX: 32000,
                minY: 32000,
                maxX: 32100,
                maxY: 32100,
                z: 7,
                monsterName: "dragon");

            filtered.Should().HaveCount(1);
            filtered[0].MonsterName.Should().Be("Dragon");
        }

        [Fact]
        public async Task SearchMonsterNamesAsync_ShouldReturnPrefixMatchesFirst()
        {
            await using SqliteConnection connection = new("DataSource=:memory:");
            await connection.OpenAsync();

            DbContextOptions<AppDbContext> options = new DbContextOptionsBuilder<AppDbContext>()
                                                     .UseSqlite(connection)
                                                     .Options;

            TestDbContextFactory factory = new(options);

            await using (AppDbContext db = await factory.CreateDbContextAsync())
            {
                await db.Database.EnsureCreatedAsync();

                MonsterSpawnCoordinateEntity coordinate = new()
                {
                    X = 32010,
                    Y = 32020,
                    Z = 7
                };

                db.MonsterSpawnCoordinates.Add(coordinate);
                db.MonsterSpawnCreatureLinks.AddRange(
                    new MonsterSpawnCreatureLinkEntity
                    {
                        Coordinate = coordinate,
                        MonsterName = "Dragon"
                    },
                    new MonsterSpawnCreatureLinkEntity
                    {
                        Coordinate = coordinate,
                        MonsterName = "Dragon Lord"
                    },
                    new MonsterSpawnCreatureLinkEntity
                    {
                        Coordinate = coordinate,
                        MonsterName = "Hydra Dragon"
                    });

                await db.SaveChangesAsync();
            }

            MonsterSpawnQueryService service = new(factory);

            IReadOnlyList<string> result = await service.SearchMonsterNamesAsync("drag", limit: 10);

            result.Should().ContainInOrder("Dragon", "Dragon Lord");
            result.Should().Contain("Hydra Dragon");
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
