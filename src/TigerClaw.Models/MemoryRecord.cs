namespace TigerClaw.Models;

/// <summary>
/// Base record for memory entries.
/// </summary>
public record MemoryRecord
{
    public required string Id { get; init; }
    public required string Key { get; init; }
    public required string Value { get; init; }
    public string? Type { get; init; }
    public DateTime CreatedAtUtc { get; init; }
    public DateTime UpdatedAtUtc { get; init; }
}
