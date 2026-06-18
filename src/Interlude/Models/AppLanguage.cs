namespace Interlude.Models;

public static class AppLanguage
{
    public const string English = "en";
    public const string ChineseSimplified = "zh-CN";

    public static bool IsSelected(string? languageCode)
    {
        var normalized = Normalize(languageCode, allowUnselected: true);
        return normalized is English or ChineseSimplified;
    }

    public static string Normalize(string? languageCode, bool allowUnselected = false)
    {
        if (string.IsNullOrWhiteSpace(languageCode))
        {
            return allowUnselected ? string.Empty : English;
        }

        var trimmed = languageCode.Trim();
        if (trimmed.Equals(English, StringComparison.OrdinalIgnoreCase) ||
            trimmed.Equals("en-US", StringComparison.OrdinalIgnoreCase))
        {
            return English;
        }

        if (trimmed.Equals(ChineseSimplified, StringComparison.OrdinalIgnoreCase) ||
            trimmed.Equals("zh", StringComparison.OrdinalIgnoreCase) ||
            trimmed.StartsWith("zh-", StringComparison.OrdinalIgnoreCase))
        {
            return ChineseSimplified;
        }

        return allowUnselected ? string.Empty : English;
    }
}
