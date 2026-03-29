namespace TigerClaw.Models;

/// <summary>
/// Result returned by skill execution.
/// </summary>
public record SkillExecutionResult
{
    public bool Success { get; init; }
    public string? Message { get; init; }
    public object? Output { get; init; }
    public string? ArtifactPath { get; init; }
    public bool WaitingHuman { get; init; }

    /// <summary>When <see cref="WaitingHuman"/> or a structured failure, e.g. <see cref="TigerClawErrorCodes"/>.</summary>
    public string? ErrorCode { get; init; }

    public IReadOnlyList<PrerequisiteIssue> Issues { get; init; } = Array.Empty<PrerequisiteIssue>();
}
