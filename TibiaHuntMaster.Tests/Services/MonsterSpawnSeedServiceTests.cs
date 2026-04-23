using System.IO.Compression;
using System.Text;

using FluentAssertions;

using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

using TibiaHuntMaster.Infrastructure.Data;
using TibiaHuntMaster.Infrastructure.Data.Entities.Content;
using TibiaHuntMaster.Infrastructure.Services.System;
using CreatureEntitys = TibiaHuntMaster.Infrastructure.Data.Entities.Content.CreatureEntity;

namespace TibiaHuntMaster.Tests.Services
{
    public sealed class MonsterSpawnSeedServiceTests
    {
        private const string SpawnResourceName = "TibiaHuntMaster.Infrastructure.SeedData.Spawns.map-spawn-v2.json.gz";
        private const string MonsterResourceName = "TibiaHuntMaster.Infrastructure.SeedData.Spawns.monsters.json.gz";

        [Fact]
        public async Task EnsureSpawnsSeededAsync_ShouldSeedAndRelink_WhenCreaturesArriveLater()
        {
            await using SqliteConnection connection = new("DataSource=:memory:");
            await connection.OpenAsync();

            DbContextOptions<AppDbContext> options = new DbContextOptionsBuilder<AppDbContext>()
                                                     .UseSqlite(connection)
                                                     .Options;

            TestDbContextFactory factory = new(options);

            await using (AppDbContext setupDb = await factory.CreateDbContextAsync())
            {
                await setupDb.Database.EnsureCreatedAsync();

                setupDb.Creatures.Add(new CreatureEntitys
                {
                    ContentId = 1,
                    Name = "Dragon",
                    ActualName = "Dragon",
                    ContentHash = "dragon-hash",
                    UpdatedAt = DateTimeOffset.UtcNow
                });

                await setupDb.SaveChangesAsync();
            }

            Dictionary<string, byte[]> resources = BuildTestResources();

            MonsterSpawnSeedService service = new(
                factory,
                NullLogger<MonsterSpawnSeedService>.Instance,
                name => new MemoryStream(resources[name], writable: false));

            await service.EnsureSpawnsSeededAsync();

            await using (AppDbContext verifyDb = await factory.CreateDbContextAsync())
            {
                int coordinates = await verifyDb.MonsterSpawnCoordinates.CountAsync();
                int links = await verifyDb.MonsterSpawnCreatureLinks.CountAsync();

                coordinates.Should().Be(4);
                links.Should().Be(4);

                MonsterSpawnCreatureLinkEntity dragonLink = await verifyDb.MonsterSpawnCreatureLinks.SingleAsync(x => x.MonsterName == "Dragon");
                dragonLink.CreatureId.Should().NotBeNull();

                MonsterSpawnCreatureLinkEntity oldBearLink = await verifyDb.MonsterSpawnCreatureLinks.SingleAsync(x => x.MonsterName == "Old Bear");
                oldBearLink.CreatureId.Should().BeNull();
            }

            await using (AppDbContext updateDb = await factory.CreateDbContextAsync())
            {
                updateDb.Creatures.Add(new CreatureEntitys
                {
                    ContentId = 2,
                    Name = "Bear (Nostalgia)",
                    ActualName = "Bear (Nostalgia)",
                    ContentHash = "bear-nostalgia-hash",
                    UpdatedAt = DateTimeOffset.UtcNow
                });

                await updateDb.SaveChangesAsync();
            }

            await service.EnsureSpawnsSeededAsync();

            await using (AppDbContext verifyRelinkDb = await factory.CreateDbContextAsync())
            {
                int coordinates = await verifyRelinkDb.MonsterSpawnCoordinates.CountAsync();
                int links = await verifyRelinkDb.MonsterSpawnCreatureLinks.CountAsync();

                coordinates.Should().Be(4);
                links.Should().Be(4);

                MonsterSpawnCreatureLinkEntity oldBearLink = await verifyRelinkDb.MonsterSpawnCreatureLinks.SingleAsync(x => x.MonsterName == "Old Bear");
                oldBearLink.CreatureId.Should().NotBeNull();
            }
        }

        private static Dictionary<string, byte[]> BuildTestResources()
        {
            string spawnJson =
                """
                {
                  "spawns": [
                    {
                      "centerx": "32000",
                      "centery": "32000",
                      "centerz": "7",
                      "radius": "2",
                      "monsters": [
                        { "name": "Dragon", "x": "0", "y": "0", "z": "7", "spawntime": "90" },
                        { "name": "Old Bear", "x": "1", "y": "0", "z": "7", "spawntime": "90" },
                        { "name": "Old Bear", "x": "1", "y": "0", "z": "7", "spawntime": "90" }
                      ]
                    },
                    {
                      "centerx": "32000",
                      "centery": "32000",
                      "centerz": "7",
                      "radius": "2",
                      "monsters": [
                        { "name": "Unknown Thing", "x": "2", "y": "2", "z": "7", "spawntime": "60" },
                        { "name": "Nomad Female", "x": "-1", "y": "1", "z": "7", "spawntime": "120" }
                      ]
                    }
                  ]
                }
                """;

            string monstersJson =
                """
                [
                  { "name": "Dragon" },
                  { "name": "Old Bear" },
                  { "name": "Unknown Thing" },
                  { "name": "Nomad Female" }
                ]
                """;

            return new Dictionary<string, byte[]>(StringComparer.Ordinal)
            {
                [SpawnResourceName] = Gzip(spawnJson),
                [MonsterResourceName] = Gzip(monstersJson)
            };
        }

        private static byte[] Gzip(string content)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(content);
            using MemoryStream output = new();
            using (GZipStream gzip = new(output, CompressionLevel.SmallestSize, leaveOpen: true))
            {
                gzip.Write(bytes, 0, bytes.Length);
            }

            return output.ToArray();
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
