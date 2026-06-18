using System.Runtime.InteropServices;
using Interlude.Models;

namespace Interlude.Services;

public sealed class AudioSessionVolumeService
{
    public static readonly TimeSpan DefaultFadeDuration = TimeSpan.FromMilliseconds(500);
    private const int FadeSteps = 20;

    private readonly IgnoredApplicationMatcher _matcher;
    private readonly LoggingService _log;
    private DateTime _lastVolumeErrorUtc = DateTime.MinValue;

    public AudioSessionVolumeService(IgnoredApplicationMatcher matcher, LoggingService log)
    {
        _matcher = matcher;
        _log = log;
    }

    public Task<float?> GetTargetVolumeAsync(
        AppSettings settings,
        CancellationToken cancellationToken = default)
    {
        return Task.Run<float?>(() =>
        {
            var sessions = EnumerateTargetVolumes(settings);
            try
            {
                return sessions.Count == 0
                    ? null
                    : sessions.Average(static session => session.Volume);
            }
            finally
            {
                ReleaseVolumeSessions(sessions);
            }
        }, cancellationToken);
    }

    public Task<bool> SetTargetVolumeAsync(
        AppSettings settings,
        float volume,
        CancellationToken cancellationToken = default)
    {
        return Task.Run(() => SetTargetVolume(settings, volume), cancellationToken);
    }

    public async Task<bool> FadeTargetVolumeAsync(
        AppSettings settings,
        float from,
        float to,
        TimeSpan duration,
        CancellationToken cancellationToken = default)
    {
        var touched = false;
        var delay = TimeSpan.FromMilliseconds(Math.Max(10, duration.TotalMilliseconds / FadeSteps));

        for (var step = 0; step <= FadeSteps; step++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var progress = (float)step / FadeSteps;
            var eased = progress * progress * (3 - (2 * progress));
            var volume = Clamp01(from + ((to - from) * eased));
            touched |= await SetTargetVolumeAsync(settings, volume, cancellationToken);

            if (step < FadeSteps)
            {
                await Task.Delay(delay, cancellationToken);
            }
        }

        return touched;
    }

    private bool SetTargetVolume(AppSettings settings, float volume)
    {
        var sessions = EnumerateTargetVolumes(settings);
        foreach (var session in sessions)
        {
            try
            {
                var eventContext = Guid.Empty;
                session.VolumeControl.SetMasterVolume(Clamp01(volume), ref eventContext);
            }
            catch (Exception ex)
            {
                LogVolumeError(ex);
            }
            finally
            {
                ReleaseComObject(session.VolumeControl);
            }
        }

        return sessions.Count > 0;
    }

    private List<TargetVolumeSession> EnumerateTargetVolumes(AppSettings settings)
    {
        var result = new List<TargetVolumeSession>();
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

            for (var index = 0; index < count; index++)
            {
                IAudioSessionControl? sessionControl = null;
                var keepVolumeControl = false;
                try
                {
                    ThrowIfFailed(sessionEnumerator.GetSession(index, out sessionControl));
                    if (sessionControl is not ISimpleAudioVolume volumeControl ||
                        !IsTargetSession(sessionControl, settings))
                    {
                        continue;
                    }

                    ThrowIfFailed(volumeControl.GetMasterVolume(out var volume));
                    result.Add(new TargetVolumeSession(volumeControl, Clamp01(volume)));
                    keepVolumeControl = true;
                }
                catch
                {
                }
                finally
                {
                    if (!keepVolumeControl)
                    {
                        ReleaseComObject(sessionControl);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            LogVolumeError(ex);
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

    private bool IsTargetSession(IAudioSessionControl sessionControl, AppSettings settings)
    {
        var processId = 0;
        if (sessionControl is IAudioSessionControl2 sessionControl2 &&
            sessionControl2.GetProcessId(out var pid) >= 0)
        {
            processId = unchecked((int)pid);
        }

        var identity = ProcessInfoResolver.Resolve(processId);
        var info = new AudioSessionInfo
        {
            ProcessId = processId,
            ProcessName = identity.ProcessName,
            ExecutablePath = identity.ExecutablePath
        };

        return _matcher.IsTargetPlayerAudioSession(info, settings);
    }

    private static float Clamp01(float value)
    {
        return Math.Clamp(value, 0.0f, 1.0f);
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

    private static void ReleaseVolumeSessions(IEnumerable<TargetVolumeSession> sessions)
    {
        foreach (var session in sessions)
        {
            ReleaseComObject(session.VolumeControl);
        }
    }

    private void LogVolumeError(Exception exception)
    {
        var now = DateTime.UtcNow;
        if (now - _lastVolumeErrorUtc < TimeSpan.FromSeconds(30))
        {
            return;
        }

        _lastVolumeErrorUtc = now;
        _log.Error("Failed to control target audio session volume.", exception);
    }

    private sealed record TargetVolumeSession(ISimpleAudioVolume VolumeControl, float Volume);
}
