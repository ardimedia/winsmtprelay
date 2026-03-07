using DnsClient;
using Microsoft.Extensions.DependencyInjection;
using WinSmtpRelay.Core.Interfaces;
using WinSmtpRelay.Security;

namespace WinSmtpRelay.Delivery;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDeliveryEngine(this IServiceCollection services)
    {
        services.AddSingleton<ILookupClient>(new LookupClient());
        services.AddSingleton<IMxResolver, MxResolver>();
        services.AddSingleton<DkimSigningService>();
        services.AddScoped<IDeliveryService, SmtpDeliveryService>();
        services.AddHostedService<DeliveryWorker>();

        return services;
    }
}
