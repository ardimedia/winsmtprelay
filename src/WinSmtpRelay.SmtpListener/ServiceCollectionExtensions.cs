using Microsoft.Extensions.DependencyInjection;
using SmtpServer.Authentication;
using WinSmtpRelay.Security;

namespace WinSmtpRelay.SmtpListener;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSmtpListener(this IServiceCollection services)
    {
        services.AddSingleton<CertificateLoader>();
        services.AddSingleton<RelayMessageStore>();
        services.AddSingleton<RelayMailboxFilter>();
        services.AddSingleton<IUserAuthenticator, RelayUserAuthenticator>();
        services.AddSingleton<SpfValidator>();
        services.AddSingleton<DmarcValidator>();
        services.AddSingleton<EmailAuthenticationService>();
        services.AddSingleton<RateLimiter>();
        services.AddHostedService<SmtpRelayServer>();
        services.AddHostedService<PickupFolderService>();

        return services;
    }
}
