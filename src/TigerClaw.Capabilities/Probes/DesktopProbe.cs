namespace TigerClaw.Capabilities.Probes;

/// <summary>
/// Heuristic: interactive desktop session suitable for GUI / Playwright when stdin is a TTY and user is interactive.
/// </summary>
public static class DesktopProbe
{
    public static bool IsLikelyDesktopInteractive()
    {
        try
        {
            if (!Environment.UserInteractive) return false;
            if (Console.IsInputRedirected && Console.IsOutputRedirected) return false;
            return true;
        }
        catch
        {
            return false;
        }
    }
}
