using System.Diagnostics;

namespace Interlude.Services;

internal sealed record ProcessIdentity(
    int ProcessId,
    string ProcessName,
    string DisplayName,
    string ExecutablePath);

internal static class ProcessInfoResolver
{
    public static ProcessIdentity Resolve(int processId)
    {
        if (processId <= 0)
        {
            return new ProcessIdentity(0, "System Sounds", "System Sounds", string.Empty);
        }

        try
        {
            using var process = Process.GetProcessById(processId);
            var processName = PlayerIdentityResolver.NormalizeProcessName(process.ProcessName);
            var executablePath = TryGetExecutablePath(process);
            var displayName = ResolveDisplayName(process, executablePath, processName);
            return new ProcessIdentity(processId, processName, displayName, executablePath);
        }
        catch
        {
            return new ProcessIdentity(processId, $"PID {processId}", $"PID {processId}", string.Empty);
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
