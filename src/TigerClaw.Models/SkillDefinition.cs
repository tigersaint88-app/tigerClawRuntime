namespace TigerClaw.Models;

/// <summary>
/// Metadata for a skill registered in the skill registry.
/// </summary>
public record SkillDefinition
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public IReadOnlyList<string> Tags { get; init; } = Array.Empty<string>();
    public string RiskLevel { get; init; } = "low";
    public string? InputSchemaJson { get; init; }
    public string? OutputSchemaJson { get; init; }
    public string ExecutionMode { get; init; } = "local";

    // OpenClaw-style fields (optional). We only use them to help derive prerequisites.
    public IReadOnlyList<string>? Bins { get; init; }
    public IReadOnlyList<string>? AnyBins { get; init; }
    public IReadOnlyList<string>? Env { get; init; }
    public IReadOnlyList<string>? Config { get; init; }

    // TigerClaw prerequisites.
    public SkillPrerequisites? Prerequisites { get; init; }
}

public record SkillPrerequisites
{
    public IReadOnlyList<SkillRequiredResource> RequiredResources { get; init; } = Array.Empty<SkillRequiredResource>();
    public IReadOnlyList<string> RequiredConfig { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> RequiredAuth { get; init; } = Array.Empty<string>();
    /// <summary>Legacy: treated as additional <see cref="CapabilityRequirements.AllOf"/> entries when merging.</summary>
    public IReadOnlyList<string> RequiredCapabilities { get; init; } = Array.Empty<string>();
    public CapabilityRequirements? Capabilities { get; init; }
}

public record SkillRequiredResource
{
    public required string Type { get; init; }
    public required string Key { get; init; }
}
