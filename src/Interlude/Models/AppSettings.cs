namespace Interlude.Models;

public sealed class AppSettings
{
    private const int CurrentSchemaVersion = 4;

    public int SchemaVersion { get; set; } = CurrentSchemaVersion;

    public string Language { get; set; } = string.Empty;

    public bool Enabled { get; set; } = true;

    public bool FirstRunCompleted { get; set; }

    public TargetPlayerConfig TargetPlayer { get; set; } = new();

    public DetectionSettings Detection { get; set; } = new();

    public List<string> ExcludedProcesses { get; set; } = ["Interlude.exe"];

    public List<IgnoredApplication> IgnoredApplications { get; set; } = [];

    public WindowPlacementSettings MainWindowPlacement { get; set; } = new();

    public bool StartWithWindows { get; set; }

    public bool RespectManualOverride { get; set; } = true;

    public bool MinimizeToTray { get; set; } = true;

    public bool CloseToTray { get; set; } = true;

    public static AppSettings CreateDefault() => new();

    public bool HasLanguageSelection => AppLanguage.IsSelected(Language);

    public bool HasTargetPlayer => FirstRunCompleted && TargetPlayer.IsConfigured;

    public void Normalize()
    {
        var loadedSchemaVersion = SchemaVersion;
        SchemaVersion = CurrentSchemaVersion;
        Language = AppLanguage.Normalize(Language, allowUnselected: true);
        TargetPlayer ??= new TargetPlayerConfig();
        Detection ??= new DetectionSettings();
        ExcludedProcesses ??= [];
        IgnoredApplications ??= [];
        MainWindowPlacement ??= new WindowPlacementSettings();

        if (loadedSchemaVersion < 2 && Detection.Mode == DetectionMode.MediaPlayback)
        {
            Detection.Mode = DetectionMode.Hybrid;
        }

        if (loadedSchemaVersion < 3 && Detection.StartConfirmMs == 100)
        {
            Detection.StartConfirmMs = 0;
        }

        if (loadedSchemaVersion < 4 &&
            Detection.Mode is DetectionMode.MediaPlayback or DetectionMode.Hybrid)
        {
            Detection.Mode = DetectionMode.AudioPeak;
        }

        TargetPlayer.Normalize();
        Detection.Normalize();
        MainWindowPlacement.Normalize();

        ExcludedProcesses = ExcludedProcesses
            .Concat(["Interlude.exe"])
            .Where(static name => !string.IsNullOrWhiteSpace(name))
            .Select(static name => name.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(static name => name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        foreach (var app in IgnoredApplications)
        {
            app.Normalize();
        }

        IgnoredApplications = IgnoredApplications
            .Where(static app =>
                !string.IsNullOrWhiteSpace(app.ProcessName) ||
                !string.IsNullOrWhiteSpace(app.ExecutablePath) ||
                !string.IsNullOrWhiteSpace(app.SourceAppUserModelId))
            .GroupBy(
                static app => string.IsNullOrWhiteSpace(app.ExecutablePath)
                    ? app.ProcessName
                    : app.ExecutablePath,
                StringComparer.OrdinalIgnoreCase)
            .Select(static group => group.First())
            .OrderBy(static app => app.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (!TargetPlayer.IsConfigured)
        {
            FirstRunCompleted = false;
        }
    }
}
