using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;

namespace WinSmtpRelay.Service;

/// <summary>
/// Writes a registry flag when the service starts and clears it on shutdown.
/// The MSI installer reads this flag during upgrades to decide whether to
/// auto-restart the service after the update.
/// </summary>
public class ServiceStateReporter(ILogger<ServiceStateReporter> logger) : IHostedService
{
    private const string RegistryKeyPath = @"SOFTWARE\ARDIMEDIA\WinSmtpRelay";
    private const string ValueName = "ServiceRunning";

    public Task StartAsync(CancellationToken cancellationToken)
    {
        SetRegistryFlag(1);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        SetRegistryFlag(0);
        return Task.CompletedTask;
    }

    private void SetRegistryFlag(int value)
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(RegistryKeyPath, writable: true);
            if (key is not null)
            {
                key.SetValue(ValueName, value, RegistryValueKind.DWord);
            }
            else
            {
                // Key doesn't exist yet (first run before MSI creates it) — create it
                using var newKey = Registry.LocalMachine.CreateSubKey(RegistryKeyPath);
                newKey.SetValue(ValueName, value, RegistryValueKind.DWord);
            }
        }
        catch (Exception ex)
        {
            // Non-critical — don't prevent service from starting/stopping
            logger.LogDebug(ex, "Could not write ServiceRunning registry flag");
        }
    }
}
