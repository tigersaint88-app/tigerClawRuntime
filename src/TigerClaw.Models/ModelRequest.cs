namespace TigerClaw.Models;

/// <summary>
/// Request for LLM completion.
/// </summary>
public record ModelRequest
{
    public required string TaskType { get; init; }
    public required string Prompt { get; init; }
    public IReadOnlyList<ChatMessage>? Messages { get; init; }
    public int MaxTokens { get; init; } = 1024;
    public double Temperature { get; init; } = 0.3;
}

/// <summary>
/// Chat message for conversation context.
/// </summary>
public record ChatMessage
{
    public required string Role { get; init; }
    public required string Content { get; init; }
}
