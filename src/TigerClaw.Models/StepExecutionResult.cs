namespace TigerClaw.Models;

/// <summary>
/// Result of executing a single workflow step.
/// </summary>
public record StepExecutionResult
{
    public required string StepId { get; init; }
    public required string Status { get; init; } // pending, running, success, failed, waiting_human, skipped, cancelled
    public string? Message { get; init; }
    public object? Output { get; init; }
    public string? ArtifactPath { get; init; }
    public DateTime? CompletedAtUtc { get; init; }
    public int? RetryCount { get; init; }

    public string? ErrorCode { get; init; }
    public IReadOnlyList<PrerequisiteIssue> Issues { get; init; } = Array.Empty<PrerequisiteIssue>();
}
