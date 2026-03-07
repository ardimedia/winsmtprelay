using WinSmtpRelay.Core.Configuration;
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

// TODO: Phase 1 — Add SmtpListener hosted service
// TODO: Phase 1 — Add DeliveryWorker hosted service
// TODO: Phase 3 — Add AdminApi endpoints
// TODO: Phase 3 — Add AdminUi Blazor

var host = builder.Build();
host.Run();
