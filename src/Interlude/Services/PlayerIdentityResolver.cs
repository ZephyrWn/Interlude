using System.Text;

namespace Interlude.Services;

internal static class PlayerIdentityResolver
{
    private static readonly (string Hint, string ProcessName)[] ProcessHints =
    [
        ("cloudmusic", "cloudmusic.exe"),
        ("netease", "cloudmusic.exe"),
        ("douyin", "douyin.exe"),
        ("bytedance", "douyin.exe"),
        ("chrome", "chrome.exe"),
        ("msedge", "msedge.exe"),
        ("microsoftedge", "msedge.exe"),
        ("spotify", "spotify.exe"),
        ("qqmusic", "QQMusic.exe"),
        ("zunemusic", "Music.UI.exe"),
        ("music.ui", "Music.UI.exe"),
        ("vlc", "vlc.exe"),
        ("foobar", "foobar2000.exe")
    ];

    private static readonly IReadOnlyDictionary<string, string> FriendlyNames =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["cloudmusic.exe"] = "\u7F51\u6613\u4E91\u97F3\u4E50",
            ["douyin.exe"] = "\u6296\u97F3",
            ["chrome.exe"] = "Chrome",
            ["msedge.exe"] = "Edge",
            ["spotify.exe"] = "Spotify",
            ["QQMusic.exe"] = "QQ \u97F3\u4E50",
            ["Music.UI.exe"] = "Media Player",
            ["vlc.exe"] = "VLC",
            ["foobar2000.exe"] = "foobar2000"
        };

    public static string ResolveDisplayName(string sourceAppUserModelId)
    {
        foreach (var processName in GetCandidateProcessNames(sourceAppUserModelId))
        {
            if (FriendlyNames.TryGetValue(processName, out var friendlyName))
            {
                return friendlyName;
            }
        }

        var token = ExtractPrimaryToken(sourceAppUserModelId);
        if (string.IsNullOrWhiteSpace(token))
        {
            return sourceAppUserModelId.Trim();
        }

        var normalized = NormalizeProcessName(token);
        return FriendlyNames.TryGetValue(normalized, out var mapped)
            ? mapped
            : HumanizeToken(token);
    }

    public static IReadOnlyList<string> GetCandidateProcessNames(string sourceAppUserModelId)
    {
        if (string.IsNullOrWhiteSpace(sourceAppUserModelId))
        {
            return [];
        }

        var candidates = new List<string>();
        foreach (var token in SplitTokens(sourceAppUserModelId))
        {
            var fileName = Path.GetFileName(token);
            if (fileName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
            {
                candidates.Add(NormalizeProcessName(fileName));
            }
        }

        foreach (var (hint, processName) in ProcessHints)
        {
            if (sourceAppUserModelId.Contains(hint, StringComparison.OrdinalIgnoreCase))
            {
                candidates.Add(NormalizeProcessName(processName));
            }
        }

        var primaryToken = ExtractPrimaryToken(sourceAppUserModelId);
        if (IsLikelyProcessToken(primaryToken))
        {
            candidates.Add(NormalizeProcessName(primaryToken));
        }

        return candidates
            .Where(static name => !string.IsNullOrWhiteSpace(name))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public static string NormalizeProcessName(string processName)
    {
        var name = processName.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            return "Unknown";
        }

        if (name.Equals("System Sounds", StringComparison.OrdinalIgnoreCase) ||
            name.StartsWith("PID ", StringComparison.OrdinalIgnoreCase))
        {
            return name;
        }

        return Path.HasExtension(name) ? name : $"{name}.exe";
    }

    private static string ExtractPrimaryToken(string sourceAppUserModelId)
    {
        var tokens = SplitTokens(sourceAppUserModelId).ToArray();
        var exe = tokens.FirstOrDefault(static token =>
            token.EndsWith(".exe", StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(exe))
        {
            return Path.GetFileName(exe);
        }

        var token = tokens.LastOrDefault();
        if (string.IsNullOrWhiteSpace(token))
        {
            return string.Empty;
        }

        var packageSeparator = token.IndexOf('_');
        if (packageSeparator > 0)
        {
            token = token[..packageSeparator];
        }

        var parts = token.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return parts.Length == 0 ? token : parts[^1];
    }

    private static IEnumerable<string> SplitTokens(string sourceAppUserModelId)
    {
        return sourceAppUserModelId.Split(
            ['!', '\\', '/', '|', ':'],
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    private static bool IsLikelyProcessToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token) ||
            token.Length > 64 ||
            token.Contains(' '))
        {
            return false;
        }

        return token.All(static c =>
            char.IsLetterOrDigit(c) ||
            c is '_' or '-' or '.');
    }

    private static string HumanizeToken(string token)
    {
        var name = Path.GetFileNameWithoutExtension(token).Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            return token.Trim();
        }

        var builder = new StringBuilder();
        var previousWasLowerOrDigit = false;
        foreach (var c in name)
        {
            if (c is '_' or '-' or '.')
            {
                AppendSpace(builder);
                previousWasLowerOrDigit = false;
                continue;
            }

            if (char.IsUpper(c) && previousWasLowerOrDigit)
            {
                AppendSpace(builder);
            }

            builder.Append(c);
            previousWasLowerOrDigit = char.IsLower(c) || char.IsDigit(c);
        }

        return builder.ToString().Trim();
    }

    private static void AppendSpace(StringBuilder builder)
    {
        if (builder.Length > 0 && builder[^1] != ' ')
        {
            builder.Append(' ');
        }
    }
}
