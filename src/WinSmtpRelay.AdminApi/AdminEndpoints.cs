using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace WinSmtpRelay.AdminApi;

public static class AdminEndpoints
{
    public static IEndpointRouteBuilder MapAdminApi(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api");

        group.MapGet("/health", () => Results.Ok(new { Status = "Healthy" }));

        // TODO: Phase 3 — Queue status, message list/retry/delete
        // TODO: Phase 3 — Relay rules CRUD, domain routing CRUD
        // TODO: Phase 3 — Metrics endpoint

        return endpoints;
    }
}
