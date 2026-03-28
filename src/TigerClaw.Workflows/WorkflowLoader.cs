using TigerClaw.Infrastructure.Loaders;
using TigerClaw.Models;

namespace TigerClaw.Workflows;

/// <summary>
/// Loads workflow definitions using Infrastructure loader.
/// </summary>
public class WorkflowLoader : Core.IWorkflowLoader
{
    private readonly WorkflowDefinitionLoader _loader;

    public WorkflowLoader(WorkflowDefinitionLoader loader)
    {
        _loader = loader;
    }

    public Task<WorkflowDefinition?> LoadAsync(string workflowId, CancellationToken cancellationToken = default)
        => _loader.LoadAsync(workflowId, cancellationToken);

    public Task<IReadOnlyList<WorkflowDefinition>> LoadAllAsync(CancellationToken cancellationToken = default)
        => _loader.LoadAllAsync(cancellationToken);
}
