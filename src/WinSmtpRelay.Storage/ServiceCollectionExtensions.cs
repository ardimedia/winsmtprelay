using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WinSmtpRelay.Core.Interfaces;

namespace WinSmtpRelay.Storage;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddRelayStorage(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<RelayDbContext>(options =>
            options.UseSqlite(connectionString, sqlite => sqlite.MigrationsAssembly("WinSmtpRelay.Storage")));

        services.AddScoped<IMessageQueue, MessageQueue>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IStatisticsService, StatisticsService>();

        // Configuration services
        services.AddScoped<IReceiveConnectorService, ReceiveConnectorService>();
        services.AddScoped<IAcceptedDomainService, AcceptedDomainService>();
        services.AddScoped<IIpAccessRuleService, IpAccessRuleService>();
        services.AddScoped<ISendConnectorService, SendConnectorService>();
        services.AddScoped<IDomainRouteService, DomainRouteService>();
        services.AddScoped<IDkimDomainService, DkimDomainService>();
        services.AddScoped<IRateLimitSettingsService, RateLimitSettingsService>();
        services.AddScoped<IMessageFilterService, MessageFilterService>();

        return services;
    }
}
