namespace TigerClaw.Models;

/// <summary>
/// Executable plan output from TaskPlanner.
/// </summary>
public record ExecutionPlan
{
    public required string PlanType { get; init; } // "workflow" or "dynamic"
    public required string WorkflowId { get; init; }
    public required IReadOnlyList<PlanStep> Steps { get; init; }
}

/// <summary>
/// A single step in an execution plan.
/// </summary>
public record PlanStep
{
    public required string Id { get; init; }
    public required string Skill { get; init; }
    public IReadOnlyDictionary<string, object?> Inputs { get; init; } = new Dictionary<string, object?>();
}
