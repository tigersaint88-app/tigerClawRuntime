using TigerClaw.Models;

namespace TigerClaw.Core;

/// <summary>
/// Loads workflow definitions from storage.
/// </summary>
public interface IWorkflowLoader
{
    Task<WorkflowDefinition?> LoadAsync(string workflowId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<WorkflowDefinition>> LoadAllAsync(CancellationToken cancellationToken = default);
}
