namespace TigerClaw.Capabilities;

/// <summary>
/// Observed runtime resource truth used for capability resolution (immutable snapshot).
/// </summary>
public sealed record ResourceSnapshot(
    string OsFamily,
    string OsCapabilityId,
    bool DesktopInteractive,
    IReadOnlySet<string> BinaryCapabilityIds,
    IReadOnlySet<string> AnyBinCapabilityIds,
    bool LlmEndpointReachable,
    string? LlmProbeDetail,
    IReadOnlyDictionary<string, string> ProbeDiagnostics);
