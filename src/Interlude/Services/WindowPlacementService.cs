using System.Windows;
using Interlude.Models;

namespace Interlude.Services;

public sealed class WindowPlacementService
{
    private readonly SettingsService _settingsService;
    private readonly LoggingService _log;

    public WindowPlacementService(SettingsService settingsService, LoggingService log)
    {
        _settingsService = settingsService;
        _log = log;
    }

    public void Restore(Window window)
    {
        var placement = _settingsService.Current.MainWindowPlacement;
        if (placement.Left is null || placement.Top is null)
        {
            window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            return;
        }

        window.WindowStartupLocation = WindowStartupLocation.Manual;
        window.Left = placement.Left.Value;
        window.Top = placement.Top.Value;

        if (!IsVisibleOnAnyScreen(window))
        {
            window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            window.Left = double.NaN;
            window.Top = double.NaN;
        }
    }

    public void Save(Window window)
    {
        if (window.WindowState != WindowState.Normal)
        {
            return;
        }

        try
        {
            _settingsService.Current.MainWindowPlacement.Left = window.Left;
            _settingsService.Current.MainWindowPlacement.Top = window.Top;
            _settingsService.Save();
        }
        catch (Exception ex)
        {
            _log.Error("Failed to save main window placement.", ex);
        }
    }

    private static bool IsVisibleOnAnyScreen(Window window)
    {
        var rect = new System.Drawing.Rectangle(
            (int)Math.Round(window.Left),
            (int)Math.Round(window.Top),
            Math.Max(1, (int)Math.Round(window.Width)),
            Math.Max(1, (int)Math.Round(window.Height)));

        return System.Windows.Forms.Screen.AllScreens.Any(screen =>
            screen.WorkingArea.IntersectsWith(rect));
    }
}
