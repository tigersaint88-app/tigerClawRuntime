namespace TigerClaw.Models;

/// <summary>
/// Response from LLM completion.
/// </summary>
public record ModelResponse
{
    public required string Content { get; init; }
    public int PromptTokens { get; init; }
    public int CompletionTokens { get; init; }
    public string? Model { get; init; }
}
