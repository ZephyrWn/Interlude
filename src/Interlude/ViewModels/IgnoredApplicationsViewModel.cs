using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Interlude.Infrastructure;
using Interlude.Models;
using Interlude.Services;
using Microsoft.Win32;

namespace Interlude.ViewModels;

public sealed class IgnoredApplicationsViewModel : ObservableObject
{
    private readonly SettingsService _settingsService;
    private readonly AudioActivityService _audioActivityService;
    private readonly LocalizationService _localization;
    private readonly LoggingService _log;
    private readonly List<IgnoredApplicationItemViewModel> _allItems = [];
    private string _searchText = string.Empty;
    private string _statusMessage = string.Empty;

    public IgnoredApplicationsViewModel(
        SettingsService settingsService,
        AudioActivityService audioActivityService,
        LocalizationService localization,
        LoggingService log)
    {
        _settingsService = settingsService;
        _audioActivityService = audioActivityService;
        _localization = localization;
        _log = log;
        _statusMessage = _localization.T("IgnoreApps.Status.Ready");

        RefreshCommand = new AsyncRelayCommand(RefreshAsync);
        AddRunningAppsCommand = new AsyncRelayCommand(RefreshAsync);
        AddManualCommand = new RelayCommand(AddManualApplication);
        SaveCommand = new RelayCommand(Save);
        CancelCommand = new RelayCommand(() => RequestClose?.Invoke(this, false));
    }

    public event EventHandler<bool>? RequestClose;

    public ObservableCollection<IgnoredApplicationItemViewModel> Applications { get; } = [];

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
            {
                ApplyFilter();
            }
        }
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    public ICommand RefreshCommand { get; }

    public ICommand AddRunningAppsCommand { get; }

    public ICommand AddManualCommand { get; }

    public ICommand SaveCommand { get; }

    public ICommand CancelCommand { get; }

    public async Task RefreshAsync()
    {
        try
        {
            StatusMessage = _localization.T("IgnoreApps.Status.Refreshing");
            var currentSettings = _settingsService.Current;
            var existing = currentSettings.IgnoredApplications.ToArray();
            var sessions = await _audioActivityService.GetCurrentSessionsAsync(currentSettings);

            _allItems.Clear();
            foreach (var item in existing.Select(CreateFromIgnoredApplication))
            {
                AddOrMerge(item);
            }

            foreach (var session in sessions
                         .Where(static session => session.ProcessId > 0)
                         .OrderBy(static session => session.ProcessName, StringComparer.OrdinalIgnoreCase))
            {
                var item = new IgnoredApplicationItemViewModel
                {
                    DisplayName = ResolveDisplayName(session),
                    ProcessName = session.ProcessName,
                    ExecutablePath = session.ExecutablePath,
                    Icon = LoadIcon(session.ExecutablePath),
                    IsIgnored = currentSettings.IgnoredApplications.Any(app =>
                        Matches(app, session.ProcessName, session.ExecutablePath))
                };
                AddOrMerge(item);
            }

            ApplyFilter();
            StatusMessage = _localization.Format("IgnoreApps.Status.FoundApplications", _allItems.Count);
        }
        catch (Exception ex)
        {
            StatusMessage = _localization.T("IgnoreApps.Status.RefreshFailed");
            _log.Error("Ignored application refresh failed.", ex);
        }
    }

    private void AddManualApplication()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Filter = _localization.T("IgnoreApps.ExecutableFilter"),
            CheckFileExists = true,
            Multiselect = false
        };

        if (dialog.ShowDialog() != true)
        {
            return;
        }

        var path = dialog.FileName;
        var processName = Path.GetFileName(path);
        var displayName = Path.GetFileNameWithoutExtension(path);

        try
        {
            var versionInfo = System.Diagnostics.FileVersionInfo.GetVersionInfo(path);
            if (!string.IsNullOrWhiteSpace(versionInfo.FileDescription))
            {
                displayName = versionInfo.FileDescription;
            }
        }
        catch
        {
            // File metadata is optional.
        }

        AddOrMerge(new IgnoredApplicationItemViewModel
        {
            DisplayName = displayName,
            ProcessName = processName,
            ExecutablePath = path,
            Icon = LoadIcon(path),
            IsIgnored = true
        });
        ApplyFilter();
    }

    private void Save()
    {
        _settingsService.Current.IgnoredApplications = _allItems
            .Where(static item => item.IsIgnored)
            .Select(static item => item.ToIgnoredApplication())
            .ToList();
        _settingsService.Save();
        RequestClose?.Invoke(this, true);
    }

    private void ApplyFilter()
    {
        var query = SearchText.Trim();
        Applications.Clear();
        foreach (var item in _allItems
                     .Where(item => string.IsNullOrWhiteSpace(query) ||
                         item.SearchText.Contains(query, StringComparison.OrdinalIgnoreCase))
                     .OrderByDescending(static item => item.IsIgnored)
                     .ThenBy(static item => item.DisplayName, StringComparer.OrdinalIgnoreCase))
        {
            Applications.Add(item);
        }
    }

    private void AddOrMerge(IgnoredApplicationItemViewModel item)
    {
        var existing = _allItems.FirstOrDefault(existing =>
            MatchesIdentity(existing, item.ProcessName, item.ExecutablePath, item.SourceAppUserModelId));
        if (existing is not null)
        {
            existing.IsIgnored |= item.IsIgnored;
            return;
        }

        _allItems.Add(item);
    }

    private static IgnoredApplicationItemViewModel CreateFromIgnoredApplication(IgnoredApplication app)
    {
        return new IgnoredApplicationItemViewModel
        {
            DisplayName = string.IsNullOrWhiteSpace(app.DisplayName)
                ? Path.GetFileNameWithoutExtension(app.ProcessName)
                : app.DisplayName,
            ProcessName = app.ProcessName,
            ExecutablePath = app.ExecutablePath,
            SourceAppUserModelId = app.SourceAppUserModelId,
            Icon = LoadIcon(app.ExecutablePath),
            IsIgnored = true
        };
    }

    private static string ResolveDisplayName(AudioSessionInfo session)
    {
        if (!string.IsNullOrWhiteSpace(session.ExecutablePath))
        {
            try
            {
                var versionInfo = System.Diagnostics.FileVersionInfo.GetVersionInfo(session.ExecutablePath);
                if (!string.IsNullOrWhiteSpace(versionInfo.FileDescription))
                {
                    return versionInfo.FileDescription.Trim();
                }
            }
            catch
            {
                // File metadata is optional.
            }
        }

        return Path.GetFileNameWithoutExtension(session.ProcessName);
    }

    private static ImageSource? LoadIcon(string executablePath)
    {
        if (string.IsNullOrWhiteSpace(executablePath) || !File.Exists(executablePath))
        {
            return null;
        }

        try
        {
            using var icon = System.Drawing.Icon.ExtractAssociatedIcon(executablePath);
            if (icon is null)
            {
                return null;
            }

            var source = Imaging.CreateBitmapSourceFromHIcon(
                icon.Handle,
                System.Windows.Int32Rect.Empty,
                BitmapSizeOptions.FromWidthAndHeight(32, 32));
            source.Freeze();
            return source;
        }
        catch
        {
            return null;
        }
    }

    private static bool Matches(IgnoredApplication app, string processName, string executablePath)
    {
        return (!string.IsNullOrWhiteSpace(app.ExecutablePath) &&
                !string.IsNullOrWhiteSpace(executablePath) &&
                string.Equals(app.ExecutablePath, executablePath, StringComparison.OrdinalIgnoreCase)) ||
            (!string.IsNullOrWhiteSpace(app.ProcessName) &&
             string.Equals(app.ProcessName, processName, StringComparison.OrdinalIgnoreCase));
    }

    private static bool MatchesIdentity(
        IgnoredApplicationItemViewModel item,
        string processName,
        string executablePath,
        string sourceAppUserModelId)
    {
        return (!string.IsNullOrWhiteSpace(item.ExecutablePath) &&
                !string.IsNullOrWhiteSpace(executablePath) &&
                string.Equals(item.ExecutablePath, executablePath, StringComparison.OrdinalIgnoreCase)) ||
            (!string.IsNullOrWhiteSpace(item.ProcessName) &&
             !string.IsNullOrWhiteSpace(processName) &&
             string.Equals(item.ProcessName, processName, StringComparison.OrdinalIgnoreCase)) ||
            (!string.IsNullOrWhiteSpace(item.SourceAppUserModelId) &&
             !string.IsNullOrWhiteSpace(sourceAppUserModelId) &&
             string.Equals(item.SourceAppUserModelId, sourceAppUserModelId, StringComparison.OrdinalIgnoreCase));
    }
}
