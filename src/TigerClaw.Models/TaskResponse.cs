namespace TigerClaw.Models;

/// <summary>
/// Represents the result of a task execution.
/// </summary>
public record TaskResponse
{
    public required string RequestId { get; init; }
    public bool Success { get; init; }
    public string? Message { get; init; }
    public string? FinalText { get; init; }
    public IReadOnlyList<string> Artifacts { get; init; } = Array.Empty<string>();
    public string? WorkflowId { get; init; }
    public IReadOnlyList<StepExecutionResult> Steps { get; init; } = Array.Empty<StepExecutionResult>();
    public string? ErrorCode { get; init; }
}
