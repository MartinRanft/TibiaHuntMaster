using System.Linq;
using System.Net;

using FluentAssertions;

using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

using TibiaHuntMaster.Infrastructure.Data;
using TibiaHuntMaster.Infrastructure.Data.Entities.Content;
using TibiaHuntMaster.Infrastructure.Http.TibiaPal;
using TibiaHuntMaster.Infrastructure.Services.TibiaPal;
using HuntingPlaceEntitys = TibiaHuntMaster.Infrastructure.Data.Entities.Content.HuntingPlaceEntity;

namespace TibiaHuntMaster.Tests.TibiaPal
{
    public sealed class TibiaPalServiceTests
    {
        [Fact(DisplayName = "🔗 Service: filters, orders and enriches TibiaPal spots with wiki data")]
        public async Task GetRecommendationsAsync_Enriches_And_Filters_By_Level()
        {
            using SqliteConnection conn = new("DataSource=:memory:");
            conn.Open();
            DbContextOptions<AppDbContext> options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(conn).Options;
            TestDbContextFactory factory = new(options);

            using (AppDbContext db = factory.CreateDbContext())
            {
                db.Database.EnsureCreated();
                db.HuntingPlaces.Add(new HuntingPlaceEntitys
                {
                    ContentId = 1,
                    Name = "Rotworms Liberty Bay",
                    Image = "http://tibia.wiki/rotworm.jpg",
                });
                await db.SaveChangesAsync();
            }

            string fakeHtml = """
                              <html>
                                <body>
                                  <table id="knight">
                                    <tr>
                                      <th>Lvl</th><th>Name</th><th>Exp</th><th>Loot</th><th>Type</th>
                                    </tr>
                                    <tr>
                                      <td>60</td>
                                      <td><a href="https://youtu.be/too-low">Rotworms Liberty Bay</a></td>
                                      <td>100k</td>
                                      <td>20k</td>
                                      <td>Physical</td>
                                    </tr>
                                    <tr>
                                      <td>80+</td>
                                      <td><a href="https://youtu.be/rotworms">Rotworms Liberty Bay</a></td>
                                      <td>150k</td>
                                      <td>35k</td>
                                      <td>Physical</td>
                                    </tr>
                                    <tr>
                                      <td>110</td>
                                      <td>Dragon Lair Darashia</td>
                                      <td>220k</td>
                                      <td>70k</td>
                                      <td>Physical</td>
                                    </tr>
                                  </table>
                                </body>
                              </html>
                              """;

            using FakeHttpHandler httpHandler = new(fakeHtml);
            using HttpClient httpClient = new(httpHandler);
            TibiaPalClient client = new(httpClient, NullLogger<TibiaPalClient>.Instance);
            TibiaPalService service = new(client, factory);

            List<EnrichedHuntingSpot> results = await service.GetRecommendationsAsync("knights", currentLevel: 100);

            results.Should().HaveCount(2, "spots below 80% of current level must be filtered out");
            results.Select(r => r.TibiaPalData.MinLevel).Should().ContainInOrder(80, 110);
            results.Should().OnlyContain(r => r.TibiaPalData.MinLevel >= 80);

            EnrichedHuntingSpot linked = results[0];
            linked.HasWikiData.Should().BeTrue();
            linked.DbName.Should().Be("Rotworms Liberty Bay");
            linked.DbId.Should().NotBeNull();
            linked.ImageUrl.Should().Be("http://tibia.wiki/rotworm.jpg");
            linked.TibiaPalData.YouTubeUrl.Should().Be("https://youtu.be/rotworms");

            EnrichedHuntingSpot unmatched = results[1];
            unmatched.HasWikiData.Should().BeFalse();
            unmatched.DbId.Should().BeNull();
            unmatched.DbName.Should().BeNull();
            unmatched.ImageUrl.Should().BeNull();
            unmatched.TibiaPalData.Name.Should().Be("Dragon Lair Darashia");
        }

        // Helper Factory
        private class TestDbContextFactory(DbContextOptions<AppDbContext> options) : IDbContextFactory<AppDbContext>
        {
            public AppDbContext CreateDbContext()
            {
                return new AppDbContext(options);
            }
            public Task<AppDbContext> CreateDbContextAsync(CancellationToken ct = default)
            {
                return Task.FromResult(new AppDbContext(options));
            }
        }

        private class FakeHttpHandler(string htmlContent) : HttpMessageHandler
        {
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(htmlContent)
                });
            }
        }
    }
}
