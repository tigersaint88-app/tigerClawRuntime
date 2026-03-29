using Microsoft.Extensions.Logging;
using TigerClaw.Infrastructure.Repositories;

namespace TigerClaw.Infrastructure.Audit;

/// <summary>
/// SQLite-backed audit logger implementation.
/// </summary>
public class AuditLogger : Core.IAuditLogger
{
    private readonly AuditLogRepository _repo;
    private readonly ILogger<AuditLogger> _logger;

    public AuditLogger(AuditLogRepository repo, ILogger<AuditLogger> logger)
    {
        _repo = repo;
        _logger = logger;
    }

    public async Task LogTaskStartAsync(string taskId, string workflowId, string userId, string inputText, CancellationToken cancellationToken = default)
    {
        await _repo.LogAsync(taskId, null, "task_start", $"Workflow: {workflowId}, User: {userId}", inputText, cancellationToken);
        _logger.LogInformation("Task started: {TaskId} workflow={WorkflowId}", taskId, workflowId);
    }

    public async Task LogStepAsync(string taskId, string stepId, string status, string? message = null, object? output = null, CancellationToken cancellationToken = default)
    {
        var payload = output != null ? System.Text.Json.JsonSerializer.Serialize(output) : null;
        await _repo.LogAsync(taskId, stepId, "step", message, payload, cancellationToken);
        _logger.LogDebug("Step {StepId} status={Status} taskId={TaskId}", stepId, status, taskId);
    }

    public async Task LogTaskCompleteAsync(string taskId, bool success, string? message = null, bool waitingHuman = false, CancellationToken cancellationToken = default)
    {
        var outcome = waitingHuman ? "waiting_human" : (success ? "success" : "failed");
        await _repo.LogAsync(taskId, null, "task_complete", message, outcome, cancellationToken);
        if (waitingHuman)
            _logger.LogInformation("Task completed: {TaskId} status=waiting_human (caller should fix preferences and re-run)", taskId);
        else
            _logger.LogInformation("Task completed: {TaskId} success={Success}", taskId, success);
    }
}
