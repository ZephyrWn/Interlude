using System.Windows;
using System.Windows.Input;
using Interlude.Infrastructure;
using Interlude.Models;
using Interlude.Services;

namespace Interlude.ViewModels;

public sealed class MainViewModel : ObservableObject
{
    private readonly SettingsService _settingsService;
    private readonly StartupService _startupService;
    private readonly AutomationEngine _automationEngine;
    private readonly LocalizationService _localization;
    private readonly WindowCoordinator _windowCoordinator;
    private readonly LoggingService _log;
    private AutomationSnapshot _snapshot;
    private MainPage _selectedPage = MainPage.Status;

    public MainViewModel(
        SettingsService settingsService,
        StartupService startupService,
        AutomationEngine automationEngine,
        LocalizationService localization,
        WindowCoordinator windowCoordinator,
        LoggingService log)
    {
        _settingsService = settingsService;
        _startupService = startupService;
        _automationEngine = automationEngine;
        _localization = localization;
        _windowCoordinator = windowCoordinator;
        _log = log;
        _snapshot = automationEngine.CurrentSnapshot;

        EnableCommand = new RelayCommand(() => SetEnabled(true), () => !IsEnabled);
        PauseAutomationCommand = new RelayCommand(() => SetEnabled(false), () => IsEnabled);
        ToggleAutomationCommand = new RelayCommand(ToggleAutomation);
        ChangePlayerCommand = new RelayCommand(() => _windowCoordinator.ShowPlayerSelection(Application.Current.MainWindow));
        OpenSettingsCommand = new RelayCommand(SelectSettingsPage);
        OpenLogsCommand = new RelayCommand(_windowCoordinator.OpenLogDirectory);
        ExitCommand = new RelayCommand(_windowCoordinator.ExitApplication);
        ImmediateRestoreCommand = new AsyncRelayCommand(() => _automationEngine.ImmediateRestoreAsync());
        ToggleStartupCommand = new RelayCommand(ToggleStartup);
        ShowStatusPageCommand = new RelayCommand(SelectStatusPage);
        ShowSettingsPageCommand = new RelayCommand(SelectSettingsPage);
        OpenIgnoredApplicationsCommand = new RelayCommand(() => _windowCoordinator.ShowIgnoredApplications(Application.Current.MainWindow));
        SaveSettingsCommand = new RelayCommand(SaveSettings);
        ResetSettingsCommand = new RelayCommand(ResetSettings);

        _automationEngine.SnapshotChanged += OnSnapshotChanged;
        _settingsService.SettingsChanged += OnSettingsChanged;
    }

    public ICommand EnableCommand { get; }

    public ICommand PauseAutomationCommand { get; }

    public ICommand ToggleAutomationCommand { get; }

    public ICommand ChangePlayerCommand { get; }

    public ICommand OpenSettingsCommand { get; }

    public ICommand OpenLogsCommand { get; }

    public ICommand ExitCommand { get; }

    public ICommand ImmediateRestoreCommand { get; }

    public ICommand ToggleStartupCommand { get; }

    public ICommand ShowStatusPageCommand { get; }

    public ICommand ShowSettingsPageCommand { get; }

    public ICommand OpenIgnoredApplicationsCommand { get; }

    public ICommand SaveSettingsCommand { get; }

    public ICommand ResetSettingsCommand { get; }

    public AppSettings Settings => _settingsService.Current;

    public IReadOnlyList<LocalizedOption<string>> LanguageOptions { get; } =
    [
        new(AppLanguage.ChineseSimplified, "中文"),
        new(AppLanguage.English, "English")
    ];

    public IReadOnlyList<LocalizedOption<DetectionMode>> DetectionModeOptions =>
        Enum.GetValues<DetectionMode>()
            .Select(mode => new LocalizedOption<DetectionMode>(mode, _localization.DetectionMode(mode)))
            .ToArray();

    public string SelectedLanguage
    {
        get => AppLanguage.Normalize(Settings.Language);
        set
        {
            var normalized = AppLanguage.Normalize(value);
            if (Settings.Language == normalized)
            {
                return;
            }

            _localization.SetLanguage(normalized, save: false);
            OnPropertyChanged();
            OnPropertyChanged(nameof(DetectionModeOptions));
        }
    }

    public MainPage SelectedPage
    {
        get => _selectedPage;
        private set
        {
            if (!SetProperty(ref _selectedPage, value))
            {
                return;
            }

            OnPropertyChanged(nameof(IsStatusPageSelected));
            OnPropertyChanged(nameof(IsSettingsPageSelected));
        }
    }

    public bool IsStatusPageSelected => SelectedPage == MainPage.Status;

    public bool IsSettingsPageSelected => SelectedPage == MainPage.Settings;

    public bool IsEnabled => _settingsService.Current.Enabled;

    public string IsEnabledText => _localization.EnabledDisabled(IsEnabled);

    public string AutomationToggleText => IsEnabled
        ? _localization.T("Main.PauseAutomation")
        : _localization.T("Main.Enable");

    public string TargetPlayer => string.IsNullOrWhiteSpace(_settingsService.Current.TargetPlayer.DisplayName)
        ? _localization.T("Common.NotConfigured")
        : _settingsService.Current.TargetPlayer.DisplayName;

    public PlayerPlaybackState PlaybackState => _snapshot.PlaybackState;

    public string PlaybackStateText => _localization.PlaybackState(PlaybackState);

    public AutomationState AutomationState => _snapshot.State;

    public string AutomationStateText => _localization.AutomationState(AutomationState);

    public bool PausedByUs => _snapshot.PausedByUs;

    public string PausedByUsText => _localization.YesNo(PausedByUs);

    public DetectionMode DetectionMode => _snapshot.DetectionMode;

    public string DetectionModeText => _localization.DetectionMode(DetectionMode);

    public string IgnoredApplicationsSummary => _localization.Format(
        "IgnoreApps.Summary",
        Settings.IgnoredApplications.Count);

    public bool StartWithWindows => _settingsService.Current.StartWithWindows;

    public string TargetAvailability => _snapshot.TargetAvailable
        ? _localization.T("Common.Available")
        : _localization.T("Common.Waiting");

    public string LogDirectory => _log.LogDirectory;

    public void SelectSettingsPage() => SelectPage(MainPage.Settings);

    private void SetEnabled(bool enabled)
    {
        _settingsService.Current.Enabled = enabled;
        _settingsService.Save();
    }

    private void ToggleAutomation()
    {
        SetEnabled(!IsEnabled);
    }

    private void ToggleStartup()
    {
        var enabled = !StartWithWindows;
        _startupService.SetEnabled(enabled);
        _settingsService.Current.StartWithWindows = enabled;
        _settingsService.Save();
    }

    private void SelectStatusPage() => SelectPage(MainPage.Status);

    private void SelectPage(MainPage page)
    {
        SelectedPage = page;
    }

    private void SaveSettings()
    {
        _localization.SetLanguage(Settings.Language, save: false);
        _startupService.SetEnabled(Settings.StartWithWindows);
        _settingsService.Save();
        RaiseSettingsProperties();
    }

    private void ResetSettings()
    {
        var language = AppLanguage.Normalize(Settings.Language);
        _startupService.SetEnabled(false);
        var settings = AppSettings.CreateDefault();
        settings.Language = language;
        _settingsService.Replace(settings);
        _localization.ApplyResources();
        RaiseSettingsProperties();
    }

    private void OnSnapshotChanged(object? sender, AutomationSnapshot snapshot)
    {
        Application.Current.Dispatcher.BeginInvoke(() =>
        {
            _snapshot = snapshot;
            RaiseAll();
        });
    }

    private void OnSettingsChanged(object? sender, EventArgs e)
    {
        Application.Current.Dispatcher.BeginInvoke(RaiseAll);
    }

    private void RaiseAll()
    {
        RaiseSettingsProperties();
        OnPropertyChanged(nameof(SelectedPage));
        OnPropertyChanged(nameof(IsStatusPageSelected));
        OnPropertyChanged(nameof(IsSettingsPageSelected));
        OnPropertyChanged(nameof(IsEnabled));
        OnPropertyChanged(nameof(IsEnabledText));
        OnPropertyChanged(nameof(AutomationToggleText));
        OnPropertyChanged(nameof(TargetPlayer));
        OnPropertyChanged(nameof(PlaybackState));
        OnPropertyChanged(nameof(PlaybackStateText));
        OnPropertyChanged(nameof(AutomationState));
        OnPropertyChanged(nameof(AutomationStateText));
        OnPropertyChanged(nameof(PausedByUs));
        OnPropertyChanged(nameof(PausedByUsText));
        OnPropertyChanged(nameof(DetectionMode));
        OnPropertyChanged(nameof(DetectionModeText));
        OnPropertyChanged(nameof(IgnoredApplicationsSummary));
        OnPropertyChanged(nameof(StartWithWindows));
        OnPropertyChanged(nameof(TargetAvailability));
        OnPropertyChanged(nameof(LogDirectory));

        if (EnableCommand is RelayCommand enable)
        {
            enable.RaiseCanExecuteChanged();
        }

        if (PauseAutomationCommand is RelayCommand pause)
        {
            pause.RaiseCanExecuteChanged();
        }
    }

    private void RaiseSettingsProperties()
    {
        OnPropertyChanged(nameof(Settings));
        OnPropertyChanged(nameof(SelectedLanguage));
        OnPropertyChanged(nameof(DetectionModeOptions));
        OnPropertyChanged(nameof(IgnoredApplicationsSummary));
    }
}

public enum MainPage
{
    Status,
    Settings
}
