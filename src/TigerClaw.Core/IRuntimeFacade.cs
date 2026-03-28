using TigerClaw.Models;

namespace TigerClaw.Core;

/// <summary>
/// Unified entry point for CLI and API.
/// </summary>
public interface IRuntimeFacade
{
    Task<TaskResponse> RunTaskAsync(TaskRequest request, CancellationToken cancellationToken = default);
    Task<TaskResponse> RunWorkflowAsync(string workflowId, IReadOnlyDictionary<string, object?> inputs, string? userId = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SkillDefinition>> ListSkillsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<WorkflowDefinition>> ListWorkflowsAsync(CancellationToken cancellationToken = default);
}
