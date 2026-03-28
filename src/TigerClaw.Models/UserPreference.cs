namespace TigerClaw.Models;

/// <summary>
/// User preference (profile memory).
/// </summary>
public record UserPreference
{
    public required string Key { get; init; }
    public required string Value { get; init; }
    public string? UserId { get; init; }
    public DateTime UpdatedAtUtc { get; init; }
}
