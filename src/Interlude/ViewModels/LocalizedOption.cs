namespace Interlude.ViewModels;

public sealed record LocalizedOption<T>(T Value, string DisplayName)
{
    public override string ToString() => DisplayName;
}
