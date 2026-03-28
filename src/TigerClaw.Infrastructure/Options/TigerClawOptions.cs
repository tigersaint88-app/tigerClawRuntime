namespace TigerClaw.Infrastructure.Options;

/// <summary>
/// Root configuration options for TigerClaw.
/// </summary>
public class TigerClawOptions
{
    public const string SectionName = "TigerClaw";
    public DatabaseOptions Database { get; set; } = new();
    public ModelRoutingOptions ModelRouting { get; set; } = new();
    public WorkspaceOptions Workspace { get; set; } = new();
    public CapabilityPolicyOptions CapabilityPolicy { get; set; } = new();
    /// <summary>
    /// Whether the runtime is allowed to interactively prompt the user.
    /// CLI sets this to true; API/tests default to false.
    /// </summary>
    public bool IsInteractive { get; set; } = false;
}
