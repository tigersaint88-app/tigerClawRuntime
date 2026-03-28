using Microsoft.Extensions.Logging;
using TigerClaw.Models;

namespace TigerClaw.Runtime;

/// <summary>
/// Unified entry point for CLI and API.
/// </summary>
public class RuntimeFacade : Core.IRuntimeFacade
{
    private readonly Core.IIntentRouter _router;
    private readonly Core.ITaskPlanner _planner;
    private readonly Core.IWorkflowEngine _engine;
    private readonly Core.IWorkflowLoader _workflowLoader;
    private readonly Core.ISkillRegistry _skillRegistry;
    private readonly Core.IMemoryStore? _memoryStore;
    private readonly ILogger<RuntimeFacade> _logger;

    public RuntimeFacade(
        Core.IIntentRouter router,
        Core.ITaskPlanner planner,
        Core.IWorkflowEngine engine,
        Core.IWorkflowLoader workflowLoader,
        Core.ISkillRegistry skillRegistry,
        ILogger<RuntimeFacade> logger,
        Core.IMemoryStore? memoryStore = null)
    {
        _router = router;
        _planner = planner;
        _engine = engine;
        _workflowLoader = workflowLoader;
        _skillRegistry = skillRegistry;
        _memoryStore = memoryStore;
        _logger = logger;
    }

    public async Task<TaskResponse> RunTaskAsync(TaskRequest request, CancellationToken cancellationToken = default)
    {
        var taskId = request.RequestId;
        try
        {
            var routing = await _router.RouteAsync(request, cancellationToken);
            var plan = await _planner.PlanAsync(request, routing, cancellationToken);

            if (plan.Steps.Count == 0 && (routing.Intent == "list_skills" || routing.Intent == "list_workflows" || routing.Intent == "list_aliases"))
            {
                return await HandleListIntentAsync(routing, request, cancellationToken);
            }

            if (string.Equals(plan.PlanType, "unsupported", StringComparison.OrdinalIgnoreCase))
            {
                return new TaskResponse
                {
                    RequestId = request.RequestId,
                    Success = false,
                    Message = $"未实现：{routing.Intent}",
                    ErrorCode = "NOT_IMPLEMENTED"
                };
            }

            var vars = new Dictionary<string, object?> { ["input"] = request.InputText };
            foreach (var kv in routing.Parameters)
                vars[kv.Key] = kv.Value;
            var result = await _engine.ExecuteAsync(plan.WorkflowId, taskId, request.UserId, vars, cancellationToken);

            return new TaskResponse
            {
                RequestId = request.RequestId,
                Success = result.Success,
                Message = result.Message,
                FinalText = result.Success ? "Task completed." : result.Message,
                Artifacts = result.Artifacts.ToList(),
                WorkflowId = plan.WorkflowId,
                Steps = result.Steps.ToList()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Task failed: {TaskId}", taskId);
            return new TaskResponse
            {
                RequestId = request.RequestId,
                Success = false,
                Message = ex.Message,
                ErrorCode = "INTERNAL_ERROR"
            };
        }
    }

    private async Task<TaskResponse> HandleListIntentAsync(Core.RoutingResult routing, TaskRequest request, CancellationToken cancellationToken)
    {
        if (routing.Intent == "list_skills")
        {
            var skills = _skillRegistry.ListAll();
            return new TaskResponse { RequestId = request.RequestId, Success = true, FinalText = string.Join("\n", skills.Select(s => $"- {s.Id}: {s.Name}")) };
        }
        if (routing.Intent == "list_workflows")
        {
            var workflows = await _workflowLoader.LoadAllAsync(cancellationToken);
            return new TaskResponse { RequestId = request.RequestId, Success = true, FinalText = string.Join("\n", workflows.Select(w => $"- {w.Id}: {w.Name}")) };
        }
        if (routing.Intent == "list_aliases" && _memoryStore != null)
        {
            var aliases = await _memoryStore.Aliases.ListAllAsync(request.UserId, cancellationToken);
            return new TaskResponse { RequestId = request.RequestId, Success = true, FinalText = string.Join("\n", aliases.Select(a => $"- {a.Alias} -> {a.ResolvedValue}")) };
        }
        return new TaskResponse { RequestId = request.RequestId, Success = true, FinalText = "Use memory service for aliases." };
    }

    public async Task<TaskResponse> RunWorkflowAsync(string workflowId, IReadOnlyDictionary<string, object?> inputs, string? userId = null, CancellationToken cancellationToken = default)
    {
        var taskId = Guid.NewGuid().ToString("N");
        var uid = userId ?? "local-user";
        var result = await _engine.ExecuteAsync(workflowId, taskId, uid, inputs ?? new Dictionary<string, object?>(), cancellationToken);
        return new TaskResponse
        {
            RequestId = taskId,
            Success = result.Success,
            Message = result.Message,
            FinalText = result.Success ? "Workflow completed." : result.Message,
            Artifacts = result.Artifacts.ToList(),
            WorkflowId = workflowId,
            Steps = result.Steps.ToList()
        };
    }

    public Task<IReadOnlyList<SkillDefinition>> ListSkillsAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(_skillRegistry.ListAll());

    public Task<IReadOnlyList<WorkflowDefinition>> ListWorkflowsAsync(CancellationToken cancellationToken = default)
        => _workflowLoader.LoadAllAsync(cancellationToken);
}
