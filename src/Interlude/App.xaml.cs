using System.Windows;
using System.Windows.Threading;
using Interlude.Infrastructure;
using Interlude.Services;

namespace Interlude;

public partial class App : System.Windows.Application
{
    private ServiceProvider? _services;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        _services = BuildServices();

        var log = _services.GetRequiredService<LoggingService>();
        ConfigureExceptionLogging(log);
        log.Info("Interlude starting.");

        var singleInstance = _services.GetRequiredService<SingleInstanceService>();
        if (!singleInstance.TryAcquire())
        {
            Shutdown();
            return;
        }

        var settingsService = _services.GetRequiredService<SettingsService>();
        var settings = settingsService.Load();
        var startupService = _services.GetRequiredService<StartupService>();
        settings.StartWithWindows = startupService.IsEnabled();
        var localization = _services.GetRequiredService<LocalizationService>();
        localization.ApplyResources();

        var automationEngine = _services.GetRequiredService<AutomationEngine>();
        var powerEvents = _services.GetRequiredService<PowerEventService>();
        powerEvents.Suspending += (_, _) => automationEngine.PauseForSleep();
        powerEvents.Resumed += (_, _) => automationEngine.ResumeAfterSleep();

        var windows = _services.GetRequiredService<WindowCoordinator>();
        var minimized = e.Args.Any(static arg =>
            string.Equals(arg, "--minimized", StringComparison.OrdinalIgnoreCase));

        if (!settings.HasLanguageSelection && !windows.ShowLanguageSelectionWindow())
        {
            Shutdown();
            return;
        }

        _ = _services.GetRequiredService<TrayIconService>();

        if (!settings.HasTargetPlayer)
        {
            windows.ShowFirstRunWindow();
        }

        if (settingsService.Current.HasTargetPlayer)
        {
            automationEngine.Start();
        }

        if (!minimized || !settingsService.Current.MinimizeToTray || !settingsService.Current.HasTargetPlayer)
        {
            windows.ShowMainWindow();
        }
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        if (_services is not null)
        {
            var log = _services.GetRequiredService<LoggingService>();
            log.Info("Interlude exiting.");
            await _services.GetRequiredService<AutomationEngine>().StopAsync();
            _services.Dispose();
        }

        base.OnExit(e);
    }

    private static ServiceProvider BuildServices()
    {
        var registry = new ServiceRegistry();
        registry
            .AddSingleton(_ => new LoggingService())
            .AddSingleton(provider => new SettingsService(provider.GetRequiredService<LoggingService>()))
            .AddSingleton(provider => new LocalizationService(provider.GetRequiredService<SettingsService>()))
            .AddSingleton(provider => new StartupService(provider.GetRequiredService<LoggingService>()))
            .AddSingleton(_ => new SingleInstanceService())
            .AddSingleton(_ => new PowerEventService())
            .AddSingleton(_ => new IgnoredApplicationMatcher())
            .AddSingleton(provider => new MediaSessionService(
                provider.GetRequiredService<IgnoredApplicationMatcher>(),
                provider.GetRequiredService<LoggingService>()))
            .AddSingleton(provider => new AudioActivityService(
                provider.GetRequiredService<IgnoredApplicationMatcher>(),
                provider.GetRequiredService<LoggingService>()))
            .AddSingleton(provider => new AudioSessionVolumeService(
                provider.GetRequiredService<IgnoredApplicationMatcher>(),
                provider.GetRequiredService<LoggingService>()))
            .AddSingleton(provider => new PlayerFadeService(
                provider.GetRequiredService<AudioSessionVolumeService>(),
                provider.GetRequiredService<LoggingService>()))
            .AddSingleton(provider => new WindowPlacementService(
                provider.GetRequiredService<SettingsService>(),
                provider.GetRequiredService<LoggingService>()))
            .AddSingleton(provider => new AutomationEngine(
                provider.GetRequiredService<SettingsService>(),
                provider.GetRequiredService<MediaSessionService>(),
                provider.GetRequiredService<AudioActivityService>(),
                provider.GetRequiredService<PlayerFadeService>(),
                provider.GetRequiredService<LoggingService>()))
            .AddSingleton(provider => new WindowCoordinator(
                provider,
                provider.GetRequiredService<LoggingService>()))
            .AddSingleton(provider => new TrayIconService(
                provider.GetRequiredService<SettingsService>(),
                provider.GetRequiredService<StartupService>(),
                provider.GetRequiredService<AutomationEngine>(),
                provider.GetRequiredService<LocalizationService>(),
                provider.GetRequiredService<WindowCoordinator>()));

        return registry.Build();
    }

    private void ConfigureExceptionLogging(LoggingService log)
    {
        DispatcherUnhandledException += (_, args) =>
        {
            log.Error("Unhandled UI exception.", args.Exception);
            args.Handled = true;
            MessageBox.Show(
                Current.Resources["App.UnexpectedError"] as string
                    ?? "Interlude hit an unexpected error. Details were written to the log directory.",
                "Interlude",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        };

        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
        {
            if (args.ExceptionObject is Exception exception)
            {
                log.Error("Unhandled application exception.", exception);
            }
        };

        TaskScheduler.UnobservedTaskException += (_, args) =>
        {
            log.Error("Unobserved task exception.", args.Exception);
            args.SetObserved();
        };
    }
}
