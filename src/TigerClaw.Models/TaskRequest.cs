namespace TigerClaw.Models;

/// <summary>
/// Represents an incoming task request from any entry point (CLI, API, etc.).
/// </summary>
public record TaskRequest
{
    public required string RequestId { get; init; }
    public required string SessionId { get; init; }
    public required string UserId { get; init; }
    public required string InputText { get; init; }
    public string Channel { get; init; } = "cli";
    public IReadOnlyList<string> Attachments { get; init; } = Array.Empty<string>();
    public IReadOnlyDictionary<string, string?> Metadata { get; init; } = new Dictionary<string, string?>();
    public DateTime CreatedAtUtc { get; init; } = DateTime.UtcNow;
}
