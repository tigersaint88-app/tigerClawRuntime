namespace TigerClaw.Core;

/// <summary>
/// Audit logging for tasks and steps.
/// </summary>
public interface IAuditLogger
{
    Task LogTaskStartAsync(string taskId, string workflowId, string userId, string inputText, CancellationToken cancellationToken = default);
    Task LogStepAsync(string taskId, string stepId, string status, string? message = null, object? output = null, CancellationToken cancellationToken = default);
    Task LogTaskCompleteAsync(string taskId, bool success, string? message = null, bool waitingHuman = false, CancellationToken cancellationToken = default);
}
