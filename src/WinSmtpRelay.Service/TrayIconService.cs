using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WinSmtpRelay.Core.Configuration;

namespace WinSmtpRelay.Service;

public class TrayIconService : BackgroundService
{
    private readonly AdminUiOptions _adminUiOptions;
    private readonly ILogger<TrayIconService> _logger;
    private System.Windows.Forms.NotifyIcon? _trayIcon;

    public TrayIconService(IOptions<AdminUiOptions> adminUiOptions, ILogger<TrayIconService> logger)
    {
        _adminUiOptions = adminUiOptions.Value;
        _logger = logger;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!Environment.UserInteractive)
        {
            _logger.LogDebug("Not running interactively, skipping tray icon");
            return Task.CompletedTask;
        }

        var thread = new Thread(() => RunTrayIcon(stoppingToken))
        {
            IsBackground = true,
            Name = "TrayIcon"
        };
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();

        return Task.CompletedTask;
    }

    private void RunTrayIcon(CancellationToken stoppingToken)
    {
        try
        {
            System.Windows.Forms.Application.EnableVisualStyles();

            _trayIcon = new System.Windows.Forms.NotifyIcon
            {
                Text = "WinSmtpRelay",
                Icon = System.Drawing.SystemIcons.Application,
                Visible = true,
                ContextMenuStrip = CreateContextMenu()
            };

            _trayIcon.DoubleClick += (_, _) => OpenAdminUi();

            stoppingToken.Register(() =>
            {
                _trayIcon.Visible = false;
                _trayIcon.Dispose();
                System.Windows.Forms.Application.ExitThread();
            });

            System.Windows.Forms.Application.Run();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Tray icon initialization failed");
        }
    }

    private System.Windows.Forms.ContextMenuStrip CreateContextMenu()
    {
        var menu = new System.Windows.Forms.ContextMenuStrip();

        var openUi = new System.Windows.Forms.ToolStripMenuItem("Open Admin UI");
        openUi.Click += (_, _) => OpenAdminUi();
        menu.Items.Add(openUi);

        menu.Items.Add(new System.Windows.Forms.ToolStripSeparator());

        var status = new System.Windows.Forms.ToolStripMenuItem("Status: Running") { Enabled = false };
        menu.Items.Add(status);

        return menu;
    }

    private void OpenAdminUi()
    {
        var url = $"http://localhost:{_adminUiOptions.Port}";
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to open Admin UI URL: {Url}", url);
        }
    }

    public override void Dispose()
    {
        _trayIcon?.Dispose();
        base.Dispose();
    }
}
