using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TigerClaw.Models;

namespace TigerClaw.Skills;

/// <summary>
/// Runs a named workflow (delegates to IWorkflowEngine).
/// Uses lazy resolution to avoid circular dependency with WorkflowEngine.
/// </summary>
public class WorkflowRunNamedSkill : Core.ISkill
{
    public string Id => "workflow.run_named";
    private readonly IServiceProvider _services;
    private readonly ILogger<WorkflowRunNamedSkill> _logger;

    public WorkflowRunNamedSkill(IServiceProvider services, ILogger<WorkflowRunNamedSkill> logger)
    {
        _services = services;
        _logger = logger;
    }

    public async Task<SkillExecutionResult> ExecuteAsync(IReadOnlyDictionary<string, object?> inputs, TaskExecutionContext context, CancellationToken cancellationToken = default)
    {
        var workflowId = inputs.TryGetValue("workflowId", out var w) ? w?.ToString() : null;
        if (string.IsNullOrWhiteSpace(workflowId))
            return new SkillExecutionResult { Success = false, Message = "Missing required input: workflowId" };

        var vars = inputs.Where(x => x.Key != "workflowId").ToDictionary(x => x.Key, x => x.Value);
        var engine = _services.GetRequiredService<Core.IWorkflowEngine>();
        var result = await engine.ExecuteAsync(workflowId, context.TaskId, context.UserId, vars, cancellationToken);
        _logger.LogDebug("Sub-workflow {WorkflowId} completed taskId={TaskId} success={Success}", workflowId, context.TaskId, result.Success);
        return new SkillExecutionResult
        {
            Success = result.Success,
            Message = result.Message,
            Output = result.Steps,
            WaitingHuman = result.WaitingHuman
        };
    }
}
