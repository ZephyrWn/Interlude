using Microsoft.Win32;

namespace Interlude.Services;

public sealed class StartupService
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "Interlude";
    private readonly LoggingService _log;

    public StartupService(LoggingService log)
    {
        _log = log;
    }

    public bool IsEnabled()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: false);
        return key?.GetValue(ValueName) is string value &&
            value.Contains(GetExecutablePath(), StringComparison.OrdinalIgnoreCase);
    }

    public void SetEnabled(bool enabled)
    {
        using var key = Registry.CurrentUser.CreateSubKey(RunKeyPath, writable: true);
        if (key is null)
        {
            throw new InvalidOperationException("Unable to open the current-user Run registry key.");
        }

        if (enabled)
        {
            key.SetValue(ValueName, $"\"{GetExecutablePath()}\" --minimized", RegistryValueKind.String);
            _log.Info("Enabled start with Windows.");
        }
        else
        {
            key.DeleteValue(ValueName, throwOnMissingValue: false);
            _log.Info("Disabled start with Windows.");
        }
    }

    private static string GetExecutablePath()
    {
        return Environment.ProcessPath
            ?? System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName
            ?? "Interlude.exe";
    }
}
