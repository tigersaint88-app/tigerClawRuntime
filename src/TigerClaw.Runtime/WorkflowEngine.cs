using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.RegularExpressions;
using TigerClaw.Capabilities;
using TigerClaw.Infrastructure.Options;
using TigerClaw.Models;

namespace TigerClaw.Runtime;

/// <summary>
/// Executes workflows step by step.
/// </summary>
public class WorkflowEngine : Core.IWorkflowEngine
{
    private readonly Core.IWorkflowLoader _loader;
    private readonly Core.ISkillRegistry _skillRegistry;
    private readonly Core.IAuditLogger _audit;
    private readonly Infrastructure.Repositories.TaskRepository _taskRepo;
    private readonly ILogger<WorkflowEngine> _logger;
    private readonly Core.IMemoryStore _memory;
    private readonly IOptions<TigerClawOptions> _options;
    private readonly ResourceSnapshotBuilder _resourceSnapshotBuilder;
    private readonly PreflightCheck _preflightCheck;

    public WorkflowEngine(
        Core.IWorkflowLoader loader,
        Core.ISkillRegistry skillRegistry,
        Core.IAuditLogger audit,
        Infrastructure.Repositories.TaskRepository taskRepo,
        ILogger<WorkflowEngine> logger,
        Core.IMemoryStore memoryStore,
        IOptions<TigerClawOptions> options,
        ResourceSnapshotBuilder resourceSnapshotBuilder,
        PreflightCheck preflightCheck)
    {
        _loader = loader;
        _skillRegistry = skillRegistry;
        _audit = audit;
        _taskRepo = taskRepo;
        _logger = logger;
        _memory = memoryStore;
        _options = options;
        _resourceSnapshotBuilder = resourceSnapshotBuilder;
        _preflightCheck = preflightCheck;
    }

    public async Task<Core.WorkflowExecutionResult> ExecuteAsync(
        string workflowId,
        string taskId,
        string userId,
        IReadOnlyDictionary<string, object?> inputs,
        CancellationToken cancellationToken = default)
    {
        var def = await _loader.LoadAsync(workflowId, cancellationToken);
        if (def == null)
        {
            _logger.LogError("Workflow not found: {WorkflowId}", workflowId);
            return new Core.WorkflowExecutionResult { Success = false, Message = $"Workflow not found: {workflowId}" };
        }

        await _taskRepo.SaveTaskAsync(taskId, workflowId, userId, null, "running", cancellationToken);
        await _audit.LogTaskStartAsync(taskId, workflowId, userId, "", cancellationToken);

        var steps = def.Steps.ToList();
        if (steps.Count == 0)
        {
            await _taskRepo.UpdateTaskStatusAsync(taskId, "success", DateTime.UtcNow.ToString("O"), cancellationToken);
            return new Core.WorkflowExecutionResult { Success = true, Steps = Array.Empty<StepExecutionResult>() };
        }

        var variables = new Dictionary<string, object?>(inputs);
        var stepResults = new Dictionary<string, StepExecutionResult>();
        var artifacts = new List<string>();
        var resourceSnapshot = await _resourceSnapshotBuilder.BuildAsync(cancellationToken);
        var current = steps[0];
        var idx = 0;
        var waitingHuman = false;
        string? waitingMessage = null;

        while (current != null)
        {
            var stepInputs = StepInputResolver.Resolve(current.Inputs ?? new Dictionary<string, object?>(), variables, stepResults);
            var context = new TaskExecutionContext
            {
                TaskId = taskId,
                WorkflowId = workflowId,
                CurrentStepId = current.Id,
                Variables = variables,
                Artifacts = artifacts.ToDictionary(a => a, _ => (object?)""),
                UserId = userId,
                StepResults = stepResults
            };

            var skillDef = _skillRegistry.GetDefinition(current.SkillId);

            var (prerequisitesOk, prereqMessage) = await EnsurePrerequisitesAsync(skillDef, variables, context, cancellationToken);
            if (!prerequisitesOk)
            {
                waitingHuman = true;
                waitingMessage = prereqMessage;

                var prereqStepResult = new StepExecutionResult
                {
                    StepId = current.Id,
                    Status = "waiting_human",
                    Message = prereqMessage,
                    Output = null,
                    ArtifactPath = null,
                    CompletedAtUtc = DateTime.UtcNow
                };

                stepResults[current.Id] = prereqStepResult;
                await _taskRepo.SaveStepAsync(taskId, prereqStepResult, cancellationToken);
                await _audit.LogStepAsync(taskId, current.Id, prereqStepResult.Status, prereqStepResult.Message, null, cancellationToken);
                break;
            }

            var preflight = await _preflightCheck.RunAsync(
                skillDef,
                current,
                resourceSnapshot,
                _options.Value.CapabilityPolicy,
                userId,
                _memory.Preferences,
                new CapabilityProviderRegistry(),
                cancellationToken);
            if (!preflight.Allowed)
            {
                var failMsg = string.Join("; ", preflight.Diagnostics.Select(d => d.Message));
                var fail = new StepExecutionResult
                {
                    StepId = current.Id,
                    Status = "failed",
                    Message = $"Capability preflight failed: {failMsg}",
                    Output = preflight.Diagnostics,
                    CompletedAtUtc = DateTime.UtcNow
                };
                stepResults[current.Id] = fail;
                await _taskRepo.SaveStepAsync(taskId, fail, cancellationToken);
                await _audit.LogStepAsync(taskId, current.Id, "failed", fail.Message, fail.Output, cancellationToken);
                await _taskRepo.UpdateTaskStatusAsync(taskId, "failed", DateTime.UtcNow.ToString("O"), cancellationToken);
                await _audit.LogTaskCompleteAsync(taskId, false, fail.Message, cancellationToken);
                return new Core.WorkflowExecutionResult
                {
                    Success = false,
                    Message = fail.Message,
                    Steps = stepResults.Values.ToList(),
                    Artifacts = artifacts,
                    WaitingHuman = false
                };
            }

            var skill = _skillRegistry.GetSkill(current.SkillId);
            if (skill == null)
            {
                var fail = new StepExecutionResult { StepId = current.Id, Status = "failed", Message = $"Skill not found: {current.SkillId}" };
                stepResults[current.Id] = fail;
                await _taskRepo.SaveStepAsync(taskId, fail, cancellationToken);
                await _audit.LogStepAsync(taskId, current.Id, "failed", fail.Message, null, cancellationToken);
                break;
            }

            var result = await skill.ExecuteAsync(stepInputs, context, cancellationToken);
            var stepResult = new StepExecutionResult
            {
                StepId = current.Id,
                Status = result.WaitingHuman ? "waiting_human" : (result.Success ? "success" : "failed"),
                Message = result.Message,
                Output = result.Output,
                ArtifactPath = result.ArtifactPath,
                CompletedAtUtc = DateTime.UtcNow
            };
            stepResults[current.Id] = stepResult;
            await _taskRepo.SaveStepAsync(taskId, stepResult, cancellationToken);
            await _audit.LogStepAsync(taskId, current.Id, stepResult.Status, result.Message, result.Output, cancellationToken);

            if (result.ArtifactPath != null) artifacts.Add(result.ArtifactPath);
            if (result.WaitingHuman)
            {
                waitingHuman = true;
                waitingMessage = result.Message ?? "等待用户确认...";
                break;
            }
            if (!result.Success)
            {
                await _taskRepo.UpdateTaskStatusAsync(taskId, "failed", DateTime.UtcNow.ToString("O"), cancellationToken);
                await _audit.LogTaskCompleteAsync(taskId, false, result.Message, cancellationToken);
                return new Core.WorkflowExecutionResult { Success = false, Message = result.Message, Steps = stepResults.Values.ToList(), Artifacts = artifacts, WaitingHuman = false };
            }

            if (result.Output != null) variables[$"step.{current.Id}"] = result.Output;

            var nextId = current.NextStepId;
            if (string.IsNullOrEmpty(nextId) && idx + 1 < steps.Count)
                nextId = steps[idx + 1].Id;
            current = steps.FirstOrDefault(s => s.Id == nextId);
            idx = current != null ? steps.IndexOf(current) : -1;
        }

        var hasFailed = stepResults.Values.Any(r => r.Status == "failed");
        var success = !hasFailed && !waitingHuman;
        var finalStatus = waitingHuman ? "waiting_human" : (success ? "success" : "failed");
        await _taskRepo.UpdateTaskStatusAsync(taskId, finalStatus, DateTime.UtcNow.ToString("O"), cancellationToken);
        await _audit.LogTaskCompleteAsync(taskId, success, waitingHuman ? waitingMessage : null, cancellationToken);

        return new Core.WorkflowExecutionResult
        {
            Success = success,
            Message = waitingHuman ? waitingMessage : null,
            Steps = stepResults.Values.ToList(),
            Artifacts = artifacts,
            WaitingHuman = waitingHuman
        };
    }

    private async Task<(bool ok, string waitingMessage)> EnsurePrerequisitesAsync(
        SkillDefinition? definition,
        IReadOnlyDictionary<string, object?> variables,
        TaskExecutionContext context,
        CancellationToken cancellationToken)
    {
        var waitingMessage = string.Empty;

        var prereq = definition?.Prerequisites;
        var requiredResources = prereq?.RequiredResources ?? Array.Empty<SkillRequiredResource>();
        var requiredConfig = new List<string>();
        if (prereq?.RequiredConfig != null) requiredConfig.AddRange(prereq.RequiredConfig);
        if (definition?.Config != null) requiredConfig.AddRange(definition.Config);

        var requiredAuth = prereq?.RequiredAuth ?? Array.Empty<string>();
        var envVars = definition?.Env ?? Array.Empty<string>();

        var needsCheck = requiredResources.Count > 0 || requiredConfig.Count > 0 || requiredAuth.Count > 0 || envVars.Count > 0;
        if (!needsCheck) return (true, string.Empty);

        var isInteractive = _options.Value.IsInteractive;
        var resourceValues = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var rr in requiredResources)
        {
            if (!string.Equals(rr.Type, "email_account", StringComparison.OrdinalIgnoreCase))
            {
                waitingMessage = $"未实现的 prerequisites requiredResources.type：{rr.Type}（skill={definition?.Id}）。";
                return (false, waitingMessage);
            }

            var accountId = variables.TryGetValue(rr.Key, out var v) ? v?.ToString() : null;
            if (string.IsNullOrWhiteSpace(accountId))
                accountId = await _memory.Preferences.GetAsync("email.default_account_id", context.UserId, cancellationToken);

            if (string.IsNullOrWhiteSpace(accountId))
            {
                if (!isInteractive)
                {
                    waitingMessage = $"未配置：{rr.Key}（邮箱账号 id）。请先设置 preferences：email.default_account_id（或在任务变量中提供 {rr.Key}）。";
                    return (false, waitingMessage);
                }

                accountId = await PromptForEmailAccountIdAsync(context.UserId, cancellationToken);
                if (!string.IsNullOrWhiteSpace(accountId))
                    await _memory.Preferences.UpsertAsync("email.default_account_id", accountId, context.UserId, cancellationToken);
            }

            if (string.IsNullOrWhiteSpace(accountId))
            {
                waitingMessage = $"未获得邮箱账号 id（{rr.Key}），无法继续执行 skill={definition?.Id}。";
                return (false, waitingMessage);
            }

            resourceValues[rr.Key] = accountId;
        }

        // Collect missing config/auth/env keys.
        var missingPreferenceKeys = new List<string>();
        var missingEnvVars = new List<string>();

        foreach (var template in requiredConfig)
        {
            var resolved = ResolveTemplate(template, resourceValues);
            if (resolved == null)
            {
                missingPreferenceKeys.Add(template);
                continue;
            }

            var value = await _memory.Preferences.GetAsync(resolved, context.UserId, cancellationToken);
            if (string.IsNullOrWhiteSpace(value))
                missingPreferenceKeys.Add(resolved);
        }

        foreach (var template in requiredAuth)
        {
            var resolved = ResolveTemplate(template, resourceValues);
            if (resolved == null)
            {
                missingPreferenceKeys.Add(template);
                continue;
            }

            var value = await _memory.Preferences.GetAsync(resolved, context.UserId, cancellationToken);
            if (string.IsNullOrWhiteSpace(value))
                missingPreferenceKeys.Add(resolved);
        }

        foreach (var env in envVars)
        {
            if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(env)))
                missingEnvVars.Add(env);
        }

        if (missingPreferenceKeys.Count == 0 && missingEnvVars.Count == 0)
            return (true, string.Empty);

        if (!isInteractive)
        {
            var parts = new List<string>();
            if (missingPreferenceKeys.Count > 0)
                parts.Add("缺少 preferences: " + string.Join(", ", missingPreferenceKeys.Distinct().Take(20)));
            if (missingEnvVars.Count > 0)
                parts.Add("缺少 env: " + string.Join(", ", missingEnvVars.Distinct().Take(20)));

            waitingMessage = $"未满足 skill prerequisites：{definition?.Id ?? "(unknown)"}。请补齐并重试。{(parts.Count > 0 ? " " + string.Join("；", parts) : "")}";
            return (false, waitingMessage);
        }

        // Interactive prompt: acquire missing preference keys / env vars.
        foreach (var key in missingPreferenceKeys.Distinct())
        {
            if (string.IsNullOrWhiteSpace(key)) continue;
            Console.WriteLine($"Missing config for skill {definition?.Id}: {key}");
            Console.Write($"{key} = ");
            var val = Console.ReadLine();
            if (!string.IsNullOrWhiteSpace(val))
                await _memory.Preferences.UpsertAsync(key, val.Trim(), context.UserId, cancellationToken);
        }

        foreach (var env in missingEnvVars.Distinct())
        {
            Console.WriteLine($"Missing env for skill {definition?.Id}: {env}");
            Console.Write($"{env} = ");
            var val = Console.ReadLine();
            if (!string.IsNullOrWhiteSpace(val))
                Environment.SetEnvironmentVariable(env, val.Trim());
        }

        // Re-check after prompting.
        foreach (var template in requiredConfig.Concat(requiredAuth).Distinct())
        {
            var resolved = ResolveTemplate(template, resourceValues);
            if (resolved == null)
            {
                waitingMessage = $"prerequisites 模板解析失败：{template}";
                return (false, waitingMessage);
            }

            var value = await _memory.Preferences.GetAsync(resolved, context.UserId, cancellationToken);
            if (string.IsNullOrWhiteSpace(value))
            {
                waitingMessage = $"仍缺少 prerequisites preference：{resolved}";
                return (false, waitingMessage);
            }
        }

        foreach (var env in envVars)
        {
            if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(env)))
            {
                waitingMessage = $"仍缺少 prerequisites env：{env}";
                return (false, waitingMessage);
            }
        }

        return (true, string.Empty);
    }

    private async Task<string?> PromptForEmailAccountIdAsync(string userId, CancellationToken cancellationToken)
    {
        var prefs = await _memory.Preferences.ListAllAsync(userId, cancellationToken);
        var accounts = prefs
            .Select(p => p.Key)
            .Where(k => k.StartsWith("email.accounts.", StringComparison.OrdinalIgnoreCase))
            .Select(k =>
            {
                var m = Regex.Match(k, @"^email\.accounts\.(?<id>[^\.]+)\.");
                return m.Success ? m.Groups["id"].Value : null;
            })
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Select(id => id!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (accounts.Count == 1)
        {
            return accounts[0];
        }

        if (accounts.Count > 1)
        {
            Console.WriteLine("Detected existing email accounts: " + string.Join(", ", accounts));
            Console.Write("Choose accountId (or type a new one): ");
            var choice = Console.ReadLine();
            if (!string.IsNullOrWhiteSpace(choice)) return choice.Trim();
        }
        else
        {
            Console.Write("No email accounts found in preferences. Please input accountId (e.g. default): ");
            var id = Console.ReadLine();
            if (!string.IsNullOrWhiteSpace(id)) return id.Trim();
        }

        return null;
    }

    private static string? ResolveTemplate(string template, IReadOnlyDictionary<string, string> values)
    {
        var resolved = template;
        foreach (var kv in values)
        {
            resolved = resolved.Replace("{" + kv.Key + "}", kv.Value, StringComparison.OrdinalIgnoreCase);
        }

        if (Regex.IsMatch(resolved, @"\{[^}]+\}"))
            return null;

        return resolved;
    }
}
