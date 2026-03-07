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

        return services;
    }
}
