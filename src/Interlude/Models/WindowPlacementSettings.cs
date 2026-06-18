namespace Interlude.Models;

public sealed class WindowPlacementSettings
{
    public double? Left { get; set; }

    public double? Top { get; set; }

    public void Normalize()
    {
        if (Left is not null && (double.IsNaN(Left.Value) || double.IsInfinity(Left.Value)))
        {
            Left = null;
        }

        if (Top is not null && (double.IsNaN(Top.Value) || double.IsInfinity(Top.Value)))
        {
            Top = null;
        }
    }
}
