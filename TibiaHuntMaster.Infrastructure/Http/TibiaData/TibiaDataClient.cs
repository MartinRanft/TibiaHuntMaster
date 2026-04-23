using System.Net;
using System.Net.Http.Json;

using Polly;
using Polly.Retry;

using TibiaHuntMaster.Core.Characters;
using TibiaHuntMaster.Core.Creatures;

namespace TibiaHuntMaster.Infrastructure.Http.TibiaData
{
    public sealed class TibiaDataClient
    {
        private const string BaseUrl = "https://api.tibiadata.com/v4/";
        internal static readonly TimeSpan DefaultRequestTimeout = TimeSpan.FromSeconds(30);
        private readonly HttpClient _httpClient;
        private readonly ResiliencePipeline<HttpResponseMessage> _pipeline;

        public TibiaDataClient(HttpClient httpClient)
            : this(httpClient, DefaultRequestTimeout)
        {
        }

        internal TibiaDataClient(HttpClient httpClient, TimeSpan requestTimeout)
        {
            _httpClient = httpClient;
            _httpClient.Timeout = Timeout.InfiniteTimeSpan; // Polly controls timeout

            _pipeline = new ResiliencePipelineBuilder<HttpResponseMessage>()
                        .AddRetry(new RetryStrategyOptions<HttpResponseMessage>
                        {
                            ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                                           .Handle<HttpRequestException>()
                                           .HandleResult(r => (int)r.StatusCode >= 500 || r.StatusCode == HttpStatusCode.TooManyRequests),
                            MaxRetryAttempts = 3,
                                           Delay = TimeSpan.FromSeconds(1),
                                           BackoffType = DelayBackoffType.Exponential,
                                           UseJitter = true
                        })
                        .AddTimeout(requestTimeout)
                        .Build();
        }

        internal async Task<TibiaDataCharacterResponse?> GetCharactersAsync(string name, CancellationToken ct = default)
        {
            string encodedName = Uri.EscapeDataString(name);
            return await GetWithRetryAsync<TibiaDataCharacterResponse>($"character/{encodedName}", ct);
        }

        internal async Task<TibiaDataCreaturesResponse?> GetCreaturesAsync(CancellationToken ct = default)
        {
            // Holt die Liste aller Kreaturen + Boosted
            return await GetWithRetryAsync<TibiaDataCreaturesResponse>("creatures", ct);
        }

        private async Task<T?> GetWithRetryAsync<T>(string endpoint, CancellationToken ct)
        {
            Uri uri = new(new Uri(BaseUrl), endpoint);

            using HttpResponseMessage response = await _pipeline.ExecuteAsync(async token =>
                await _httpClient.GetAsync(uri, token),
                ct);

            response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync<T>(ct);
        }
    }
}
