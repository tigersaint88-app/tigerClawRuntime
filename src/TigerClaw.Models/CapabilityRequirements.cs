namespace TigerClaw.Models;

/// <summary>
/// Capability prerequisite expression (allOf / anyOf / noneOf / prefer).
/// Merge rules when combining skill + workflow step are implemented in <c>TigerClaw.Capabilities.SkillPrerequisitesMerge</c>.
/// </summary>
public sealed record CapabilityRequirements
{
    public IReadOnlyList<string> AllOf { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> AnyOf { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> NoneOf { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> Prefer { get; init; } = Array.Empty<string>();
}
