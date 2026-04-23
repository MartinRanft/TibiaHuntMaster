using System.Net;
using System.Text.RegularExpressions;

using HtmlAgilityPack;

using Microsoft.Extensions.Logging;

using Polly;
using Polly.Retry;

using TibiaHuntMaster.Core.TibiaPal;

namespace TibiaHuntMaster.Infrastructure.Http.TibiaPal
{
    public sealed partial class TibiaPalClient(HttpClient httpClient, ILogger<TibiaPalClient> logger)
    {
        private const string BaseUrl = "https://tibiapal.com/hunting";

        // Pipeline mit Logging im Retry-Fall
        private readonly ResiliencePipeline<HttpResponseMessage> _pipeline = new ResiliencePipelineBuilder<HttpResponseMessage>()
                                                                             .AddRetry(new RetryStrategyOptions<HttpResponseMessage>
                                                                             {
                                                                                 ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                                                                                                .Handle<HttpRequestException>()
                                                                                                .HandleResult(r => (int)r.StatusCode >= 500 || r.StatusCode == HttpStatusCode.TooManyRequests),
                                                                                 MaxRetryAttempts = 3,
                                                                                 Delay = TimeSpan.FromSeconds(4),
                                                                                 BackoffType = DelayBackoffType.Exponential,
                                                                                 UseJitter = true,
                                                                                 OnRetry = args =>
                                                                                 {
                                                                                     logger.LogWarning("TibiaPal Retry #{Count} ({Reason})",
                                                                                         args.AttemptNumber,
                                                                                         args.Outcome.Exception?.Message ?? args.Outcome.Result?.StatusCode.ToString());
                                                                                     return ValueTask.CompletedTask;
                                                                                 }
                                                                             })
                                                                             .AddTimeout(TimeSpan.FromSeconds(30))
                                                                             .Build();

        internal async Task<List<TibiaPalHuntingSpot>> FetchHuntingSpotsAsync(string vocation, CancellationToken ct = default)
        {
            // Simple Switch Expression (C# 8+)
            string targetId = vocation.ToLowerInvariant().Trim() switch
            {
                "knight" or "knights" => "knight",
                "paladin" or "paladins" => "paladin",
                "mage" or "mages" or "druid" or "sorcerer" => "druid-sorcerer",
                "duo" => "duos",
                "team" => "teamhunts",
                _ => "knight"
            };

            logger.LogInformation("Fetching TibiaPal data for: {Vocation} (Tab ID: {Tab})", vocation, targetId);

            try
            {
                // Resilience Pipeline Execution
                using HttpResponseMessage response = await _pipeline.ExecuteAsync(async token =>
                    {
                        // Request neu erstellen für jeden Versuch (HttpRequestMessage ist One-Time-Use)
                        using HttpRequestMessage request = new(HttpMethod.Get, BaseUrl);
                        request.Headers.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
                        return await httpClient.SendAsync(request, token);
                    },
                    ct);

                if(!response.IsSuccessStatusCode)
                {
                    return [];
                }

                string html = await response.Content.ReadAsStringAsync(ct);
                HtmlDocument doc = new();
                doc.LoadHtml(html);

                // Container-Suche
                HtmlNode? container = doc.GetElementbyId(targetId);
                HtmlNodeCollection? rows = container?.SelectNodes(".//tr") ?? doc.DocumentNode.SelectNodes("//tr");

                if(rows is null)
                {
                    return [];
                }

                List<TibiaPalHuntingSpot> results = [];
                foreach(HtmlNode row in rows)
                {
                    if(row.SelectNodes("th") is not null)
                    {
                        continue;
                    }

                    HtmlNodeCollection? cells = row.SelectNodes("td");
                    if(cells is null || cells.Count < 5)
                    {
                        continue;
                    }

                    // Parsing mit modernem Indexing und Pattern Matching
                    string lvlText = cells[0].InnerText.Trim();
                    string name = cells[1].InnerText.Trim();
                    string? ytUrl = cells[1].SelectSingleNode(".//a")?.GetAttributeValue("href", string.Empty);
                    string exp = cells[2].InnerText.Trim();
                    string loot = cells[3].InnerText.Trim();
                    string weapon = cells[4].InnerText.Trim();

                    int minLevel = 0;
                    Match match = LevelRegex().Match(lvlText);
                    if(match.Success)
                    {
                        int.TryParse(match.Groups["val"].Value, out minLevel);
                    }

                    results.Add(new TibiaPalHuntingSpot(name, minLevel, vocation, exp, loot, weapon, ytUrl));
                }

                return results;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error scraping TibiaPal");
                return [];
            }
        }

        [GeneratedRegex(@"^(?<val>\d+)", RegexOptions.Compiled)]
        private static partial Regex LevelRegex();
    }
}