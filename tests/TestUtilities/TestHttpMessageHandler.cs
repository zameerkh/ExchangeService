using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;

namespace ExchangeService.Tests.TestUtilities;

public class TestHttpMessageHandler : HttpMessageHandler
{
    private readonly ConcurrentQueue<Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>>> _responses = new();

    public void EnqueueResponse(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> responder)
        => _responses.Enqueue(responder);

    public void EnqueueJson(object payload, HttpStatusCode status = HttpStatusCode.OK, IDictionary<string, string>? headers = null)
    {
        EnqueueResponse((req, ct) =>
        {
            var json = System.Text.Json.JsonSerializer.Serialize(payload);
            var resp = new HttpResponseMessage(status)
            {
                Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
            };
            if (headers != null)
            {
                foreach (var kv in headers)
                    resp.Headers.TryAddWithoutValidation(kv.Key, kv.Value);
            }
            return Task.FromResult(resp);
        });
    }

    public void EnqueueStatus(HttpStatusCode status)
    {
        EnqueueResponse((req, ct) => Task.FromResult(new HttpResponseMessage(status)));
    }

    public void EnqueueDelay(TimeSpan delay, HttpStatusCode status = HttpStatusCode.OK)
    {
        EnqueueResponse(async (req, ct) =>
        {
            await Task.Delay(delay, ct);
            return new HttpResponseMessage(status) { Content = new StringContent("") };
        });
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (_responses.TryDequeue(out var responder))
        {
            return responder(request, cancellationToken);
        }

        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound)
        {
            Content = new StringContent("No test response enqueued for this request")
        });
    }
}
