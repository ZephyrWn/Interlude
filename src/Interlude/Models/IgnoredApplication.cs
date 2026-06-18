namespace Interlude.Models;

public sealed class IgnoredApplication
{
    public string DisplayName { get; set; } = string.Empty;

    public string ProcessName { get; set; } = string.Empty;

    public string ExecutablePath { get; set; } = string.Empty;

    public string SourceAppUserModelId { get; set; } = string.Empty;

    public void Normalize()
    {
        DisplayName = DisplayName.Trim();
        ProcessName = PlayerIdentityName(ProcessName);
        ExecutablePath = NormalizePath(ExecutablePath);
        SourceAppUserModelId = SourceAppUserModelId.Trim();
    }

    private static string PlayerIdentityName(string processName)
    {
        var name = processName.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            return string.Empty;
        }

        return Path.HasExtension(name) ? name : $"{name}.exe";
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
