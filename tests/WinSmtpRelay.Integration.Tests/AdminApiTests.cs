using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WinSmtpRelay.AdminApi;
using WinSmtpRelay.Core.Interfaces;
using WinSmtpRelay.Core.Models;
using WinSmtpRelay.Storage;

namespace WinSmtpRelay.Integration.Tests;

[TestClass]
public class AdminApiTests
{
    private WebApplication _app = null!;
    private HttpClient _client = null!;
    private string _dbPath = null!;

    [TestInitialize]
    public async Task Setup()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"adminapi_test_{Guid.NewGuid()}.db");

        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Services.AddRelayStorage($"Data Source={_dbPath}");

        _app = builder.Build();

        using (var scope = _app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<RelayDbContext>();
            await db.Database.MigrateAsync();
        }

        _app.MapAdminApi();

        await _app.StartAsync();

        var address = _app.Urls.First();
        _client = new HttpClient { BaseAddress = new Uri(address) };
    }

    [TestCleanup]
    public async Task Cleanup()
    {
        _client.Dispose();
        await _app.StopAsync();
        await _app.DisposeAsync();
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        if (File.Exists(_dbPath)) File.Delete(_dbPath);
    }

    [TestMethod]
    [TestCategory("Integration")]
    public async Task Health_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/health");
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<HealthResponse>();
        Assert.AreEqual("Healthy", body?.Status);
    }

    [TestMethod]
    [TestCategory("Integration")]
    public async Task QueueStatus_ReturnsDepth()
    {
        var response = await _client.GetAsync("/api/queue/status");
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<QueueStatusResponse>();
        Assert.IsNotNull(body);
        Assert.AreEqual(0, body.Depth);
    }

    [TestMethod]
    [TestCategory("Integration")]
    public async Task QueueMessages_ReturnsEmptyList()
    {
        var response = await _client.GetAsync("/api/queue/messages");
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

        var messages = await response.Content.ReadFromJsonAsync<MessageSummary[]>();
        Assert.IsNotNull(messages);
        Assert.AreEqual(0, messages.Length);
    }

    [TestMethod]
    [TestCategory("Integration")]
    public async Task QueueMessage_NotFound_Returns404()
    {
        var response = await _client.GetAsync("/api/queue/messages/999");
        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
    }

    [TestMethod]
    [TestCategory("Integration")]
    public async Task QueueMessage_EnqueueAndRetrieve()
    {
        // Enqueue a message directly via the service
        using (var scope = _app.Services.CreateScope())
        {
            var queue = scope.ServiceProvider.GetRequiredService<IMessageQueue>();
            await queue.EnqueueAsync(new QueuedMessage
            {
                MessageId = "<test@example.com>",
                Sender = "sender@example.com",
                Recipients = "recipient@example.com",
                RawMessage = "From: test"u8.ToArray(),
                SizeBytes = 10
            });
        }

        // Verify via API
        var statusResponse = await _client.GetAsync("/api/queue/status");
        var status = await statusResponse.Content.ReadFromJsonAsync<QueueStatusResponse>();
        Assert.AreEqual(1, status?.Depth);

        var messagesResponse = await _client.GetAsync("/api/queue/messages");
        var messages = await messagesResponse.Content.ReadFromJsonAsync<MessageSummary[]>();
        Assert.AreEqual(1, messages?.Length);
        Assert.AreEqual("sender@example.com", messages?[0].Sender);
    }

    [TestMethod]
    [TestCategory("Integration")]
    public async Task QueueMessage_Delete_RemovesMessage()
    {
        long msgId;
        using (var scope = _app.Services.CreateScope())
        {
            var queue = scope.ServiceProvider.GetRequiredService<IMessageQueue>();
            msgId = await queue.EnqueueAsync(new QueuedMessage
            {
                MessageId = "<delete@example.com>",
                Sender = "sender@example.com",
                Recipients = "recipient@example.com",
                RawMessage = "From: test"u8.ToArray(),
                SizeBytes = 10
            });
        }

        var deleteResponse = await _client.DeleteAsync($"/api/queue/messages/{msgId}");
        Assert.AreEqual(HttpStatusCode.OK, deleteResponse.StatusCode);

        var getResponse = await _client.GetAsync($"/api/queue/messages/{msgId}");
        Assert.AreEqual(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [TestMethod]
    [TestCategory("Integration")]
    public async Task Users_CreateAndList()
    {
        var createResponse = await _client.PostAsJsonAsync("/api/users",
            new CreateUserRequest("testuser", "P@ssw0rd!"));
        Assert.AreEqual(HttpStatusCode.Created, createResponse.StatusCode);

        var listResponse = await _client.GetAsync("/api/users");
        var users = await listResponse.Content.ReadFromJsonAsync<UserSummary[]>();
        Assert.AreEqual(1, users?.Length);
        Assert.AreEqual("testuser", users?[0].Username);
    }

    [TestMethod]
    [TestCategory("Integration")]
    public async Task Users_CreateDuplicate_ReturnsConflict()
    {
        await _client.PostAsJsonAsync("/api/users",
            new CreateUserRequest("alice", "pass1"));

        var response = await _client.PostAsJsonAsync("/api/users",
            new CreateUserRequest("alice", "pass2"));
        Assert.AreEqual(HttpStatusCode.Conflict, response.StatusCode);
    }

    [TestMethod]
    [TestCategory("Integration")]
    public async Task Users_Delete_RemovesUser()
    {
        await _client.PostAsJsonAsync("/api/users",
            new CreateUserRequest("tobedeleted", "pass"));

        var listResponse = await _client.GetAsync("/api/users");
        var users = await listResponse.Content.ReadFromJsonAsync<UserSummary[]>();
        var userId = users![0].Id;

        var deleteResponse = await _client.DeleteAsync($"/api/users/{userId}");
        Assert.AreEqual(HttpStatusCode.OK, deleteResponse.StatusCode);

        listResponse = await _client.GetAsync("/api/users");
        users = await listResponse.Content.ReadFromJsonAsync<UserSummary[]>();
        Assert.AreEqual(0, users?.Length);
    }

    [TestMethod]
    [TestCategory("Integration")]
    public async Task ServerInfo_ReturnsVersionAndRuntime()
    {
        var response = await _client.GetAsync("/api/server/info");
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        Assert.IsTrue(body.Contains("runtime", StringComparison.OrdinalIgnoreCase));
        Assert.IsTrue(body.Contains(".NET", StringComparison.OrdinalIgnoreCase));
    }

    private record HealthResponse(string Status);
}
