using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WinSmtpRelay.Core.Configuration;
using WinSmtpRelay.Core.Interfaces;

namespace WinSmtpRelay.Service;

public class TrayIconService : BackgroundService
{
    private readonly AdminUiOptions _adminUiOptions;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<TrayIconService> _logger;
    private System.Windows.Forms.NotifyIcon? _trayIcon;
    private System.Windows.Forms.ToolStripMenuItem? _statusItem;
    private System.Windows.Forms.ToolStripMenuItem? _queueItem;

    public TrayIconService(
        IOptions<AdminUiOptions> adminUiOptions,
        IServiceScopeFactory scopeFactory,
        ILogger<TrayIconService> logger)
    {
        _adminUiOptions = adminUiOptions.Value;
        _scopeFactory = scopeFactory;
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

            // Start periodic status updates
            var timer = new System.Windows.Forms.Timer { Interval = 5000 };
            timer.Tick += async (_, _) => await UpdateStatusAsync();
            timer.Start();

            stoppingToken.Register(() =>
            {
                timer.Stop();
                timer.Dispose();
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

        _statusItem = new System.Windows.Forms.ToolStripMenuItem("Status: Running") { Enabled = false };
        menu.Items.Add(_statusItem);

        _queueItem = new System.Windows.Forms.ToolStripMenuItem("Queue: ...") { Enabled = false };
        menu.Items.Add(_queueItem);

        return menu;
    }

    private async Task UpdateStatusAsync()
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var queue = scope.ServiceProvider.GetRequiredService<IMessageQueue>();
            var depth = await queue.GetQueueDepthAsync();

            var text = depth == 0
                ? "WinSmtpRelay — Idle"
                : $"WinSmtpRelay — {depth} queued";

            if (_trayIcon is not null)
            {
                _trayIcon.Text = text.Length > 63 ? text[..63] : text;
            }

            _queueItem?.GetCurrentParent()?.Invoke((System.Windows.Forms.MethodInvoker)(() =>
            {
                if (_queueItem is not null)
                    _queueItem.Text = $"Queue: {depth} message(s)";
            }));
        }
        catch
        {
            // Ignore errors during status updates
        }
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
