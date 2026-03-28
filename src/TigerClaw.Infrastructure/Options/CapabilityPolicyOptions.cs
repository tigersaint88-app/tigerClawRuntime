namespace TigerClaw.Infrastructure.Options;

/// <summary>
/// Policy and user blocks for capabilities. Overrides observed availability.
/// </summary>
public sealed class CapabilityPolicyOptions
{
    /// <summary>Policy-level deny list (e.g. org policy).</summary>
    public IReadOnlyList<string> BlockedCapabilities { get; set; } = Array.Empty<string>();

    /// <summary>User-level deny list.</summary>
    public IReadOnlyList<string> UserBlockedCapabilities { get; set; } = Array.Empty<string>();
}
