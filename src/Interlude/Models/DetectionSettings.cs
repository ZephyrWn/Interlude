namespace Interlude.Models;

public sealed class DetectionSettings
{
    public DetectionMode Mode { get; set; } = DetectionMode.MediaPlayback;

    public int PollIntervalMs { get; set; } = 50;

    public double StartThreshold { get; set; } = 0.005;

    public int StartConfirmMs { get; set; } = 0;

    public int SilenceConfirmMs { get; set; } = 300;

    public int ResumeDelayMs { get; set; } = 2000;

    public bool IgnoreSystemSounds { get; set; } = true;

    public void Normalize()
    {
        PollIntervalMs = Math.Clamp(PollIntervalMs, 25, 1000);
        StartThreshold = Math.Clamp(StartThreshold, 0.0001, 1.0);
        StartConfirmMs = Math.Clamp(StartConfirmMs, 0, 10000);
        SilenceConfirmMs = Math.Clamp(SilenceConfirmMs, 0, 10000);
        ResumeDelayMs = Math.Clamp(ResumeDelayMs, 0, 60000);
    }
}
