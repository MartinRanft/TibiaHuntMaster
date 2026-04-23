using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;

using FluentAssertions;

using Polly.Timeout;

using TibiaHuntMaster.Infrastructure.Http.TibiaData;

namespace TibiaHuntMaster.Tests.TibiaData
{
    public sealed class TibiaDataClientTests
    {
        [Theory]
        [InlineData(HttpStatusCode.InternalServerError)]
        [InlineData(HttpStatusCode.TooManyRequests)]
        public async Task GetCreaturesAsync_ShouldRetryTransientResponses_AndEventuallySucceed(HttpStatusCode statusCode)
        {
            using SequenceHandler handler = new(
                CreateResponse(statusCode, """{"error":"transient"}"""),
                CreateResponse(statusCode, """{"error":"transient"}"""),
                CreateResponse(
                    HttpStatusCode.OK,
                    """{"creatures":{"boosted":{"name":"Dragon","race":"reptile","image_url":"x","featured":true},"creature_list":[]}}"""));
            using HttpClient httpClient = new(handler);
            TibiaDataClient client = new(httpClient, TimeSpan.FromSeconds(2));

            var result = await client.GetCreaturesAsync();

            result.Should().NotBeNull();
            result!.Creatures.Boosted.Name.Should().Be("Dragon");
            handler.CallCount.Should().Be(3);
        }

        [Fact]
        public async Task GetCreaturesAsync_ShouldNotRetryNonTransientClientErrors()
        {
            using SequenceHandler handler = new(CreateResponse(HttpStatusCode.NotFound, """{"error":"missing"}"""));
            using HttpClient httpClient = new(handler);
            TibiaDataClient client = new(httpClient, TimeSpan.FromSeconds(2));

            Func<Task> act = async () => await client.GetCreaturesAsync();

            await act.Should().ThrowAsync<HttpRequestException>();
            handler.CallCount.Should().Be(1);
        }

        [Fact]
        public async Task GetCharactersAsync_ShouldRespectConfiguredTimeout()
        {
            using HttpClient httpClient = new(new DelayedResponseHandler(TimeSpan.FromSeconds(5)));
            TibiaDataClient client = new(httpClient, TimeSpan.FromMilliseconds(150));
            Stopwatch stopwatch = Stopwatch.StartNew();

            Func<Task> act = async () => await client.GetCharactersAsync("Tentakel");

            await act.Should().ThrowAsync<TimeoutRejectedException>();
            stopwatch.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(2));
        }

        [Fact]
        public void DefaultRequestTimeout_ShouldBeThirtySeconds()
        {
            TibiaDataClient.DefaultRequestTimeout.Should().Be(TimeSpan.FromSeconds(30));
        }

        private static HttpResponseMessage CreateResponse(HttpStatusCode statusCode, string json)
        {
            return new HttpResponseMessage(statusCode)
            {
                Content = JsonContent.Create(new { })
            }.WithJson(json);
        }

        private sealed class DelayedResponseHandler(TimeSpan delay) : HttpMessageHandler
        {
            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                await Task.Delay(delay, cancellationToken);
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(new { ok = true })
                };
            }
        }

        private sealed class SequenceHandler(params HttpResponseMessage[] responses) : HttpMessageHandler
        {
            private readonly Queue<HttpResponseMessage> _responses = new(responses);

            public int CallCount { get; private set; }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                CallCount++;
                if (_responses.Count == 0)
                {
                    throw new InvalidOperationException("No more responses configured.");
                }

                return Task.FromResult(_responses.Dequeue());
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    foreach (HttpResponseMessage response in _responses)
                    {
                        response.Dispose();
                    }
                }

                base.Dispose(disposing);
            }
        }
    }

    internal static class HttpResponseMessageExtensions
    {
        public static HttpResponseMessage WithJson(this HttpResponseMessage response, string json)
        {
            response.Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            return response;
        }
    }
}
