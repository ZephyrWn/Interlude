namespace Interlude.Services;

public static class MediaAudioFallbackMatcher
{
    private static readonly HashSet<string> SupportedProcessNames = new(
        StringComparer.OrdinalIgnoreCase)
    {
        "QQ.exe",
        "QQNT.exe",
        "TIM.exe",
        "WeChat.exe",
        "Weixin.exe",
        "WXWork.exe"
    };

    public static bool IsSupportedProcess(string processName)
    {
        if (string.IsNullOrWhiteSpace(processName))
        {
            return false;
        }

        var fileName = Path.GetFileName(processName.Trim());
        if (!Path.HasExtension(fileName))
        {
            fileName += ".exe";
        }

        return SupportedProcessNames.Contains(fileName);
    }
}
