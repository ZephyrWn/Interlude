using Interlude.Models;

namespace Interlude.Services;

public sealed class IgnoredApplicationMatcher
{
    private static readonly StringComparer Comparer = StringComparer.OrdinalIgnoreCase;

    public bool IsIgnoredAudioSession(AudioSessionInfo session, AppSettings settings)
    {
        return IsIgnored(
            session.ProcessName,
            session.ExecutablePath,
            sourceAppUserModelId: string.Empty,
            settings);
    }

    public bool IsIgnoredMediaSession(MediaSessionInfo session, AppSettings settings)
    {
        if (IsIgnored(
                processName: string.Empty,
                executablePath: string.Empty,
                session.SourceAppUserModelId,
                settings))
        {
            return true;
        }

        var candidateNames = PlayerIdentityResolver.GetCandidateProcessNames(session.SourceAppUserModelId);
        return candidateNames.Any(name => IsIgnored(name, string.Empty, string.Empty, settings));
    }

    public bool IsTargetPlayerAudioSession(AudioSessionInfo session, AppSettings settings)
    {
        var targetNames = BuildTargetProcessNames(settings);
        return targetNames.Contains(PlayerIdentityResolver.NormalizeProcessName(session.ProcessName));
    }

    public bool IsIgnored(
        string processName,
        string executablePath,
        string sourceAppUserModelId,
        AppSettings settings)
    {
        var normalizedProcessName = NormalizeProcessNameOrEmpty(processName);
        var normalizedPath = NormalizePath(executablePath);
        var normalizedAumid = sourceAppUserModelId.Trim();

        return settings.IgnoredApplications.Any(app =>
            (!string.IsNullOrWhiteSpace(app.ExecutablePath) &&
             !string.IsNullOrWhiteSpace(normalizedPath) &&
             Comparer.Equals(NormalizePath(app.ExecutablePath), normalizedPath)) ||
            (!string.IsNullOrWhiteSpace(app.ProcessName) &&
             !string.IsNullOrWhiteSpace(normalizedProcessName) &&
             Comparer.Equals(PlayerIdentityResolver.NormalizeProcessName(app.ProcessName), normalizedProcessName)) ||
            (!string.IsNullOrWhiteSpace(app.SourceAppUserModelId) &&
             !string.IsNullOrWhiteSpace(normalizedAumid) &&
             Comparer.Equals(app.SourceAppUserModelId.Trim(), normalizedAumid)));
    }

    public static ISet<string> BuildTargetProcessNames(AppSettings settings)
    {
        return settings.TargetPlayer.TargetProcessNames
            .Select(PlayerIdentityResolver.NormalizeProcessName)
            .Concat(PlayerIdentityResolver.GetCandidateProcessNames(settings.TargetPlayer.SourceAppUserModelId))
            .Where(static name => !string.IsNullOrWhiteSpace(name))
            .ToHashSet(Comparer);
    }

    private static string NormalizeProcessNameOrEmpty(string processName)
    {
        return string.IsNullOrWhiteSpace(processName)
            ? string.Empty
            : PlayerIdentityResolver.NormalizeProcessName(processName);
    }

    private static string NormalizePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return string.Empty;
        }

        try
        {
            return Path.GetFullPath(path.Trim());
        }
        catch
        {
            return path.Trim();
        }
    }
}
