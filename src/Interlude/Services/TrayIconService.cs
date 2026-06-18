using System.Windows;
using Interlude.Models;
using Forms = System.Windows.Forms;
using Drawing = System.Drawing;

namespace Interlude.Services;

public sealed class TrayIconService : IDisposable
{
    private readonly SettingsService _settingsService;
    private readonly StartupService _startupService;
    private readonly AutomationEngine _automationEngine;
    private readonly LocalizationService _localization;
    private readonly WindowCoordinator _windowCoordinator;
    private readonly Drawing.Icon _trayIcon;
    private readonly Forms.NotifyIcon _notifyIcon;
    private readonly Forms.ContextMenuStrip _menu = new();
    private bool _disposed;

    public TrayIconService(
        SettingsService settingsService,
        StartupService startupService,
        AutomationEngine automationEngine,
        LocalizationService localization,
        WindowCoordinator windowCoordinator)
    {
        _settingsService = settingsService;
        _startupService = startupService;
        _automationEngine = automationEngine;
        _localization = localization;
        _windowCoordinator = windowCoordinator;
        _trayIcon = CreateTrayIcon();
        _notifyIcon = new Forms.NotifyIcon
        {
            Icon = _trayIcon,
            Text = "间奏 | Interlude",
            Visible = true,
            ContextMenuStrip = _menu
        };
        _notifyIcon.DoubleClick += (_, _) => _windowCoordinator.ShowMainWindow();
        _menu.Opening += (_, _) => RebuildMenu();
        _automationEngine.SnapshotChanged += OnSnapshotChanged;
        RebuildMenu();
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _automationEngine.SnapshotChanged -= OnSnapshotChanged;
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
        _trayIcon.Dispose();
        _menu.Dispose();
        _disposed = true;
    }

    private void OnSnapshotChanged(object? sender, AutomationSnapshot snapshot)
    {
        Application.Current.Dispatcher.BeginInvoke(() =>
        {
            _notifyIcon.Text = "间奏 | Interlude";
        });
    }

    private void RebuildMenu()
    {
        _menu.Items.Clear();
        var settings = _settingsService.Current;
        var snapshot = _automationEngine.CurrentSnapshot;

        var player = string.IsNullOrWhiteSpace(settings.TargetPlayer.DisplayName)
            ? _localization.T("Common.NotConfigured")
            : settings.TargetPlayer.DisplayName;
        _menu.Items.Add(CreateStatusItem(_localization.Format(
            "Tray.Status",
            _localization.EnabledDisabled(settings.Enabled))));
        _menu.Items.Add(CreateStatusItem(_localization.Format("Tray.DefaultPlayer", player)));
        _menu.Items.Add(CreateStatusItem(_localization.Format(
            "Tray.State",
            _localization.AutomationState(snapshot.State))));
        _menu.Items.Add(new Forms.ToolStripSeparator());

        _menu.Items.Add(CreateActionItem(
            settings.Enabled ? _localization.T("Tray.DisableAutomation") : _localization.T("Tray.EnableAutomation"),
            (_, _) =>
            {
                settings.Enabled = !settings.Enabled;
                _settingsService.Save();
            }));

        var restoreItem = CreateActionItem(_localization.T("Tray.RestoreMusicNow"), async (_, _) =>
        {
            await _automationEngine.ImmediateRestoreAsync();
        });
        restoreItem.Enabled = _automationEngine.PausedByUs;
        _menu.Items.Add(restoreItem);

        _menu.Items.Add(CreateActionItem(_localization.T("Tray.ShowMainWindow"), (_, _) => _windowCoordinator.ShowMainWindow()));
        _menu.Items.Add(CreateActionItem(_localization.T("Tray.ChangeDefaultPlayer"), (_, _) => _windowCoordinator.ShowPlayerSelection()));
        _menu.Items.Add(CreateActionItem(_localization.T("Tray.Settings"), (_, _) => _windowCoordinator.ShowSettings()));
        _menu.Items.Add(CreateActionItem(_localization.T("Tray.OpenLogDirectory"), (_, _) => _windowCoordinator.OpenLogDirectory()));

        _menu.Items.Add(CreateActionItem(
            settings.StartWithWindows ? _localization.T("Tray.DisableStartWithWindows") : _localization.T("Tray.EnableStartWithWindows"),
            (_, _) =>
            {
                var enabled = !settings.StartWithWindows;
                _startupService.SetEnabled(enabled);
                settings.StartWithWindows = enabled;
                _settingsService.Save();
            }));

        _menu.Items.Add(new Forms.ToolStripSeparator());
        _menu.Items.Add(CreateActionItem(_localization.T("Tray.ExitInterlude"), (_, _) => _windowCoordinator.ExitApplication()));
    }

    private static Forms.ToolStripMenuItem CreateStatusItem(string text)
    {
        return new Forms.ToolStripMenuItem(text)
        {
            Enabled = false
        };
    }

    private static Forms.ToolStripMenuItem CreateActionItem(
        string text,
        EventHandler onClick)
    {
        var item = new Forms.ToolStripMenuItem(text);
        item.Click += onClick;
        return item;
    }

    private static Drawing.Icon CreateTrayIcon()
    {
        var resource = Application.GetResourceStream(new Uri("pack://application:,,,/Assets/interlude.ico"));
        if (resource is null)
        {
            return (Drawing.Icon)Drawing.SystemIcons.Application.Clone();
        }

        using var icon = new Drawing.Icon(resource.Stream);
        return (Drawing.Icon)icon.Clone();
    }
}
