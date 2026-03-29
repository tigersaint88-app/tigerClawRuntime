using TigerClaw.Models;

namespace TigerClaw.Core;

/// <summary>
/// Executes workflows step by step.
/// </summary>
public interface IWorkflowEngine
{
    /// <summary>
    /// Executes a workflow by ID with given inputs.
    /// </summary>
    Task<WorkflowExecutionResult> ExecuteAsync(
        string workflowId,
        string taskId,
        string userId,
        IReadOnlyDictionary<string, object?> inputs,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of workflow execution.
/// </summary>
public record WorkflowExecutionResult
{
    public bool Success { get; init; }
    public string? Message { get; init; }
    public IReadOnlyList<StepExecutionResult> Steps { get; init; } = Array.Empty<StepExecutionResult>();
    public IReadOnlyList<string> Artifacts { get; init; } = Array.Empty<string>();
    public bool WaitingHuman { get; init; }
    public string? ErrorCode { get; init; }
    public IReadOnlyList<PrerequisiteIssue> Issues { get; init; } = Array.Empty<PrerequisiteIssue>();
    public string? InteractionMessage { get; init; }
}
