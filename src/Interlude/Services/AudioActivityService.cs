using System.Diagnostics;
using System.Runtime.InteropServices;
using Interlude.Models;

namespace Interlude.Services;

public sealed class AudioActivityService
{
    private static readonly StringComparer ProcessComparer = StringComparer.OrdinalIgnoreCase;
    private readonly IgnoredApplicationMatcher _ignoredApplicationMatcher;
    private readonly LoggingService _log;
    private DateTime _lastAudioErrorUtc = DateTime.MinValue;
    private string _lastMatchedTargetSignature = string.Empty;

    public AudioActivityService(IgnoredApplicationMatcher ignoredApplicationMatcher, LoggingService log)
    {
        _ignoredApplicationMatcher = ignoredApplicationMatcher;
        _log = log;
    }

    public Task<IReadOnlyList<AudioSessionInfo>> GetCurrentSessionsAsync(
        AppSettings settings,
        CancellationToken cancellationToken = default)
    {
        return Task.Run(() => (IReadOnlyList<AudioSessionInfo>)EnumerateSessions(settings), cancellationToken);
    }

    public async Task<InterruptionSnapshot> GetInterruptionSnapshotAsync(
        AppSettings settings,
        CancellationToken cancellationToken = default)
    {
        var sessions = await GetCurrentSessionsAsync(settings, cancellationToken);
        var triggers = sessions
            .Where(session =>
                session.IsAudible &&
                !session.IsTargetPlayer &&
                !session.IsExcluded &&
                !(settings.Detection.IgnoreSystemSounds && session.IsSystemSound))
            .Select(session => session.ProcessName)
            .Distinct(ProcessComparer)
            .OrderBy(static name => name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new InterruptionSnapshot
        {
            RawActive = triggers.Count > 0,
            AudioSessions = sessions,
            TriggeringProcesses = triggers
        };
    }

    public async Task<IReadOnlyList<string>> FindMatchingProcessNamesAsync(
        string sourceAppUserModelId,
        AppSettings settings,
        CancellationToken cancellationToken = default)
    {
        var candidateNames = PlayerIdentityResolver
            .GetCandidateProcessNames(sourceAppUserModelId)
            .ToHashSet(ProcessComparer);

        if (candidateNames.Count == 0)
        {
            return [];
        }

        var sessions = await GetCurrentSessionsAsync(settings, cancellationToken);
        return sessions
            .Where(session =>
                !session.IsSystemSound &&
                candidateNames.Contains(PlayerIdentityResolver.NormalizeProcessName(session.ProcessName)))
            .Select(session => PlayerIdentityResolver.NormalizeProcessName(session.ProcessName))
            .Distinct(ProcessComparer)
            .OrderBy(static name => name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private List<AudioSessionInfo> EnumerateSessions(AppSettings settings)
    {
        var result = new List<AudioSessionInfo>();
        IMMDeviceEnumerator? enumerator = null;
        IMMDevice? device = null;
        object? managerObject = null;
        IAudioSessionEnumerator? sessionEnumerator = null;

        try
        {
            enumerator = (IMMDeviceEnumerator)(object)new MMDeviceEnumeratorComObject();
            ThrowIfFailed(enumerator.GetDefaultAudioEndpoint(
                EDataFlow.ERender,
                ERole.EMultimedia,
                out device));

            var managerId = typeof(IAudioSessionManager2).GUID;
            ThrowIfFailed(device.Activate(
                ref managerId,
                ClsCtx.All,
                IntPtr.Zero,
                out managerObject));

            var manager = (IAudioSessionManager2)managerObject;
            ThrowIfFailed(manager.GetSessionEnumerator(out sessionEnumerator));
            ThrowIfFailed(sessionEnumerator.GetCount(out var count));
            var excludedNames = settings.ExcludedProcesses
                .Select(PlayerIdentityResolver.NormalizeProcessName)
                .ToHashSet(ProcessComparer);

            for (var index = 0; index < count; index++)
            {
                IAudioSessionControl? sessionControl = null;
                try
                {
                    ThrowIfFailed(sessionEnumerator.GetSession(index, out sessionControl));
                    result.Add(CreateSessionInfo(
                        sessionControl,
                        settings,
                        excludedNames));
                }
                catch
                {
                    // Individual sessions can disappear while Windows is enumerating them.
                }
                finally
                {
                    ReleaseComObject(sessionControl);
                }
            }

            CacheMatchedTargetProcessNames(settings, result);
        }
        catch (Exception ex)
        {
            LogAudioError(ex);
        }
        finally
        {
            ReleaseComObject(sessionEnumerator);
            ReleaseComObject(managerObject);
            ReleaseComObject(device);
            ReleaseComObject(enumerator);
        }

        return result;
    }

    private AudioSessionInfo CreateSessionInfo(
        IAudioSessionControl sessionControl,
        AppSettings settings,
        ISet<string> excludedNames)
    {
        _ = sessionControl.GetState(out var state);

        var processId = 0;
        var isSystemSound = false;
        if (sessionControl is IAudioSessionControl2 sessionControl2)
        {
            if (sessionControl2.GetProcessId(out var pid) >= 0)
            {
                processId = unchecked((int)pid);
            }

            isSystemSound = sessionControl2.IsSystemSoundsSession() == 0;
        }

        var peak = 0.0f;
        if (sessionControl is IAudioMeterInformation meter)
        {
            _ = meter.GetPeakValue(out peak);
        }

        var identity = ProcessInfoResolver.Resolve(processId);
        var normalizedName = PlayerIdentityResolver.NormalizeProcessName(identity.ProcessName);

        var isOwnProcess = processId == Environment.ProcessId;
        var sessionInfo = new AudioSessionInfo
        {
            ProcessName = normalizedName,
            ExecutablePath = identity.ExecutablePath,
            ProcessId = processId
        };
        var isTargetPlayer = _ignoredApplicationMatcher.IsTargetPlayerAudioSession(sessionInfo, settings);
        var isExcluded = excludedNames.Contains(normalizedName) ||
            isOwnProcess ||
            _ignoredApplicationMatcher.IsIgnoredAudioSession(sessionInfo, settings);

        return new AudioSessionInfo
        {
            ProcessName = normalizedName,
            ExecutablePath = identity.ExecutablePath,
            ProcessId = processId,
            PeakValue = peak,
            IsAudible = state == AudioSessionState.Active &&
                peak >= settings.Detection.StartThreshold,
            IsTargetPlayer = isTargetPlayer,
            IsExcluded = isExcluded,
            IsSystemSound = isSystemSound,
            State = state.ToString()
        };
    }

    private void CacheMatchedTargetProcessNames(
        AppSettings settings,
        IReadOnlyList<AudioSessionInfo> sessions)
    {
        if (!settings.TargetPlayer.IsConfigured)
        {
            return;
        }

        var matchedNames = sessions
            .Where(static session =>
                session.IsTargetPlayer &&
                !session.IsSystemSound &&
                !session.ProcessName.StartsWith("PID ", StringComparison.OrdinalIgnoreCase) &&
                !session.ProcessName.Equals("Unknown", StringComparison.OrdinalIgnoreCase))
            .Select(static session => PlayerIdentityResolver.NormalizeProcessName(session.ProcessName))
            .Distinct(ProcessComparer)
            .OrderBy(static name => name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (matchedNames.Count == 0)
        {
            return;
        }

        var currentNames = settings.TargetPlayer.TargetProcessNames
            .Select(PlayerIdentityResolver.NormalizeProcessName)
            .OrderBy(static name => name, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (currentNames.SequenceEqual(matchedNames, StringComparer.OrdinalIgnoreCase))
        {
            return;
        }

        settings.TargetPlayer.TargetProcessNames = matchedNames;

        var signature = string.Join("|", matchedNames);
        if (!signature.Equals(_lastMatchedTargetSignature, StringComparison.OrdinalIgnoreCase))
        {
            _lastMatchedTargetSignature = signature;
            _log.Info($"Matched target player audio process: {string.Join(", ", matchedNames)}.");
        }
    }

    private static void ThrowIfFailed(int hresult)
    {
        if (hresult < 0)
        {
            Marshal.ThrowExceptionForHR(hresult);
        }
    }

    private static void ReleaseComObject(object? value)
    {
        if (value is not null && Marshal.IsComObject(value))
        {
            Marshal.ReleaseComObject(value);
        }
    }

    private void LogAudioError(Exception exception)
    {
        var now = DateTime.UtcNow;
        if (now - _lastAudioErrorUtc < TimeSpan.FromSeconds(30))
        {
            return;
        }

        _lastAudioErrorUtc = now;
        _log.Error("Failed to enumerate Core Audio sessions.", exception);
    }
}
