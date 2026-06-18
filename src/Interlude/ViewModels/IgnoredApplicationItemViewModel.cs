using System.Windows.Media;
using Interlude.Infrastructure;
using Interlude.Models;

namespace Interlude.ViewModels;

public sealed class IgnoredApplicationItemViewModel : ObservableObject
{
    private bool _isIgnored;

    public string DisplayName { get; init; } = string.Empty;

    public string ProcessName { get; init; } = string.Empty;

    public string ExecutablePath { get; init; } = string.Empty;

    public string SourceAppUserModelId { get; init; } = string.Empty;

    public ImageSource? Icon { get; init; }

    public bool IsIgnored
    {
        get => _isIgnored;
        set => SetProperty(ref _isIgnored, value);
    }

    public string Detail => string.IsNullOrWhiteSpace(ExecutablePath)
        ? ProcessName
        : $"{ProcessName} - {ExecutablePath}";

    public string SearchText => $"{DisplayName} {ProcessName} {ExecutablePath} {SourceAppUserModelId}";

    public IgnoredApplication ToIgnoredApplication()
    {
        return new IgnoredApplication
        {
            DisplayName = DisplayName,
            ProcessName = ProcessName,
            ExecutablePath = ExecutablePath,
            SourceAppUserModelId = SourceAppUserModelId
        };
    }
}
