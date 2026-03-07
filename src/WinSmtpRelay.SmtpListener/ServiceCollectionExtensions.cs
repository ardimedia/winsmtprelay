using Microsoft.Extensions.DependencyInjection;

namespace WinSmtpRelay.SmtpListener;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSmtpListener(this IServiceCollection services)
    {
        services.AddSingleton<RelayMessageStore>();
        services.AddSingleton<RelayMailboxFilter>();
        services.AddHostedService<SmtpRelayServer>();

        return services;
    }
}
