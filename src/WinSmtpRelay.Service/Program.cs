using Microsoft.EntityFrameworkCore;
using WinSmtpRelay.Core.Configuration;
using WinSmtpRelay.Delivery;
using WinSmtpRelay.SmtpListener;
using WinSmtpRelay.Storage;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddWindowsService(options =>
{
    options.ServiceName = "WinSmtpRelay";
});

// Configuration
builder.Services.Configure<SmtpListenerOptions>(builder.Configuration.GetSection(SmtpListenerOptions.SectionName));
builder.Services.Configure<DeliveryOptions>(builder.Configuration.GetSection(DeliveryOptions.SectionName));

// Storage
var connectionString = builder.Configuration.GetConnectionString("RelayDb") ?? "Data Source=winsmtprelay.db";
builder.Services.AddRelayStorage(connectionString);

// SMTP Listener
builder.Services.AddSmtpListener();

// Delivery Engine
builder.Services.AddDeliveryEngine();

// TODO: Phase 3 — Add AdminApi endpoints
// TODO: Phase 3 — Add AdminUi Blazor

var host = builder.Build();

// Auto-apply EF Core migrations on startup
using (var scope = host.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<RelayDbContext>();
    await db.Database.MigrateAsync();
}

await host.RunAsync();
