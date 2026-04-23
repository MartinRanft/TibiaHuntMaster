using System.Text.Json;

using FluentAssertions;

using Microsoft.Extensions.Logging.Abstractions;

using TibiaHuntMaster.Core.TibiaPal;
using TibiaHuntMaster.Infrastructure.Http.TibiaPal;

using Xunit.Abstractions;

namespace TibiaHuntMaster.Tests.TibiaPal
{
    public sealed class TibiaPal_E2E_Tests(ITestOutputHelper output)
    {
        [Fact(DisplayName = "🌐 LIVE: TibiaPal Scraper fetches Knights spots correctly")]
        [Trait("Category", "Online")]
        public async Task Live_Fetch_Knights_Returns_Spots()
        {
            // Arrange
            using HttpClient httpClient = new();
            // Wir nutzen einen echten HTTP Client ohne Mocking
            TibiaPalClient client = new(httpClient, NullLogger<TibiaPalClient>.Instance);

            // Act
            // Wir holen "Knights", da wir wissen, dass es dort viele Einträge gibt (z.B. Rotworms)
            List<TibiaPalHuntingSpot> results = await client.FetchHuntingSpotsAsync("knights");

            // Assert
            results.Should().NotBeNull();
            results.Should().HaveCountGreaterThan(5, "TibiaPal should list plenty of spots for knights");

            // Validierung eines beliebigen Eintrags auf Plausibilität
            // Wir suchen uns einen Spot, der wahrscheinlich existiert, oder nehmen einen Random
            TibiaPalHuntingSpot? randomSpot = results[Random.Shared.Next(results.Count)];

            randomSpot.Name.Should().NotBeNullOrWhiteSpace();
            randomSpot.Vocation.Should().Be("knights");

            // Prüfen ob EXP/Loot Info Text enthalten (z.B. "35k", "-5k")
            randomSpot.ExpInfo.Should().NotBeNullOrEmpty();
            randomSpot.LootInfo.Should().NotBeNullOrEmpty();

            // Weapon Type Check
            randomSpot.WeaponType.Should().NotBeNullOrEmpty("Weapon Type needs to be parsed from table");

            // Output für Developer (damit du siehst was ankommt)
            output.WriteLine("🎲 Random Picked Spot from Live Data:");
            output.WriteLine(JsonSerializer.Serialize(randomSpot,
                new JsonSerializerOptions
                {
                    WriteIndented = true
                }));

            output.WriteLine($"\nTotal Spots Found: {results.Count}");
        }
    }
}