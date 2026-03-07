using Microsoft.EntityFrameworkCore;
using WinSmtpRelay.AdminApi;
using WinSmtpRelay.Core.Configuration;
using WinSmtpRelay.Delivery;
using WinSmtpRelay.SmtpListener;
using WinSmtpRelay.Storage;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddWindowsService(options =>
{
    options.ServiceName = "WinSmtpRelay";
});

// Configuration
builder.Services.Configure<SmtpListenerOptions>(builder.Configuration.GetSection(SmtpListenerOptions.SectionName));
builder.Services.Configure<DeliveryOptions>(builder.Configuration.GetSection(DeliveryOptions.SectionName));
builder.Services.Configure<TlsOptions>(builder.Configuration.GetSection(TlsOptions.SectionName));
builder.Services.Configure<DkimOptions>(builder.Configuration.GetSection(DkimOptions.SectionName));
builder.Services.Configure<AdminUiOptions>(builder.Configuration.GetSection(AdminUiOptions.SectionName));

// Storage
var connectionString = builder.Configuration.GetConnectionString("RelayDb") ?? "Data Source=winsmtprelay.db";
builder.Services.AddRelayStorage(connectionString);

// SMTP Listener
builder.Services.AddSmtpListener();

// Delivery Engine
builder.Services.AddDeliveryEngine();

// Kestrel for Admin UI + API
var adminUiConfig = builder.Configuration.GetSection(AdminUiOptions.SectionName).Get<AdminUiOptions>() ?? new();
if (adminUiConfig.Enabled)
{
    builder.WebHost.ConfigureKestrel(options =>
    {
        options.Listen(System.Net.IPAddress.Parse(adminUiConfig.BindAddress), adminUiConfig.Port);
    });

    builder.Services.AddRazorComponents()
        .AddInteractiveServerComponents();
}

var app = builder.Build();

// Auto-apply EF Core migrations on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<RelayDbContext>();
    await db.Database.MigrateAsync();
}

if (adminUiConfig.Enabled)
{
    app.UseStaticFiles();
    app.UseAntiforgery();

    // Admin REST API
    app.MapAdminApi();

    // Blazor Admin UI
    app.MapRazorComponents<WinSmtpRelay.AdminUi.Components.App>()
        .AddInteractiveServerRenderMode();
}

await app.RunAsync();
