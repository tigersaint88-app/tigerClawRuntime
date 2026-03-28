using TigerClaw.Models;

namespace TigerClaw.Runtime;

/// <summary>
/// Creates execution contexts.
/// </summary>
public class TaskContextFactory : Core.ITaskContextFactory
{
    public TaskExecutionContext Create(string taskId, string workflowId, string userId, string firstStepId, IReadOnlyDictionary<string, object?> variables)
    {
        return new TaskExecutionContext
        {
            TaskId = taskId,
            WorkflowId = workflowId,
            CurrentStepId = firstStepId,
            Variables = variables,
            UserId = userId
        };
    }
}
