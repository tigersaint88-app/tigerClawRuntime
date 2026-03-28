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
}
