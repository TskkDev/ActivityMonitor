using System.Runtime.InteropServices;

namespace ActivityMonitor.Agent.Services;

internal static class UserActivityTracker
{
    [StructLayout(LayoutKind.Sequential)]
    private struct LASTINPUTINFO
    {
        public uint cbSize;
        public uint dwTime;
    }

    [DllImport("user32.dll")]
    private static extern bool GetLastInputInfo(ref LASTINPUTINFO plii);

    [DllImport("kernel32.dll")]
    private static extern uint GetTickCount();

    public static DateTimeOffset GetLastInputUtc()
    {
        var info = new LASTINPUTINFO
        {
            cbSize = (uint)Marshal.SizeOf<LASTINPUTINFO>()
        };

        if (!GetLastInputInfo(ref info))
            return DateTimeOffset.UtcNow;

        var idleMs = unchecked(GetTickCount() - info.dwTime);
        return DateTimeOffset.UtcNow - TimeSpan.FromMilliseconds(idleMs);
    }
}
