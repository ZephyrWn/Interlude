using System.Collections.ObjectModel;
using System.Windows.Input;
using Interlude.Infrastructure;
using Interlude.Models;
using Interlude.Services;

namespace Interlude.ViewModels;

public sealed class PlayerSelectionViewModel : ObservableObject
{
    private readonly SettingsService _settingsService;
    private readonly MediaSessionService _mediaSessionService;
    private readonly AudioActivityService _audioActivityService;
    private readonly LocalizationService _localization;
    private readonly LoggingService _log;
    private MediaSessionInfo? _selectedMediaSession;
    private string _statusMessage;

    public PlayerSelectionViewModel(
        SettingsService settingsService,
        MediaSessionService mediaSessionService,
        AudioActivityService audioActivityService,
        LocalizationService localization,
        LoggingService log)
    {
        _settingsService = settingsService;
        _mediaSessionService = mediaSessionService;
        _audioActivityService = audioActivityService;
        _localization = localization;
        _log = log;
        _statusMessage = _localization.T("PlayerSelection.Status.OpenPlayer");

        RefreshCommand = new AsyncRelayCommand(RefreshAsync);
        SetAsDefaultCommand = new AsyncRelayCommand(SetAsDefaultAsync, () => SelectedMediaSession is not null);
    }

    public event EventHandler? SelectionCompleted;

    public ObservableCollection<MediaSessionInfo> MediaSessions { get; } = [];

    public bool HasMediaSessions => MediaSessions.Count > 0;

    public bool HasNoMediaSessions => MediaSessions.Count == 0;

    public MediaSessionInfo? SelectedMediaSession
    {
        get => _selectedMediaSession;
        set
        {
            if (SetProperty(ref _selectedMediaSession, value))
            {
                RaiseCommandStates();
            }
        }
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    public ICommand RefreshCommand { get; }

    public ICommand SetAsDefaultCommand { get; }

    public async Task RefreshAsync()
    {
        try
        {
            StatusMessage = _localization.T("PlayerSelection.Status.Refreshing");
            var mediaSessions = await _mediaSessionService.GetSessionsAsync();
            var previousSourceAppUserModelId =
                SelectedMediaSession?.SourceAppUserModelId ??
                _settingsService.Current.TargetPlayer.SourceAppUserModelId;

            MediaSessions.Clear();
            foreach (var session in mediaSessions.OrderBy(static s => s.DisplayName, StringComparer.OrdinalIgnoreCase))
            {
                MediaSessions.Add(session);
            }
            OnMediaSessionsChanged();

            SelectedMediaSession = MediaSessions.FirstOrDefault(session =>
                    string.Equals(
                        session.SourceAppUserModelId,
                        previousSourceAppUserModelId,
                        StringComparison.OrdinalIgnoreCase))
                ?? (MediaSessions.Count == 1 ? MediaSessions[0] : null);

            StatusMessage = MediaSessions.Count == 0
                ? _localization.T("PlayerSelection.Status.NoMediaSessions")
                : _localization.Format("PlayerSelection.Status.FoundMediaSessions", MediaSessions.Count);
        }
        catch (Exception ex)
        {
            StatusMessage = _localization.T("PlayerSelection.Status.RefreshFailed");
            _log.Error("Player selection refresh failed.", ex);
        }
    }

    private async Task SetAsDefaultAsync()
    {
        if (SelectedMediaSession is null)
        {
            StatusMessage = _localization.T("PlayerSelection.Status.SelectMediaSessionFirst");
            return;
        }

        var matchedProcessNames = await _audioActivityService.FindMatchingProcessNamesAsync(
            SelectedMediaSession.SourceAppUserModelId,
            _settingsService.Current);

        var settings = _settingsService.Current;
        settings.TargetPlayer = new TargetPlayerConfig
        {
            DisplayName = SelectedMediaSession.DisplayName,
            SourceAppUserModelId = SelectedMediaSession.SourceAppUserModelId,
            TargetProcessNames = matchedProcessNames.ToList()
        };
        settings.FirstRunCompleted = true;
        _settingsService.Save();
        _log.Info(matchedProcessNames.Count == 0
            ? $"Target player configured: {settings.TargetPlayer.DisplayName}. Audio process matching is pending."
            : $"Target player configured: {settings.TargetPlayer.DisplayName}. Matched audio processes: {string.Join(", ", matchedProcessNames)}.");
        StatusMessage = _localization.T("PlayerSelection.Status.DefaultPlayerSaved");
        SelectionCompleted?.Invoke(this, EventArgs.Empty);
    }

    private void RaiseCommandStates()
    {
        if (SetAsDefaultCommand is AsyncRelayCommand save)
        {
            save.RaiseCanExecuteChanged();
        }
    }

    private void OnMediaSessionsChanged()
    {
        OnPropertyChanged(nameof(HasMediaSessions));
        OnPropertyChanged(nameof(HasNoMediaSessions));
    }
}
