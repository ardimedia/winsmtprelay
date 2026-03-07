using DnsClient;
using Microsoft.Extensions.DependencyInjection;
using WinSmtpRelay.Core.Interfaces;

namespace WinSmtpRelay.Delivery;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDeliveryEngine(this IServiceCollection services)
    {
        services.AddSingleton<ILookupClient>(new LookupClient());
        services.AddSingleton<IMxResolver, MxResolver>();
        services.AddScoped<IDeliveryService, SmtpDeliveryService>();
        services.AddHostedService<DeliveryWorker>();

        return services;
    }
}
