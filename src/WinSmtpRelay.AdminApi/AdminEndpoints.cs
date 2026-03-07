using System.Diagnostics;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using WinSmtpRelay.Core.Interfaces;
using WinSmtpRelay.Core.Models;
using WinSmtpRelay.Security;
using WinSmtpRelay.Storage;

namespace WinSmtpRelay.AdminApi;

public static class AdminEndpoints
{
    public static IEndpointRouteBuilder MapAdminApi(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api");

        MapHealthEndpoints(group);
        MapMetricsEndpoints(group);
        MapQueueEndpoints(group);
        MapDeliveryLogEndpoints(group);
        MapUserEndpoints(group);
        MapDkimEndpoints(group);
        MapServerEndpoints(group);

        return endpoints;
    }

    private static void MapHealthEndpoints(RouteGroupBuilder group)
    {
        group.MapGet("/health", () => Results.Ok(new
        {
            Status = "Healthy",
            Timestamp = DateTime.UtcNow
        }));
    }

    private static void MapMetricsEndpoints(RouteGroupBuilder group)
    {
        group.MapGet("/metrics", async (IMessageQueue q, RelayDbContext db, CancellationToken ct) =>
        {
            var process = Process.GetCurrentProcess();
            var queueDepth = await q.GetQueueDepthAsync(ct);
            var totalDeliveries = await db.DeliveryLogs.CountAsync(ct);
            var failedDeliveries = await db.DeliveryLogs.CountAsync(l => l.StatusCode.StartsWith("5"), ct);
            var totalMessages = await db.QueuedMessages.CountAsync(ct);

            return Results.Ok(new
            {
                Timestamp = DateTime.UtcNow,
                Queue = new { Depth = queueDepth, TotalProcessed = totalMessages },
                Deliveries = new { Total = totalDeliveries, Failed = failedDeliveries },
                Process = new
                {
                    UptimeSeconds = (DateTime.UtcNow - process.StartTime.ToUniversalTime()).TotalSeconds,
                    MemoryMB = process.WorkingSet64 / (1024.0 * 1024.0),
                    ThreadCount = process.Threads.Count
                }
            });
        });
    }

    private static void MapQueueEndpoints(RouteGroupBuilder group)
    {
        var queue = group.MapGroup("/queue");

        queue.MapGet("/status", async (IMessageQueue q, CancellationToken ct) =>
        {
            var depth = await q.GetQueueDepthAsync(ct);
            return Results.Ok(new QueueStatusResponse(depth));
        });

        queue.MapGet("/messages", async (IMessageQueue q, CancellationToken ct,
            int limit = 50) =>
        {
            var messages = await q.GetPendingAsync(limit, ct);
            return Results.Ok(messages.Select(m => new MessageSummary(
                m.Id, m.MessageId, m.Sender, m.Recipients, m.SizeBytes,
                m.Status, m.RetryCount, m.LastError, m.CreatedUtc, m.NextRetryUtc, m.CompletedUtc)));
        });

        queue.MapGet("/messages/{id:long}", async (long id, IMessageQueue q, CancellationToken ct) =>
        {
            var msg = await q.GetByIdAsync(id, ct);
            return msg is null ? Results.NotFound() : Results.Ok(msg);
        });

        queue.MapPost("/messages/{id:long}/retry", async (long id, IMessageQueue q, CancellationToken ct) =>
        {
            var msg = await q.GetByIdAsync(id, ct);
            if (msg is null) return Results.NotFound();
            if (msg.Status is not (MessageStatus.Failed or MessageStatus.Bounced))
                return Results.BadRequest(new { Error = "Only failed or bounced messages can be retried" });

            await q.UpdateStatusAsync(id, MessageStatus.Queued, null, ct);
            await q.SetRetryAsync(id, 0, DateTime.UtcNow, ct);
            return Results.Ok(new { Message = "Message re-queued for delivery" });
        });

        queue.MapDelete("/messages/{id:long}", async (long id, IMessageQueue q, CancellationToken ct) =>
        {
            var msg = await q.GetByIdAsync(id, ct);
            if (msg is null) return Results.NotFound();
            await q.DeleteAsync(id, ct);
            return Results.Ok(new { Message = "Message deleted" });
        });
    }

    private static void MapUserEndpoints(RouteGroupBuilder group)
    {
        var users = group.MapGroup("/users");

        users.MapGet("/", async (IUserService svc, CancellationToken ct) =>
        {
            var all = await svc.GetAllUsersAsync(ct);
            return Results.Ok(all.Select(u => new UserSummary(
                u.Id, u.Username, u.IsEnabled, u.AllowedSenderAddresses,
                u.RateLimitPerMinute, u.RateLimitPerDay, u.CreatedUtc)));
        });

        users.MapPost("/", async (CreateUserRequest req, IUserService svc, CancellationToken ct) =>
        {
            var existing = await svc.GetByUsernameAsync(req.Username, ct);
            if (existing is not null)
                return Results.Conflict(new { Error = $"User '{req.Username}' already exists" });

            await svc.CreateUserAsync(req.Username, req.Password, ct);
            return Results.Created($"/api/users/{req.Username}", new { Message = "User created" });
        });

        users.MapPut("/{id:int}", async (int id, UpdateUserRequest req, RelayDbContext db, CancellationToken ct) =>
        {
            var user = await db.RelayUsers.FindAsync([id], ct);
            if (user is null) return Results.NotFound();

            user.AllowedSenderAddresses = req.AllowedSenderAddresses;
            user.RateLimitPerMinute = req.RateLimitPerMinute;
            user.RateLimitPerDay = req.RateLimitPerDay;
            user.IsEnabled = req.IsEnabled;
            await db.SaveChangesAsync(ct);

            return Results.Ok(new { Message = "User updated" });
        });

        users.MapDelete("/{id:int}", async (int id, IUserService svc, CancellationToken ct) =>
        {
            await svc.DeleteUserAsync(id, ct);
            return Results.Ok(new { Message = "User deleted" });
        });
    }

    private static void MapDkimEndpoints(RouteGroupBuilder group)
    {
        group.MapPost("/dkim/generate", (DkimGenerateRequest req) =>
        {
            var (privateKey, publicKey, dnsTxt) = DkimKeyGenerator.GenerateKeyPair(
                req.Domain, req.Selector, req.KeySize > 0 ? req.KeySize : 2048);

            return Results.Ok(new
            {
                Domain = req.Domain,
                Selector = req.Selector,
                PrivateKeyPem = privateKey,
                PublicKeyPem = publicKey,
                DnsRecord = $"{req.Selector}._domainkey.{req.Domain}",
                DnsTxtValue = dnsTxt
            });
        });
    }

    private static void MapDeliveryLogEndpoints(RouteGroupBuilder group)
    {
        var logs = group.MapGroup("/deliverylogs");

        logs.MapGet("/", async (RelayDbContext db, CancellationToken ct,
            long? messageId = null, int limit = 50, int offset = 0) =>
        {
            var query = db.DeliveryLogs.AsNoTracking().AsQueryable();
            if (messageId.HasValue)
                query = query.Where(l => l.QueuedMessageId == messageId.Value);

            var items = await query
                .OrderByDescending(l => l.TimestampUtc)
                .Skip(offset)
                .Take(limit)
                .Select(l => new DeliveryLogSummary(
                    l.Id, l.QueuedMessageId, l.Recipient, l.StatusCode,
                    l.StatusMessage, l.RemoteServer, l.TimestampUtc))
                .ToListAsync(ct);

            return Results.Ok(items);
        });

        logs.MapGet("/count", async (RelayDbContext db, CancellationToken ct) =>
        {
            var count = await db.DeliveryLogs.CountAsync(ct);
            return Results.Ok(new { Count = count });
        });
    }

    private static void MapServerEndpoints(RouteGroupBuilder group)
    {
        group.MapGet("/server/info", () =>
        {
            var assembly = typeof(AdminEndpoints).Assembly;
            var version = assembly.GetName().Version?.ToString() ?? "0.0.0";

            return Results.Ok(new
            {
                Version = version,
                Runtime = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription,
                OS = System.Runtime.InteropServices.RuntimeInformation.OSDescription,
                StartedUtc = System.Diagnostics.Process.GetCurrentProcess().StartTime.ToUniversalTime()
            });
        });
    }
}

public record QueueStatusResponse(int Depth);

public record MessageSummary(
    long Id, string MessageId, string Sender, string Recipients, int SizeBytes,
    MessageStatus Status, int RetryCount, string? LastError,
    DateTime CreatedUtc, DateTime? NextRetryUtc, DateTime? CompletedUtc);

public record UserSummary(
    int Id, string Username, bool IsEnabled, string? AllowedSenderAddresses,
    int? RateLimitPerMinute, int? RateLimitPerDay, DateTime CreatedUtc);

public record CreateUserRequest(string Username, string Password);

public record UpdateUserRequest(
    bool IsEnabled, string? AllowedSenderAddresses,
    int? RateLimitPerMinute, int? RateLimitPerDay);

public record DkimGenerateRequest(string Domain, string Selector, int KeySize = 2048);

public record DeliveryLogSummary(
    long Id, long QueuedMessageId, string Recipient, string StatusCode,
    string StatusMessage, string? RemoteServer, DateTime TimestampUtc);
