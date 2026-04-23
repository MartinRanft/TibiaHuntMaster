using FluentAssertions;

using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

using TibiaHuntMaster.Core.Content.Creatures;
using TibiaHuntMaster.Core.Content.HuntingPlaces;
using TibiaHuntMaster.Core.Content.Items;
using TibiaHuntMaster.Core.Content.Loot;
using TibiaHuntMaster.Core.Content.Shared;
using TibiaHuntMaster.Core.Content.Sync;
using TibiaHuntMaster.Infrastructure.Data;
using TibiaHuntMaster.Infrastructure.Data.Entities.Content;
using TibiaHuntMaster.Infrastructure.Http.Content.Abstractions;
using TibiaHuntMaster.Infrastructure.Services.Content;
using TibiaHuntMaster.Infrastructure.Services.Content.Imports;

namespace TibiaHuntMaster.Tests.Content
{
    public sealed class ContentImportServiceTests
    {
        [Fact]
        public async Task ItemContentImportService_ShouldImportUpdateAndReinitializeItems()
        {
            await using TestDatabaseContext testDb = await TestDatabaseContext.CreateAsync();
            FakeItemsClient itemsClient = new();
            itemsClient.SetItem(CreateItemDetail(1, "Golden Helmet", "100", DateTimeOffset.Parse("2026-04-20T10:00:00Z")));

            ItemContentImportService service = new(itemsClient, testDb.Factory, new ContentProgressService(), NullLogger<ItemContentImportService>.Instance);

            var importResult = await service.ImportItemsAsync();

            importResult.Created.Should().Be(1);
            importResult.Updated.Should().Be(0);

            ItemEntity importedItem = await testDb.DbContext.Items.SingleAsync();
            importedItem.ContentId.Should().Be(1);
            importedItem.Name.Should().Be("Golden Helmet");
            importedItem.Value.Should().Be(100);

            itemsClient.SetItem(CreateItemDetail(1, "Golden Helmet", "250", DateTimeOffset.Parse("2026-04-20T11:00:00Z")));

            var updateResult = await service.UpdateItemsAsync();

            updateResult.Updated.Should().Be(1);

            await using AppDbContext verifyUpdatedItemsDb = await testDb.Factory.CreateDbContextAsync();
            ItemEntity updatedItem = await verifyUpdatedItemsDb.Items.SingleAsync();
            updatedItem.Value.Should().Be(250);
            updatedItem.SourceLastUpdatedAt.Should().Be(DateTimeOffset.Parse("2026-04-20T11:00:00Z"));

            var reinitializeResult = await service.ReInitializeItemsAsync();

            reinitializeResult.Created.Should().Be(1);
            (await testDb.DbContext.Items.CountAsync()).Should().Be(1);
        }

        [Fact]
        public async Task ItemContentImportService_ShouldFallbackToNpcValue_WhenPrimaryValueIsMissing()
        {
            await using TestDatabaseContext testDb = await TestDatabaseContext.CreateAsync();
            FakeItemsClient itemsClient = new();
            ItemDetailsResponse item = CreateItemDetail(2, "Giant Shimmering Pearl", string.Empty, DateTimeOffset.Parse("2026-04-20T10:00:00Z"));
            item.NpcValue = "3000";
            itemsClient.SetItem(item);

            ItemContentImportService service = new(itemsClient, testDb.Factory, new ContentProgressService(), NullLogger<ItemContentImportService>.Instance);

            await service.ImportItemsAsync();

            ItemEntity importedItem = await testDb.DbContext.Items.SingleAsync(x => x.ContentId == 2);
            importedItem.Value.Should().Be(3000);
            importedItem.NpcValue.Should().Be(3000);
        }

        [Fact]
        public async Task CreatureContentImportService_ShouldImportUpdateAndReinitializeCreatures()
        {
            await using TestDatabaseContext testDb = await TestDatabaseContext.CreateAsync();
            FakeCreaturesClient creaturesClient = new();
            creaturesClient.SetCreature(CreateCreatureDetail(10, "Dragon", 1000, 700, "Grr", "gold coin", "1-10", "common", DateTimeOffset.Parse("2026-04-20T10:00:00Z")));

            CreatureContentImportService service = new(creaturesClient, testDb.Factory, new ContentProgressService(), NullLogger<CreatureContentImportService>.Instance);

            var importResult = await service.ImportCreaturesAsync();

            importResult.Created.Should().Be(1);

            await using AppDbContext verifyImportedCreaturesDb = await testDb.Factory.CreateDbContextAsync();
            CreatureEntity creature = await verifyImportedCreaturesDb.Creatures
                .Include(x => x.Loot)
                .Include(x => x.Sounds)
                .SingleAsync();

            creature.ContentId.Should().Be(10);
            creature.Name.Should().Be("Dragon");
            creature.Loot.Should().ContainSingle(x => x.ItemName == "gold coin");
            creature.Sounds.Should().ContainSingle(x => x.Text == "Grr");

            creaturesClient.SetCreature(CreateCreatureDetail(10, "Dragon", 1200, 900, "Rooar", "platinum coin", "2-4", "uncommon", DateTimeOffset.Parse("2026-04-20T11:00:00Z")));

            var updateResult = await service.UpdateCreaturesAsync();

            updateResult.Updated.Should().Be(1);

            await using AppDbContext verifyUpdatedCreaturesDb = await testDb.Factory.CreateDbContextAsync();
            CreatureEntity updatedCreature = await verifyUpdatedCreaturesDb.Creatures
                .Include(x => x.Loot)
                .Include(x => x.Sounds)
                .SingleAsync();

            updatedCreature.Hp.Should().Be(1200);
            updatedCreature.Exp.Should().Be(900);
            updatedCreature.Loot.Should().ContainSingle(x => x.ItemName == "platinum coin");
            updatedCreature.Sounds.Should().ContainSingle(x => x.Text == "Rooar");

            var reinitializeResult = await service.ReInitializeCreaturesAsync();

            reinitializeResult.Created.Should().Be(1);
            (await testDb.DbContext.Creatures.CountAsync()).Should().Be(1);
        }

        [Fact]
        public async Task HuntingPlaceContentImportService_ShouldImportUpdateAndReinitializeHuntingPlaces()
        {
            await using TestDatabaseContext testDb = await TestDatabaseContext.CreateAsync();
            FakeHuntingPlacesClient huntingPlacesClient = new();
            huntingPlacesClient.SetHuntingPlace(CreateHuntingPlaceDetail(
                20,
                "Dragon Lair",
                "Dragon Lair",
                "Dragon",
                "Entry Hall",
                DateTimeOffset.Parse("2026-04-20T10:00:00Z")));

            testDb.DbContext.Creatures.Add(new CreatureEntity
            {
                ContentId = 999,
                Name = "Dragon",
                ActualName = "Dragon",
                ContentHash = "dragon",
                SourceLastUpdatedAt = DateTimeOffset.Parse("2026-04-20T09:00:00Z"),
                UpdatedAt = DateTimeOffset.UtcNow
            });
            await testDb.DbContext.SaveChangesAsync();

            HuntingPlaceContentImportService service = new(huntingPlacesClient, testDb.Factory, new ContentProgressService(), NullLogger<HuntingPlaceContentImportService>.Instance);

            var importResult = await service.ImportHuntingPlacesAsync();

            importResult.Created.Should().Be(1);

            await using AppDbContext verifyImportedPlacesDb = await testDb.Factory.CreateDbContextAsync();
            HuntingPlaceEntity place = await verifyImportedPlacesDb.HuntingPlaces
                .Include(x => x.Creatures)
                .Include(x => x.LowerLevels)
                .SingleAsync();

            place.ContentId.Should().Be(20);
            place.Name.Should().Be("Dragon Lair");
            place.Creatures.Should().ContainSingle(x => x.CreatureName == "Dragon");
            place.LowerLevels.Should().ContainSingle(x => x.AreaName == "Entry Hall");

            huntingPlacesClient.SetHuntingPlace(CreateHuntingPlaceDetail(
                20,
                "Dragon Lair",
                "Dragon Lair Updated",
                "Dragon Lord",
                "Lower Floor",
                DateTimeOffset.Parse("2026-04-20T11:00:00Z")));

            var updateResult = await service.UpdateHuntingPlacesAsync();

            updateResult.Updated.Should().Be(1);

            await using AppDbContext verifyUpdatedPlacesDb = await testDb.Factory.CreateDbContextAsync();
            HuntingPlaceEntity updatedPlace = await verifyUpdatedPlacesDb.HuntingPlaces
                .Include(x => x.Creatures)
                .Include(x => x.LowerLevels)
                .SingleAsync();

            updatedPlace.Title.Should().Be("Dragon Lair Updated");
            updatedPlace.Creatures.Should().ContainSingle(x => x.CreatureName == "Dragon Lord");
            updatedPlace.LowerLevels.Should().ContainSingle(x => x.AreaName == "Lower Floor");

            var reinitializeResult = await service.ReInitializeHuntingPlacesAsync();

            reinitializeResult.Created.Should().Be(1);
            (await testDb.DbContext.HuntingPlaces.CountAsync()).Should().Be(1);
        }

        private static ItemDetailsResponse CreateItemDetail(int id, string name, string value, DateTimeOffset lastUpdated)
        {
            return new ItemDetailsResponse
            {
                Id = id,
                Name = name,
                ActualName = name,
                ValueValue = value,
                CategorySlug = "helmets",
                CategoryName = "Helmets",
                LastUpdated = lastUpdated,
                Images =
                [
                    new ItemImageResponse
                    {
                        AssetId = id * 100,
                        StorageKey = $"items/{id}.webp",
                        FileName = $"{id}.webp",
                        MimeType = "image/webp"
                    }
                ]
            };
        }

        private static CreatureDetailsResponse CreateCreatureDetail(
            int id,
            string name,
            int hitpoints,
            long experience,
            string sound,
            string lootItem,
            string lootRaw,
            string rarity,
            DateTimeOffset lastUpdated)
        {
            return new CreatureDetailsResponse
            {
                Id = id,
                Name = name,
                Hitpoints = hitpoints,
                Experience = experience,
                LastUpdated = lastUpdated,
                StructuredData = new CreatureStructuredDataResponse
                {
                    Template = "Creature",
                    Infobox = new CreatureInfoboxResponse
                    {
                        ActualName = name,
                        Sounds = sound
                    }
                },
                LootStatistics =
                [
                    new LootStatisticEntryResponse
                    {
                        ItemName = lootItem,
                        Raw = lootRaw,
                        Rarity = rarity
                    }
                ]
            };
        }

        private static HuntingPlaceDetailsResponse CreateHuntingPlaceDetail(
            int id,
            string name,
            string title,
            string creatureName,
            string lowerLevelArea,
            DateTimeOffset lastUpdated)
        {
            return new HuntingPlaceDetailsResponse
            {
                Id = id,
                Name = name,
                Title = title,
                City = "Kazordoon",
                Vocation = "Knight",
                LastUpdated = lastUpdated,
                Creatures =
                [
                    new HuntingPlaceCreatureResponse
                    {
                        CreatureId = 999,
                        Name = creatureName
                    }
                ],
                LowerLevels =
                [
                    new HuntingPlaceAreaRecommendationResponse
                    {
                        AreaName = lowerLevelArea,
                        LevelKnights = "120"
                    }
                ]
            };
        }

        private sealed class FakeItemsClient : IItemsClient
        {
            private readonly Dictionary<int, ItemDetailsResponse> _items = [];

            public void SetItem(ItemDetailsResponse item)
            {
                _items[item.Id] = item;
            }

            public Task<List<string>> GetItemNamesAsync(CancellationToken cancellationToken = default)
            {
                return Task.FromResult(_items.Values.Select(x => x.Name).ToList());
            }

            public Task<List<string>> GetItemCategoriesAsync(CancellationToken cancellationToken = default)
            {
                return Task.FromResult(_items.Values.Select(x => x.CategoryName ?? string.Empty).Distinct().ToList());
            }

            public Task<PagedResponseOfItemListItemResponse> GetPagedItemAsync(int page = 1, int pageSize = 100, CancellationToken cancellationToken = default)
            {
                List<ItemListItemResponse> items = _items.Values
                    .OrderBy(x => x.Id)
                    .Select(x => new ItemListItemResponse
                    {
                        Id = x.Id,
                        Name = x.Name,
                        CategorySlug = x.CategorySlug,
                        CategoryName = x.CategoryName,
                        LastUpdated = x.LastUpdated
                    })
                    .ToList();

                return Task.FromResult(new PagedResponseOfItemListItemResponse
                {
                    Page = page,
                    PageSize = pageSize,
                    TotalCount = items.Count,
                    Items = items
                });
            }

            public Task<ItemDetailsResponse> GetItemDetailsAsync(int id, CancellationToken cancellationToken = default)
            {
                return Task.FromResult(_items[id]);
            }

            public Task<List<ItemListItemResponse>> GetItemsByCategoryAsync(string category, CancellationToken cancellationToken = default)
            {
                List<ItemListItemResponse> items = _items.Values
                    .Where(x => string.Equals(x.CategorySlug, category, StringComparison.OrdinalIgnoreCase))
                    .Select(x => new ItemListItemResponse { Id = x.Id, Name = x.Name })
                    .ToList();

                return Task.FromResult(items);
            }

            public Task<List<SyncStateResponse>> GetSyncStatesAsync(CancellationToken cancellationToken = default)
            {
                return Task.FromResult(_items.Values
                    .Select(x => new SyncStateResponse
                    {
                        Id = x.Id,
                        LastUpdated = x.LastUpdated,
                        LastSeenAt = x.LastSeenAt
                    })
                    .ToList());
            }

            public Task<List<SyncStateResponse>> GetSyncStatesByDateAsync(DateTimeOffset date, CancellationToken cancellationToken = default)
            {
                return Task.FromResult(_items.Values
                    .Where(x => x.LastUpdated >= date)
                    .Select(x => new SyncStateResponse
                    {
                        Id = x.Id,
                        LastUpdated = x.LastUpdated,
                        LastSeenAt = x.LastSeenAt
                    })
                    .ToList());
            }
        }

        private sealed class FakeCreaturesClient : ICreaturesClient
        {
            private readonly Dictionary<int, CreatureDetailsResponse> _creatures = [];

            public void SetCreature(CreatureDetailsResponse creature)
            {
                _creatures[creature.Id] = creature;
            }

            public Task<PagedResponseOfCreatureListItemResponse> GetPagedCreatureAsync(int page = 1, int pageSize = 100, CancellationToken cancellationToken = default)
            {
                List<CreatureListItemResponse> creatures = _creatures.Values
                    .OrderBy(x => x.Id)
                    .Select(x => new CreatureListItemResponse
                    {
                        Id = x.Id,
                        Name = x.Name,
                        Hitpoints = x.Hitpoints,
                        Experience = x.Experience,
                        LastUpdated = x.LastUpdated
                    })
                    .ToList();

                return Task.FromResult(new PagedResponseOfCreatureListItemResponse
                {
                    Page = page,
                    PageSize = pageSize,
                    TotalCount = creatures.Count,
                    Creatures = creatures
                });
            }

            public Task<List<string>> GetCreatureNamesAsync(CancellationToken cancellationToken = default)
            {
                return Task.FromResult(_creatures.Values.Select(x => x.Name).ToList());
            }

            public Task<CreatureDetailsResponse> GetCreatureDetailsAsync(int id, CancellationToken cancellationToken = default)
            {
                return Task.FromResult(_creatures[id]);
            }

            public Task<LootStatisticDetailsResponse> GetCreatureLootStatisticsAsync(int id, CancellationToken cancellationToken = default)
            {
                CreatureDetailsResponse creature = _creatures[id];
                return Task.FromResult(new LootStatisticDetailsResponse
                {
                    CreatureId = creature.Id,
                    CreatureName = creature.Name,
                    LootStatistics = creature.LootStatistics,
                    LastUpdated = creature.LastUpdated
                });
            }

            public Task<List<SyncStateResponse>> GetSyncStatesAsync(CancellationToken cancellationToken = default)
            {
                return Task.FromResult(_creatures.Values
                    .Select(x => new SyncStateResponse
                    {
                        Id = x.Id,
                        LastUpdated = x.LastUpdated
                    })
                    .ToList());
            }

            public Task<List<SyncStateResponse>> GetSyncStatesByDateAsync(DateTimeOffset date, CancellationToken cancellationToken = default)
            {
                return Task.FromResult(_creatures.Values
                    .Where(x => x.LastUpdated >= date)
                    .Select(x => new SyncStateResponse
                    {
                        Id = x.Id,
                        LastUpdated = x.LastUpdated
                    })
                    .ToList());
            }
        }

        private sealed class FakeHuntingPlacesClient : IHuntingPlacesClient
        {
            private readonly Dictionary<int, HuntingPlaceDetailsResponse> _places = [];

            public void SetHuntingPlace(HuntingPlaceDetailsResponse place)
            {
                _places[place.Id] = place;
            }

            public Task<List<HuntingPlaceListItemResponse>> GetHuntingPlacesAsync(CancellationToken cancellationToken = default)
            {
                return Task.FromResult(_places.Values
                    .OrderBy(x => x.Id)
                    .Select(x => new HuntingPlaceListItemResponse
                    {
                        Id = x.Id,
                        Name = x.Name,
                        Title = x.Title,
                        Summary = x.Summary,
                        City = x.City,
                        Location = x.Location,
                        Vocation = x.Vocation,
                        WikiUrl = x.WikiUrl,
                        LastUpdated = x.LastUpdated
                    })
                    .ToList());
            }

            public Task<HuntingPlaceDetailsResponse> GetHuntingPlaceDetailsAsync(int id, CancellationToken cancellationToken = default)
            {
                return Task.FromResult(_places[id]);
            }

            public Task<List<SyncStateResponse>> GetSyncStatesAsync(CancellationToken cancellationToken = default)
            {
                return Task.FromResult(_places.Values
                    .Select(x => new SyncStateResponse
                    {
                        Id = x.Id,
                        LastUpdated = x.LastUpdated,
                        LastSeenAt = x.LastSeenAt
                    })
                    .ToList());
            }

            public Task<List<SyncStateResponse>> GetSyncStatesByDateAsync(DateTimeOffset date, CancellationToken cancellationToken = default)
            {
                return Task.FromResult(_places.Values
                    .Where(x => x.LastUpdated >= date)
                    .Select(x => new SyncStateResponse
                    {
                        Id = x.Id,
                        LastUpdated = x.LastUpdated,
                        LastSeenAt = x.LastSeenAt
                    })
                    .ToList());
            }
        }

        private sealed class TestDatabaseContext : IAsyncDisposable
        {
            private readonly SqliteConnection _connection;

            private TestDatabaseContext(SqliteConnection connection, AppDbContext dbContext, IDbContextFactory<AppDbContext> factory)
            {
                _connection = connection;
                DbContext = dbContext;
                Factory = factory;
            }

            public AppDbContext DbContext { get; }

            public IDbContextFactory<AppDbContext> Factory { get; }

            public static async Task<TestDatabaseContext> CreateAsync()
            {
                SqliteConnection connection = new("DataSource=:memory:");
                await connection.OpenAsync();

                DbContextOptions<AppDbContext> options = new DbContextOptionsBuilder<AppDbContext>()
                    .UseSqlite(connection)
                    .Options;

                AppDbContext dbContext = new(options);
                await dbContext.Database.EnsureCreatedAsync();

                return new TestDatabaseContext(connection, dbContext, new TestDbContextFactory(connection));
            }

            public async ValueTask DisposeAsync()
            {
                await DbContext.DisposeAsync();
                await _connection.DisposeAsync();
            }
        }

        private sealed class TestDbContextFactory(SqliteConnection connection) : IDbContextFactory<AppDbContext>
        {
            public AppDbContext CreateDbContext()
            {
                DbContextOptions<AppDbContext> options = new DbContextOptionsBuilder<AppDbContext>()
                    .UseSqlite(connection)
                    .Options;

                return new AppDbContext(options);
            }

            public Task<AppDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
            {
                return Task.FromResult(CreateDbContext());
            }
        }
    }
}
