using TigerClaw.Models;

namespace TigerClaw.Core;

/// <summary>
/// Generates executable plans from task requests.
/// </summary>
public interface ITaskPlanner
{
    /// <summary>
    /// Creates an execution plan for the given request and routing result.
    /// </summary>
    Task<ExecutionPlan> PlanAsync(TaskRequest request, RoutingResult routing, CancellationToken cancellationToken = default);
}
