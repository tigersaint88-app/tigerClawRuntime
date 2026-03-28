namespace TigerClaw.Models;

/// <summary>
/// Procedure memory - successful task execution path.
/// </summary>
public record ProcedureRecord
{
    public required string TaskType { get; init; }
    public required IReadOnlyList<string> StepsSummary { get; init; }
    public string? UserId { get; init; }
    public DateTime CreatedAtUtc { get; init; }
}
