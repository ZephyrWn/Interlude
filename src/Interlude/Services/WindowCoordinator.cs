using System.Diagnostics;
using System.Windows;
using Interlude.Infrastructure;
using Interlude.ViewModels;
using Interlude.Views;

namespace Interlude.Services;

public sealed class WindowCoordinator
{
    private readonly ServiceProvider _services;
    private readonly LoggingService _log;
    private MainWindow? _mainWindow;
    private bool _isExiting;

    public WindowCoordinator(ServiceProvider services, LoggingService log)
    {
        _services = services;
        _log = log;
    }

    public bool IsExiting => _isExiting;

    public void ShowMainWindow()
    {
        if (_mainWindow is null)
        {
            var viewModel = CreateMainViewModel();
            _mainWindow = new MainWindow(
                viewModel,
                this,
                _services.GetRequiredService<WindowPlacementService>());
            _mainWindow.Closed += (_, _) =>
            {
                _mainWindow = null;
            };
            Application.Current.MainWindow = _mainWindow;
        }

        _mainWindow.Show();
        if (_mainWindow.WindowState == WindowState.Minimized)
        {
            _mainWindow.WindowState = WindowState.Normal;
        }

        _mainWindow.Activate();
    }

    public bool ShowFirstRunWindow()
    {
        var viewModel = new FirstRunViewModel(this);
        var window = new FirstRunWindow(viewModel);
        return window.ShowDialog() == true;
    }

    public bool ShowLanguageSelectionWindow()
    {
        var window = new LanguageSelectionWindow(
            _services.GetRequiredService<LocalizationService>());
        return window.ShowDialog() == true;
    }

    public bool ShowPlayerSelection(Window? owner = null)
    {
        var viewModel = CreatePlayerSelectionViewModel();
        var window = new PlayerSelectionWindow(viewModel);
        var actualOwner = owner
            ?? Application.Current.Windows.OfType<Window>().FirstOrDefault(static candidate => candidate.IsActive)
            ?? _mainWindow;
        if (actualOwner is not null)
        {
            window.Owner = actualOwner;
        }

        return window.ShowDialog() == true;
    }

    public bool ShowIgnoredApplications(Window? owner = null)
    {
        var viewModel = CreateIgnoredApplicationsViewModel();
        var window = new IgnoredApplicationsWindow(viewModel);
        var actualOwner = owner
            ?? Application.Current.Windows.OfType<Window>().FirstOrDefault(static candidate => candidate.IsActive)
            ?? _mainWindow;
        if (actualOwner is not null)
        {
            window.Owner = actualOwner;
        }

        return window.ShowDialog() == true;
    }

    public void ShowSettings(Window? owner = null)
    {
        ShowMainWindow();
        _mainWindow?.SelectSettingsPage();
    }

    public void OpenLogDirectory()
    {
        var logDirectory = _services.GetRequiredService<LoggingService>().LogDirectory;
        Directory.CreateDirectory(logDirectory);
        Process.Start(new ProcessStartInfo
        {
            FileName = logDirectory,
            UseShellExecute = true
        });
    }

    public void ExitApplication()
    {
        _isExiting = true;
        _log.Info("User requested application exit.");
        Application.Current.Shutdown();
    }

    public bool ShouldCloseToTray()
    {
        return !_isExiting &&
            _services.GetRequiredService<SettingsService>().Current.CloseToTray;
    }

    private MainViewModel CreateMainViewModel()
    {
        return new MainViewModel(
            _services.GetRequiredService<SettingsService>(),
            _services.GetRequiredService<StartupService>(),
            _services.GetRequiredService<AutomationEngine>(),
            _services.GetRequiredService<LocalizationService>(),
            this,
            _services.GetRequiredService<LoggingService>());
    }

    private PlayerSelectionViewModel CreatePlayerSelectionViewModel()
    {
        return new PlayerSelectionViewModel(
            _services.GetRequiredService<SettingsService>(),
            _services.GetRequiredService<MediaSessionService>(),
            _services.GetRequiredService<AudioActivityService>(),
            _services.GetRequiredService<LocalizationService>(),
            _services.GetRequiredService<LoggingService>());
    }

    private IgnoredApplicationsViewModel CreateIgnoredApplicationsViewModel()
    {
        return new IgnoredApplicationsViewModel(
            _services.GetRequiredService<SettingsService>(),
            _services.GetRequiredService<AudioActivityService>(),
            _services.GetRequiredService<LocalizationService>(),
            _services.GetRequiredService<LoggingService>());
    }
}
