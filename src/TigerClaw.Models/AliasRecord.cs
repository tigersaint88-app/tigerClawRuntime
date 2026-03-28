namespace TigerClaw.Models;

/// <summary>
/// Alias mapping (e.g., "老板邮箱" -> "boss@company.com").
/// </summary>
public record AliasRecord
{
    public required string Alias { get; init; }
    public required string ResolvedValue { get; init; }
    public string? UserId { get; init; }
    public DateTime UpdatedAtUtc { get; init; }
}
