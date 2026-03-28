using TigerClaw.Models;

namespace TigerClaw.Core;

/// <summary>
/// Creates execution contexts for workflow runs.
/// </summary>
public interface ITaskContextFactory
{
    TigerClaw.Models.TaskExecutionContext Create(string taskId, string workflowId, string userId, string firstStepId, IReadOnlyDictionary<string, object?> variables);
}
