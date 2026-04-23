using System.Net.Http.Json;
using System.Net;

using Microsoft.Extensions.Logging;

namespace TibiaHuntMaster.Infrastructure.Http.Content.Shared
{
    public abstract class ContentHttpClientBase<TClient>(HttpClient httpClient, ILogger<TClient> logger)
    {
        private const int MaxRetryAttempts = 6;
        private static readonly TimeSpan MaxRetryDelay = TimeSpan.FromSeconds(30);

        protected HttpClient HttpClient { get; } = httpClient;

        protected ILogger Logger { get; } = logger;

        protected async Task<TResponse> GetJsonAsync<TResponse>(string relativeUrl, CancellationToken ct = default)
        {
            for(int attempt = 1; attempt <= MaxRetryAttempts; attempt++)
            {
                using HttpResponseMessage response = await HttpClient.GetAsync(relativeUrl, ct);

                if(response.IsSuccessStatusCode)
                {
                    TResponse? result = await response.Content.ReadFromJsonAsync<TResponse>(cancellationToken: ct);

                    if(result is not null)
                    {
                        return result;
                    }

                    Logger.LogError(
                        "Deserialization of response from {RelativeUrl} to type {TypeName} returned null.",
                        relativeUrl,
                        typeof(TResponse).Name);

                    throw new InvalidOperationException(
                        $"Response for '{relativeUrl}' could not be deserialized to '{typeof(TResponse).Name}'.");
                }

                if(IsRetryable(response.StatusCode) && attempt < MaxRetryAttempts)
                {
                    TimeSpan delay = GetRetryDelay(response, attempt);

                    Logger.LogWarning(
                        "HTTP GET {RelativeUrl} returned {StatusCode}. Retrying in {Delay} (attempt {Attempt}/{MaxAttempts}).",
                        relativeUrl,
                        (int)response.StatusCode,
                        delay,
                        attempt,
                        MaxRetryAttempts);

                    await Task.Delay(delay, ct);
                    continue;
                }

                string responseBody = await response.Content.ReadAsStringAsync(ct);

                Logger.LogError(
                    "HTTP GET {RelativeUrl} failed with status code {StatusCode}. Response: {ResponseBody}",
                    relativeUrl,
                    (int)response.StatusCode,
                    responseBody);

                response.EnsureSuccessStatusCode();
            }

            throw new InvalidOperationException($"HTTP GET for '{relativeUrl}' exceeded retry limit unexpectedly.");
        }

        protected async Task DownloadToStreamAsync(string relativeUrl, Stream destination, CancellationToken ct = default)
        {
            if(destination is null)
            {
                throw new ArgumentNullException(nameof(destination));
            }

            if(!destination.CanWrite)
            {
                throw new ArgumentException("Destination stream must be writable.", nameof(destination));
            }

            for(int attempt = 1; attempt <= MaxRetryAttempts; attempt++)
            {
                using HttpResponseMessage response = await HttpClient.GetAsync(relativeUrl, HttpCompletionOption.ResponseHeadersRead, ct);

                if(response.IsSuccessStatusCode)
                {
                    await using Stream source = await response.Content.ReadAsStreamAsync(ct);
                    await source.CopyToAsync(destination, ct);
                    return;
                }

                if(IsRetryable(response.StatusCode) && attempt < MaxRetryAttempts)
                {
                    TimeSpan delay = GetRetryDelay(response, attempt);

                    Logger.LogWarning(
                        "HTTP GET {RelativeUrl} returned {StatusCode}. Retrying binary download in {Delay} (attempt {Attempt}/{MaxAttempts}).",
                        relativeUrl,
                        (int)response.StatusCode,
                        delay,
                        attempt,
                        MaxRetryAttempts);

                    await Task.Delay(delay, ct);
                    continue;
                }

                string responseBody = await response.Content.ReadAsStringAsync(ct);

                Logger.LogError(
                    "HTTP GET {RelativeUrl} failed with status code {StatusCode}. Response: {ResponseBody}",
                    relativeUrl,
                    (int)response.StatusCode,
                    responseBody);

                response.EnsureSuccessStatusCode();
            }
        }

        private static bool IsRetryable(HttpStatusCode statusCode)
        {
            return statusCode == HttpStatusCode.TooManyRequests
                   || statusCode == HttpStatusCode.RequestTimeout
                   || (int)statusCode >= 500;
        }

        private static TimeSpan GetRetryDelay(HttpResponseMessage response, int attempt)
        {
            if(response.Headers.RetryAfter?.Delta is TimeSpan retryAfterDelta)
            {
                return retryAfterDelta;
            }

            if(response.Headers.RetryAfter?.Date is DateTimeOffset retryAfterDate)
            {
                TimeSpan untilRetry = retryAfterDate - DateTimeOffset.UtcNow;
                return untilRetry > TimeSpan.Zero ? untilRetry : TimeSpan.Zero;
            }

            double exponentialSeconds = Math.Min(Math.Pow(2, attempt - 1), MaxRetryDelay.TotalSeconds);
            int jitterMilliseconds = Random.Shared.Next(0, 250);
            return TimeSpan.FromSeconds(exponentialSeconds) + TimeSpan.FromMilliseconds(jitterMilliseconds);
        }
    }
}
