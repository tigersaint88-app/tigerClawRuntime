using TigerClaw.Models;

namespace TigerClaw.Core;

/// <summary>
/// Routes incoming requests to appropriate handlers based on intent.
/// </summary>
public interface IIntentRouter
{
    /// <summary>
    /// Determines the intent and routing decision for a task request.
    /// </summary>
    Task<RoutingResult> RouteAsync(TaskRequest request, CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of intent routing.
/// </summary>
public record RoutingResult
{
    public required string Intent { get; init; }
    public string? WorkflowId { get; init; }
    public IReadOnlyDictionary<string, object?> Parameters { get; init; } = new Dictionary<string, object?>();
}
