using System.Runtime.InteropServices;

namespace Interlude.Services;

public sealed class SingleInstanceService : IDisposable
{
    private const string MutexName = "Interlude.SingleInstance";
    private Mutex? _mutex;

    public bool TryAcquire()
    {
        _mutex = new Mutex(initiallyOwned: true, MutexName, out var createdNew);
        if (createdNew)
        {
            return true;
        }

        ActivateExistingWindow();
        return false;
    }

    public void Dispose()
    {
        try
        {
            _mutex?.ReleaseMutex();
        }
        catch (ApplicationException)
        {
        }

        _mutex?.Dispose();
    }

    private static void ActivateExistingWindow()
    {
        var handle = FindWindow(null, "Interlude");
        if (handle != IntPtr.Zero)
        {
            ShowWindow(handle, 9);
            SetForegroundWindow(handle);
        }
    }

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern IntPtr FindWindow(string? lpClassName, string? lpWindowName);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);
}
