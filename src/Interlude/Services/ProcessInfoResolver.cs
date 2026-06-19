using System.Diagnostics;
using System.Collections.Concurrent;

namespace Interlude.Services;

internal sealed record ProcessIdentity(
    int ProcessId,
    string ProcessName,
    string DisplayName,
    string ExecutablePath);

internal static class ProcessInfoResolver
{
    private static readonly ConcurrentDictionary<int, ProcessIdentity> Cache = new();

    public static ProcessIdentity Resolve(int processId)
    {
        if (processId <= 0)
        {
            return new ProcessIdentity(0, "System Sounds", "System Sounds", string.Empty);
        }

        if (Cache.TryGetValue(processId, out var cached))
        {
            return cached;
        }

        try
        {
            using var process = Process.GetProcessById(processId);
            var processName = PlayerIdentityResolver.NormalizeProcessName(process.ProcessName);
            var executablePath = TryGetExecutablePath(process);
            var displayName = ResolveDisplayName(process, executablePath, processName);
            var identity = new ProcessIdentity(processId, processName, displayName, executablePath);
            Cache[processId] = identity;
            return identity;
        }
        catch
        {
            var identity = new ProcessIdentity(processId, $"PID {processId}", $"PID {processId}", string.Empty);
            Cache[processId] = identity;
            return identity;
        }
    }

    private static string TryGetExecutablePath(Process process)
    {
        try
        {
            return process.MainModule?.FileName ?? string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    private static string ResolveDisplayName(Process process, string executablePath, string processName)
    {
        if (!string.IsNullOrWhiteSpace(process.MainWindowTitle))
        {
            return process.MainWindowTitle.Trim();
        }

        if (!string.IsNullOrWhiteSpace(executablePath))
        {
            try
            {
                var versionInfo = FileVersionInfo.GetVersionInfo(executablePath);
                if (!string.IsNullOrWhiteSpace(versionInfo.FileDescription))
                {
                    return versionInfo.FileDescription.Trim();
                }
            }
            catch
            {
                // File version metadata is optional.
            }
        }

        return Path.GetFileNameWithoutExtension(processName);
    }
}
