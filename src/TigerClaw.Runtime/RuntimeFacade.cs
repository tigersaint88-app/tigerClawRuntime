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
                    ErrorCode = "NOT_IMPLEMENTED",
                    Outcome = TaskOutcomes.Failed
                };
            }

            var vars = new Dictionary<string, object?> { ["input"] = request.InputText };
            foreach (var kv in routing.Parameters)
                vars[kv.Key] = kv.Value;
            var result = await _engine.ExecuteAsync(plan.WorkflowId, taskId, request.UserId, vars, cancellationToken);

            return FromWorkflowExecution(request.RequestId, result, plan.WorkflowId, "Task completed.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Task failed: {TaskId}", taskId);
            return new TaskResponse
            {
                RequestId = request.RequestId,
                Success = false,
                Message = ex.Message,
                ErrorCode = "INTERNAL_ERROR",
                Outcome = TaskOutcomes.Failed
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
        return FromWorkflowExecution(taskId, result, workflowId, "Workflow completed.");
    }

    private static TaskResponse FromWorkflowExecution(
        string requestId,
        Core.WorkflowExecutionResult result,
        string? workflowId,
        string completedFinalText)
    {
        var interaction = result.InteractionMessage
            ?? (result.Issues.Count > 0 ? PrerequisiteInteractionFormatter.Format(result.Issues, result.Message) : null);

        var outcome = result.WaitingHuman
            ? TaskOutcomes.NeedsUserInput
            : (result.Success ? TaskOutcomes.Completed : TaskOutcomes.Failed);

        var finalText = result.WaitingHuman
            ? (interaction ?? result.Message ?? "请补齐配置后重新运行工作流。")
            : (result.Success ? completedFinalText : result.Message);

        return new TaskResponse
        {
            RequestId = requestId,
            Success = result.Success,
            Message = result.Message,
            FinalText = finalText,
            Artifacts = result.Artifacts.ToList(),
            WorkflowId = workflowId,
            Steps = result.Steps.ToList(),
            ErrorCode = result.ErrorCode,
            WaitingHuman = result.WaitingHuman,
            Issues = result.Issues.ToList(),
            InteractionMessage = interaction,
            Outcome = outcome,
            RequiresUserInput = result.WaitingHuman,
            SuggestedPreferenceKeys = ExtractSuggestedPreferenceKeys(result.Issues),
            RemediationHint = result.WaitingHuman ? ClientRemediationHints.PreferencesThenRerun : null
        };
    }

    private static IReadOnlyList<string> ExtractSuggestedPreferenceKeys(IReadOnlyList<PrerequisiteIssue> issues)
    {
        if (issues.Count == 0) return Array.Empty<string>();
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var i in issues)
        {
            if (string.IsNullOrWhiteSpace(i.Key)) continue;
            if (string.Equals(i.Kind, "preference", StringComparison.OrdinalIgnoreCase)
                || string.Equals(i.Kind, "network", StringComparison.OrdinalIgnoreCase)
                || string.Equals(i.Kind, "resource", StringComparison.OrdinalIgnoreCase))
                set.Add(i.Key.Trim());
        }

        return set.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList();
    }

    public Task<IReadOnlyList<SkillDefinition>> ListSkillsAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(_skillRegistry.ListAll());

    public Task<IReadOnlyList<WorkflowDefinition>> ListWorkflowsAsync(CancellationToken cancellationToken = default)
        => _workflowLoader.LoadAllAsync(cancellationToken);
}
