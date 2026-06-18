namespace Interlude.Models;

public sealed class TargetPlayerConfig
{
    public string DisplayName { get; set; } = string.Empty;

    public string SourceAppUserModelId { get; set; } = string.Empty;

    public List<string> TargetProcessNames { get; set; } = [];

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(SourceAppUserModelId);

    public void Normalize()
    {
        DisplayName = DisplayName.Trim();
        SourceAppUserModelId = SourceAppUserModelId.Trim();
        TargetProcessNames = TargetProcessNames
            .Where(static name => !string.IsNullOrWhiteSpace(name))
            .Select(static name => name.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(static name => name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
