using System.Net;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using WinSmtpRelay.Core.Configuration;
using WinSmtpRelay.SmtpListener;

namespace WinSmtpRelay.SmtpListener.Tests;

[TestClass]
public class WebhookServiceTests
{
    [TestMethod]
    [TestCategory("Unit")]
    public async Task NotifyMessageReceived_NoEndpoints_DoesNotThrow()
    {
        var service = CreateService(new WebhookOptions());
        await service.NotifyMessageReceivedAsync("msg1", "sender@test.com", "rcpt@test.com", 1024, "127.0.0.1");
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task NotifyMessageReceived_WithEndpoint_SendsPost()
    {
        var handler = new TrackingHandler();
        var service = CreateService(new WebhookOptions
        {
            OnMessageReceived = [new WebhookEndpoint { Url = "http://localhost:9999/hook", TimeoutSeconds = 5 }]
        }, handler);

        await service.NotifyMessageReceivedAsync("<test@relay>", "from@a.com", "to@b.com;cc@b.com", 2048, "10.0.0.1");

        Assert.AreEqual(1, handler.Requests.Count);
        var req = handler.Requests[0];
        Assert.AreEqual(HttpMethod.Post, req.Method);
        Assert.AreEqual("http://localhost:9999/hook", req.RequestUri!.ToString());

        var body = await req.Content!.ReadAsStringAsync();
        Assert.IsTrue(body.Contains("\"event\":\"message.received\""));
        Assert.IsTrue(body.Contains("from@a.com"));
        Assert.IsTrue(body.Contains("to@b.com"));
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task NotifyMessageReceived_WithSecret_AddsSignatureHeader()
    {
        var handler = new TrackingHandler();
        var service = CreateService(new WebhookOptions
        {
            OnMessageReceived = [new WebhookEndpoint { Url = "http://localhost:9999/hook", Secret = "mysecret" }]
        }, handler);

        await service.NotifyMessageReceivedAsync("msg1", "s@a.com", "r@b.com", 100, null);

        Assert.AreEqual(1, handler.Requests.Count);
        Assert.IsTrue(handler.Requests[0].Headers.Contains("X-Webhook-Signature"));
        var sig = handler.Requests[0].Headers.GetValues("X-Webhook-Signature").First();
        Assert.IsTrue(sig.StartsWith("sha256="));
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task NotifyMessageReceived_EndpointFails_DoesNotThrow()
    {
        var handler = new TrackingHandler(HttpStatusCode.InternalServerError);
        var service = CreateService(new WebhookOptions
        {
            OnMessageReceived = [new WebhookEndpoint { Url = "http://localhost:9999/hook" }]
        }, handler);

        // Should not throw even when endpoint returns 500
        await service.NotifyMessageReceivedAsync("msg1", "s@a.com", "r@b.com", 100, null);
        Assert.AreEqual(1, handler.Requests.Count);
    }

    private static WebhookService CreateService(WebhookOptions options, TrackingHandler? handler = null)
    {
        handler ??= new TrackingHandler();
        var httpClientFactory = new StubHttpClientFactory(handler);
        return new WebhookService(
            Options.Create(options),
            httpClientFactory,
            NullLogger<WebhookService>.Instance);
    }

    private class TrackingHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _statusCode;
        public List<HttpRequestMessage> Requests { get; } = [];

        public TrackingHandler(HttpStatusCode statusCode = HttpStatusCode.OK)
        {
            _statusCode = statusCode;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            // Clone content so we can read it later
            Requests.Add(request);
            return Task.FromResult(new HttpResponseMessage(_statusCode));
        }
    }

    private class StubHttpClientFactory : IHttpClientFactory
    {
        private readonly HttpMessageHandler _handler;
        public StubHttpClientFactory(HttpMessageHandler handler) => _handler = handler;
        public HttpClient CreateClient(string name) => new(_handler, disposeHandler: false);
    }
}
