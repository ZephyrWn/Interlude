namespace Interlude.Services;

public sealed class LoggingService
{
    private readonly object _gate = new();

    public string LogDirectory { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Interlude",
        "Logs");

    public void Info(string message) => Write("INFO", message);

    public void Warning(string message) => Write("WARN", message);

    public void Error(string message, Exception? exception = null) =>
        Write("ERROR", exception is null ? message : $"{message}{Environment.NewLine}{exception}");

    public void Debug(string message, bool enabled)
    {
        if (enabled)
        {
            Write("DEBUG", message);
        }
    }

    private void Write(string level, string message)
    {
        try
        {
            Directory.CreateDirectory(LogDirectory);
            var path = Path.Combine(LogDirectory, $"interlude-{DateTime.Now:yyyyMMdd}.log");
            var line = $"{DateTime.Now:O} [{level}] {message}{Environment.NewLine}";

            lock (_gate)
            {
                File.AppendAllText(path, line);
            }
        }
        catch
        {
            // Logging must never break audio automation.
        }
    }
}
