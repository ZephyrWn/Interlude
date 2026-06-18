using System.Runtime.InteropServices;

namespace Interlude.Services;

internal enum EDataFlow
{
    ERender = 0,
    ECapture = 1,
    EAll = 2
}

internal enum ERole
{
    EConsole = 0,
    EMultimedia = 1,
    ECommunications = 2
}

internal enum AudioSessionState
{
    Inactive = 0,
    Active = 1,
    Expired = 2
}

[Flags]
internal enum ClsCtx : uint
{
    InprocServer = 0x1,
    InprocHandler = 0x2,
    LocalServer = 0x4,
    RemoteServer = 0x10,
    All = InprocServer | InprocHandler | LocalServer | RemoteServer
}

[ComImport]
[Guid("BCDE0395-E52F-467C-8E3D-C4579291692E")]
internal sealed class MMDeviceEnumeratorComObject
{
}

[ComImport]
[Guid("A95664D2-9614-4F35-A746-DE8DB63617E6")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IMMDeviceEnumerator
{
    int EnumAudioEndpoints(EDataFlow dataFlow, uint stateMask, out IMMDeviceCollection devices);

    int GetDefaultAudioEndpoint(EDataFlow dataFlow, ERole role, out IMMDevice endpoint);

    int GetDevice(
        [MarshalAs(UnmanagedType.LPWStr)] string id,
        out IMMDevice device);

    int RegisterEndpointNotificationCallback(IntPtr client);

    int UnregisterEndpointNotificationCallback(IntPtr client);
}

[ComImport]
[Guid("0BD7A1BE-7A1A-44DB-8397-C0F9F90388D8")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IMMDeviceCollection
{
    int GetCount(out uint count);

    int Item(uint index, out IMMDevice device);
}

[ComImport]
[Guid("D666063F-1587-4E43-81F1-B948E807363F")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IMMDevice
{
    int Activate(
        ref Guid interfaceId,
        ClsCtx classContext,
        IntPtr activationParameters,
        [MarshalAs(UnmanagedType.IUnknown)] out object activatedInterface);

    int OpenPropertyStore(int access, out IntPtr properties);

    int GetId(out IntPtr id);

    int GetState(out uint state);
}

[ComImport]
[Guid("77AA99A0-1BD6-484F-8BC7-2C654C9A9B6F")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IAudioSessionManager2
{
    int GetAudioSessionControl(
        ref Guid audioSessionGuid,
        uint streamFlags,
        out IntPtr sessionControl);

    int GetSimpleAudioVolume(
        ref Guid audioSessionGuid,
        uint streamFlags,
        out IntPtr audioVolume);

    int GetSessionEnumerator(out IAudioSessionEnumerator sessionEnumerator);

    int RegisterSessionNotification(IntPtr sessionNotification);

    int UnregisterSessionNotification(IntPtr sessionNotification);

    int RegisterDuckNotification(
        [MarshalAs(UnmanagedType.LPWStr)] string sessionId,
        IntPtr duckNotification);

    int UnregisterDuckNotification(IntPtr duckNotification);
}

[ComImport]
[Guid("E2F5BB11-0570-40CA-ACDD-3AA01277DEE8")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IAudioSessionEnumerator
{
    int GetCount(out int sessionCount);

    int GetSession(int sessionIndex, out IAudioSessionControl sessionControl);
}

[ComImport]
[Guid("F4B1A599-7266-4319-A8CA-E70ACB11E8CD")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IAudioSessionControl
{
    int GetState(out AudioSessionState state);

    int GetDisplayName([MarshalAs(UnmanagedType.LPWStr)] out string displayName);

    int SetDisplayName(
        [MarshalAs(UnmanagedType.LPWStr)] string displayName,
        ref Guid eventContext);

    int GetIconPath([MarshalAs(UnmanagedType.LPWStr)] out string iconPath);

    int SetIconPath(
        [MarshalAs(UnmanagedType.LPWStr)] string iconPath,
        ref Guid eventContext);

    int GetGroupingParam(out Guid groupingId);

    int SetGroupingParam(ref Guid groupingId, ref Guid eventContext);

    int RegisterAudioSessionNotification(IntPtr newNotifications);

    int UnregisterAudioSessionNotification(IntPtr newNotifications);
}

[ComImport]
[Guid("BFB7FF88-7239-4FC9-8FA2-07C950BE9C6D")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IAudioSessionControl2
{
    int GetState(out AudioSessionState state);

    int GetDisplayName([MarshalAs(UnmanagedType.LPWStr)] out string displayName);

    int SetDisplayName(
        [MarshalAs(UnmanagedType.LPWStr)] string displayName,
        ref Guid eventContext);

    int GetIconPath([MarshalAs(UnmanagedType.LPWStr)] out string iconPath);

    int SetIconPath(
        [MarshalAs(UnmanagedType.LPWStr)] string iconPath,
        ref Guid eventContext);

    int GetGroupingParam(out Guid groupingId);

    int SetGroupingParam(ref Guid groupingId, ref Guid eventContext);

    int RegisterAudioSessionNotification(IntPtr newNotifications);

    int UnregisterAudioSessionNotification(IntPtr newNotifications);

    int GetSessionIdentifier([MarshalAs(UnmanagedType.LPWStr)] out string sessionIdentifier);

    int GetSessionInstanceIdentifier([MarshalAs(UnmanagedType.LPWStr)] out string sessionInstanceIdentifier);

    int GetProcessId(out uint processId);

    int IsSystemSoundsSession();

    int SetDuckingPreference([MarshalAs(UnmanagedType.Bool)] bool optOut);
}

[ComImport]
[Guid("C02216F6-8C67-4B5B-9D00-D008E73E0064")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IAudioMeterInformation
{
    int GetPeakValue(out float peak);

    int GetMeteringChannelCount(out int channelCount);

    int GetChannelsPeakValues(
        int channelCount,
        [Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] float[] peakValues);

    int QueryHardwareSupport(out int hardwareSupportMask);
}

[ComImport]
[Guid("87CE5498-68D6-44E5-9215-6DA47EF883D8")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface ISimpleAudioVolume
{
    int SetMasterVolume(float level, ref Guid eventContext);

    int GetMasterVolume(out float level);

    int SetMute(
        [MarshalAs(UnmanagedType.Bool)] bool isMuted,
        ref Guid eventContext);

    int GetMute([MarshalAs(UnmanagedType.Bool)] out bool isMuted);
}
