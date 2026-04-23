using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;

using FluentAssertions;

using Microsoft.Extensions.Logging.Abstractions;

using TibiaHuntMaster.Infrastructure.Http.Content.Assets;
using TibiaHuntMaster.Infrastructure.Http.Content.Creatures;
using TibiaHuntMaster.Infrastructure.Http.Content.HuntingPlaces;
using TibiaHuntMaster.Infrastructure.Http.Content.Items;

namespace TibiaHuntMaster.Tests.Content
{
    public sealed class ContentApiClientTests
    {
        [Fact]
        public async Task ItemClient_GetPagedItemAsync_ShouldUseExpectedRoute_AndDeserializeResponse()
        {
            RecordingHttpMessageHandler handler = new(request =>
            {
                request.RequestUri!.PathAndQuery.Should().Be("/api/v1/items?page=2&pageSize=50");

                return CreateJsonResponse(new
                {
                    page = 2,
                    pageSize = 50,
                    totalCount = 101,
                    items = new[]
                    {
                        new
                        {
                            id = 7,
                            name = "Golden Helmet",
                            categorySlug = "helmets",
                            categoryName = "Helmets",
                            primaryType = "Armor",
                            secondaryType = "Headgear",
                            objectClass = "Equipment",
                            wikiUrl = "https://example/items/7",
                            lastUpdated = "2026-04-20T10:15:30Z",
                            primaryImage = new
                            {
                                assetId = 9001,
                                storageKey = "items/9001.webp",
                                fileName = "9001.webp",
                                mimeType = "image/webp",
                                width = 32,
                                height = 32
                            }
                        }
                    }
                });
            });

            using HttpClient httpClient = CreateHttpClient(handler);
            ItemClient client = new(httpClient, NullLogger<ItemClient>.Instance);

            var response = await client.GetPagedItemAsync(2, 50);

            response.Page.Should().Be(2);
            response.PageSize.Should().Be(50);
            response.TotalCount.Should().Be(101);
            response.Items.Should().ContainSingle();
            response.Items[0].Id.Should().Be(7);
            response.Items[0].Name.Should().Be("Golden Helmet");
        }

        [Fact]
        public async Task ItemClient_GetItemsByCategoryAsync_ShouldTranslate404ToKeyNotFoundException()
        {
            RecordingHttpMessageHandler handler = new(_ => new HttpResponseMessage(HttpStatusCode.NotFound)
            {
                Content = new StringContent("missing")
            });

            using HttpClient httpClient = CreateHttpClient(handler);
            ItemClient client = new(httpClient, NullLogger<ItemClient>.Instance);

            Func<Task> act = async () => await client.GetItemsByCategoryAsync("missing");

            await act.Should().ThrowAsync<KeyNotFoundException>()
                .WithMessage("*missing*");
        }

        [Fact]
        public async Task CreaturesClient_GetSyncStatesByDateAsync_ShouldFormatIsoTimestampInQuery()
        {
            DateTimeOffset timestamp = new(2026, 4, 20, 10, 15, 30, TimeSpan.Zero);
            string expectedQueryValue = Uri.EscapeDataString(timestamp.ToString("O", CultureInfo.InvariantCulture));

            RecordingHttpMessageHandler handler = new(request =>
            {
                request.RequestUri!.PathAndQuery.Should().Be($"/api/v1/creatures/sync/by-date?time={expectedQueryValue}");

                return CreateJsonResponse(new[]
                {
                    new
                    {
                        id = 42,
                        lastUpdated = "2026-04-20T10:15:30Z",
                        lastSeenAt = "2026-04-20T10:00:00Z"
                    }
                });
            });

            using HttpClient httpClient = CreateHttpClient(handler);
            CreaturesClient client = new(httpClient, NullLogger<CreaturesClient>.Instance);

            var syncStates = await client.GetSyncStatesByDateAsync(timestamp);

            syncStates.Should().ContainSingle();
            syncStates[0].Id.Should().Be(42);
        }

        [Fact]
        public async Task HuntingPlacesClient_GetHuntingPlaceDetailsAsync_ShouldRejectInvalidId()
        {
            using HttpClient httpClient = CreateHttpClient(new RecordingHttpMessageHandler(_ => throw new InvalidOperationException("Should not be called.")));
            HuntingPlacesClient client = new(httpClient, NullLogger<HuntingPlacesClient>.Instance);

            Func<Task> act = async () => await client.GetHuntingPlaceDetailsAsync(0);

            await act.Should().ThrowAsync<ArgumentOutOfRangeException>();
        }

        [Fact]
        public async Task AssetsClient_DownloadAssetAsync_ShouldWriteBinaryPayloadToDestination()
        {
            byte[] payload = Encoding.UTF8.GetBytes("asset-binary-payload");

            RecordingHttpMessageHandler handler = new(request =>
            {
                request.RequestUri!.PathAndQuery.Should().Be("/api/v1/assets/1337");

                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new ByteArrayContent(payload)
                };
            });

            using HttpClient httpClient = CreateHttpClient(handler);
            AssetsClient client = new(httpClient, NullLogger<AssetsClient>.Instance);
            await using MemoryStream destination = new();

            await client.DownloadAssetAsync(1337, destination);

            destination.ToArray().Should().Equal(payload);
        }

        [Fact]
        public async Task ItemClient_GetItemDetailsAsync_ShouldRetryOn429AndEventuallySucceed()
        {
            int attempts = 0;

            RecordingHttpMessageHandler handler = new(_ =>
            {
                attempts++;

                if(attempts < 3)
                {
                    HttpResponseMessage retryResponse = new(HttpStatusCode.TooManyRequests)
                    {
                        Content = new StringContent("{\"error\":\"rate_limited\"}", Encoding.UTF8, "application/json")
                    };
                    retryResponse.Headers.RetryAfter = new System.Net.Http.Headers.RetryConditionHeaderValue(TimeSpan.Zero);
                    return retryResponse;
                }

                return CreateJsonResponse(new
                {
                    id = 77,
                    name = "Magic Plate Armor",
                    actualName = "Magic Plate Armor",
                    categorySlug = "armors",
                    categoryName = "Armors",
                    value = "90000",
                    lastUpdated = "2026-04-20T10:15:30Z",
                    images = Array.Empty<object>()
                });
            });

            using HttpClient httpClient = CreateHttpClient(handler);
            ItemClient client = new(httpClient, NullLogger<ItemClient>.Instance);

            var item = await client.GetItemDetailsAsync(77);

            attempts.Should().Be(3);
            item.Id.Should().Be(77);
            item.Name.Should().Be("Magic Plate Armor");
        }

        private static HttpClient CreateHttpClient(HttpMessageHandler handler)
        {
            return new HttpClient(handler)
            {
                BaseAddress = new Uri("https://tibiadata.bytewizards.de")
            };
        }

        private static HttpResponseMessage CreateJsonResponse(object payload)
        {
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
            };
        }

        private sealed class RecordingHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
        {
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                return Task.FromResult(responder(request));
            }
        }
    }
}
