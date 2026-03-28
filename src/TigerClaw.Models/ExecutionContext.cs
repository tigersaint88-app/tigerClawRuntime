namespace TigerClaw.Models;

/// <summary>
/// Runtime context for workflow execution.
/// </summary>
public record TaskExecutionContext
{
    public required string TaskId { get; init; }
    public required string WorkflowId { get; init; }
    public required string CurrentStepId { get; init; }
    public IReadOnlyDictionary<string, object?> Variables { get; init; } = new Dictionary<string, object?>();
    public IReadOnlyDictionary<string, object?> Artifacts { get; init; } = new Dictionary<string, object?>();
    public required string UserId { get; init; }
    public DateTime StartedAtUtc { get; init; } = DateTime.UtcNow;
    public IReadOnlyDictionary<string, StepExecutionResult> StepResults { get; init; } = new Dictionary<string, StepExecutionResult>();
}
