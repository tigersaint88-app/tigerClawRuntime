namespace TigerClaw.Capabilities;

/// <summary>
/// Documentation-oriented catalog of well-known capability ids. Not exhaustive.
/// </summary>
public static class CapabilityCatalog
{
    public static readonly IReadOnlyList<string> WellKnown = new[]
    {
        CapabilityIds.OsFamily("windows"),
        CapabilityIds.OsFamily("linux"),
        CapabilityIds.OsFamily("osx"),
        CapabilityIds.DesktopInteractive,
        CapabilityIds.LlmEndpointReachable,
        CapabilityIds.EmailRead
    };
}
