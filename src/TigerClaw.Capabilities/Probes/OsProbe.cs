using System.Runtime.InteropServices;

namespace TigerClaw.Capabilities.Probes;

public static class OsProbe
{
    public static string DetectOsFamily()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return "windows";
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) return "osx";
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) return "linux";
        return "unknown";
    }
}
