using Microsoft.Win32;
using System.Diagnostics;

namespace ActivityMonitor.Agent.Services;

internal static class Autostart
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "ActivityMonitorAgent";

    public static void EnsureEnabled()
    {
        var exePath = Environment.ProcessPath
                      ?? Process.GetCurrentProcess().MainModule?.FileName
                      ?? throw new InvalidOperationException("Cannot resolve agent executable path.");

        var desired = $"\"{exePath}\"";

        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true)
                       ?? Registry.CurrentUser.CreateSubKey(RunKeyPath, true);

        var current = key.GetValue(ValueName) as string;
        if (!string.Equals(current, desired, StringComparison.OrdinalIgnoreCase))
            key.SetValue(ValueName, desired);
    }
}
