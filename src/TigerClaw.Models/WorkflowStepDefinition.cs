namespace TigerClaw.Models;

/// <summary>
/// Defines a single step in a workflow.
/// </summary>
public record WorkflowStepDefinition
{
    public required string Id { get; init; }
    public string? Name { get; init; }
    public string Type { get; init; } = "skill";
    public required string SkillId { get; init; }
    public IReadOnlyDictionary<string, object?> Inputs { get; init; } = new Dictionary<string, object?>();
    public string? NextStepId { get; init; }
    public string? OnFailureStepId { get; init; }
    public RetryPolicy? RetryPolicy { get; init; }
    public string? HumanInstruction { get; init; }

    /// <summary>Optional capability requirements for this node (merged with skill-level; see merge rules).</summary>
    public CapabilityRequirements? CapabilityRequirements { get; init; }
}

/// <summary>
/// Defines retry behavior for a step.
/// </summary>
public record RetryPolicy
{
    public int MaxRetries { get; init; }
    public int DelayMs { get; init; }
    public string? Backoff { get; init; }
}
