using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SmtpServer;
using SmtpServer.Authentication;
using WinSmtpRelay.Core.Interfaces;

namespace WinSmtpRelay.SmtpListener;

public class RelayUserAuthenticator : UserAuthenticator, IUserAuthenticator
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<RelayUserAuthenticator> _logger;

    public RelayUserAuthenticator(IServiceScopeFactory scopeFactory, ILogger<RelayUserAuthenticator> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public override async Task<bool> AuthenticateAsync(
        ISessionContext context,
        string user,
        string password,
        CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var userService = scope.ServiceProvider.GetRequiredService<IUserService>();

        var result = await userService.ValidateCredentialsAsync(user, password, cancellationToken);

        if (result)
        {
            context.Properties["AuthenticatedUser"] = user;
            _logger.LogInformation("SMTP AUTH successful for user {User}", user);
        }
        else
        {
            _logger.LogWarning("SMTP AUTH failed for user {User}", user);
        }

        return result;
    }
}
