using System.Text.Json;
using System.Text.Json.Serialization;
using Interlude.Models;

namespace Interlude.Services;

public sealed class SettingsService
{
    private readonly LoggingService _log;
    private readonly JsonSerializerOptions _jsonOptions;

    public SettingsService(LoggingService log)
    {
        _log = log;
        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        };
        _jsonOptions.Converters.Add(new JsonStringEnumConverter());
    }

    public event EventHandler? SettingsChanged;

    public string ConfigDirectory { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "Interlude");

    public string SettingsPath => Path.Combine(ConfigDirectory, "settings.json");

    public AppSettings Current { get; private set; } = AppSettings.CreateDefault();

    public AppSettings Load()
    {
        Directory.CreateDirectory(ConfigDirectory);

        if (!File.Exists(SettingsPath))
        {
            Current = AppSettings.CreateDefault();
            Current.Normalize();
            Save();
            _log.Info("Created default configuration.");
            return Current;
        }

        try
        {
            var json = File.ReadAllText(SettingsPath);
            Current = JsonSerializer.Deserialize<AppSettings>(json, _jsonOptions)
                ?? AppSettings.CreateDefault();
            Current.Normalize();
            _log.Info("Loaded configuration.");
        }
        catch (Exception ex)
        {
            BackupCorruptedSettings();
            Current = AppSettings.CreateDefault();
            Current.Normalize();
            Save();
            _log.Error("Configuration was corrupted; default configuration was created.", ex);
        }

        SettingsChanged?.Invoke(this, EventArgs.Empty);
        return Current;
    }

    public void Save()
    {
        Directory.CreateDirectory(ConfigDirectory);
        Current.Normalize();
        var json = JsonSerializer.Serialize(Current, _jsonOptions);
        File.WriteAllText(SettingsPath, json);
        SettingsChanged?.Invoke(this, EventArgs.Empty);
    }

    public void Replace(AppSettings settings)
    {
        Current = settings;
        Save();
    }

    public void Reset()
    {
        Current = AppSettings.CreateDefault();
        Save();
        _log.Info("Settings reset.");
    }

    private void BackupCorruptedSettings()
    {
        try
        {
            if (!File.Exists(SettingsPath))
            {
                return;
            }

            var backupName = $"settings.corrupted-{DateTime.Now:yyyyMMdd-HHmmss}.json";
            File.Copy(SettingsPath, Path.Combine(ConfigDirectory, backupName), overwrite: true);
        }
        catch (Exception ex)
        {
            _log.Error("Failed to back up corrupted settings.", ex);
        }
    }
}
