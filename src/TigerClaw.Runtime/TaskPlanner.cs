using Microsoft.Extensions.Logging;
using TigerClaw.Models;

namespace TigerClaw.Runtime;

/// <summary>
/// Creates execution plans. Template-first strategy.
/// </summary>
public class TaskPlanner : Core.ITaskPlanner
{
    private readonly Core.IWorkflowLoader _loader;
    private readonly Core.IWorkflowTemplateResolver _resolver;
    private readonly ILogger<TaskPlanner> _logger;

    public TaskPlanner(Core.IWorkflowLoader loader, Core.IWorkflowTemplateResolver resolver, ILogger<TaskPlanner> logger)
    {
        _loader = loader;
        _resolver = resolver;
        _logger = logger;
    }

    public async Task<ExecutionPlan> PlanAsync(TaskRequest request, Core.RoutingResult routing, CancellationToken cancellationToken = default)
    {
        var workflowId = routing.WorkflowId;
        if (string.IsNullOrEmpty(workflowId))
        {
            var resolved = await _resolver.ResolveWorkflowIdAsync(routing.Intent, routing.Parameters, cancellationToken);
            if (string.IsNullOrWhiteSpace(resolved))
                return new ExecutionPlan { PlanType = "unsupported", WorkflowId = "unsupported", Steps = new List<PlanStep>() };
            workflowId = resolved;
        }

        var def = await _loader.LoadAsync(workflowId, cancellationToken);
        if (def != null)
        {
            var steps = def.Steps.Select(s => new PlanStep
            {
                Id = s.Id,
                Skill = s.SkillId,
                Inputs = s.Inputs ?? new Dictionary<string, object?>()
            }).ToList();
            _logger.LogDebug("Planned workflow {WorkflowId} with {Count} steps", workflowId, steps.Count);
            return new ExecutionPlan { PlanType = "workflow", WorkflowId = workflowId, Steps = steps };
        }

        _logger.LogWarning("Workflow {WorkflowId} not found, returning unsupported plan", workflowId);
        return new ExecutionPlan { PlanType = "unsupported", WorkflowId = workflowId, Steps = new List<PlanStep>() };
    }
}
