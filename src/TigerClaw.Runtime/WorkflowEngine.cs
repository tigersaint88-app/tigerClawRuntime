using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.RegularExpressions;
using TigerClaw.Capabilities;
using TigerClaw.Capabilities.Resolver;
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
            return new Core.WorkflowExecutionResult
            {
                Success = false,
                Message = $"Workflow not found: {workflowId}",
                ErrorCode = TigerClawErrorCodes.WorkflowNotFound
            };
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
        string? workflowErrorCode = null;
        IReadOnlyList<PrerequisiteIssue> workflowIssues = Array.Empty<PrerequisiteIssue>();
        string? workflowInteractionMessage = null;

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

            var (prerequisitesOk, prereqMessage, prereqIssues) = await EnsurePrerequisitesAsync(skillDef, variables, context, cancellationToken);
            if (!prerequisitesOk)
            {
                waitingHuman = true;
                waitingMessage = prereqMessage;
                workflowErrorCode = TigerClawErrorCodes.PrerequisiteMissing;
                workflowIssues = prereqIssues;
                workflowInteractionMessage = PrerequisiteInteractionFormatter.Format(prereqIssues, prereqMessage);

                var structured = new
                {
                    errorCode = workflowErrorCode,
                    issues = prereqIssues,
                    interactionMessage = workflowInteractionMessage
                };
                var prereqStepResult = new StepExecutionResult
                {
                    StepId = current.Id,
                    Status = "waiting_human",
                    Message = prereqMessage,
                    Output = structured,
                    ArtifactPath = null,
                    CompletedAtUtc = DateTime.UtcNow,
                    ErrorCode = workflowErrorCode,
                    Issues = prereqIssues
                };

                stepResults[current.Id] = prereqStepResult;
                await _taskRepo.SaveStepAsync(taskId, prereqStepResult, cancellationToken);
                await _audit.LogStepAsync(taskId, current.Id, prereqStepResult.Status, prereqStepResult.Message, structured, cancellationToken);
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
                var hardDenied = preflight.Diagnostics.Any(d =>
                    string.Equals(d.Code, "blocked_by_policy", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(d.Code, "none_of_violation", StringComparison.OrdinalIgnoreCase));

                if (hardDenied)
                {
                    var fail = new StepExecutionResult
                    {
                        StepId = current.Id,
                        Status = "failed",
                        Message = $"Capability preflight failed: {failMsg}",
                        Output = preflight.Diagnostics,
                        CompletedAtUtc = DateTime.UtcNow,
                        ErrorCode = TigerClawErrorCodes.CapabilityNotMet,
                        Issues = MapCapabilityDiagnostics(preflight.Diagnostics, skillDef?.Id)
                    };
                    stepResults[current.Id] = fail;
                    await _taskRepo.SaveStepAsync(taskId, fail, cancellationToken);
                    await _audit.LogStepAsync(taskId, current.Id, "failed", fail.Message, fail.Output, cancellationToken);
                    await _taskRepo.UpdateTaskStatusAsync(taskId, "failed", DateTime.UtcNow.ToString("O"), cancellationToken);
                    await _audit.LogTaskCompleteAsync(taskId, false, fail.Message, false, cancellationToken);
                    return new Core.WorkflowExecutionResult
                    {
                        Success = false,
                        Message = fail.Message,
                        Steps = stepResults.Values.ToList(),
                        Artifacts = artifacts,
                        WaitingHuman = false,
                        ErrorCode = TigerClawErrorCodes.CapabilityNotMet,
                        Issues = fail.Issues,
                        InteractionMessage = PrerequisiteInteractionFormatter.Format(fail.Issues, fail.Message)
                    };
                }

                waitingHuman = true;
                waitingMessage = $"Capability preflight failed: {failMsg}";
                workflowErrorCode = TigerClawErrorCodes.CapabilityNotMet;
                workflowIssues = MapCapabilityDiagnostics(preflight.Diagnostics, skillDef?.Id);
                workflowInteractionMessage = PrerequisiteInteractionFormatter.Format(workflowIssues, waitingMessage);
                var capStructured = new
                {
                    errorCode = workflowErrorCode,
                    issues = workflowIssues,
                    interactionMessage = workflowInteractionMessage
                };
                var capStep = new StepExecutionResult
                {
                    StepId = current.Id,
                    Status = "waiting_human",
                    Message = waitingMessage,
                    Output = capStructured,
                    CompletedAtUtc = DateTime.UtcNow,
                    ErrorCode = workflowErrorCode,
                    Issues = workflowIssues
                };
                stepResults[current.Id] = capStep;
                await _taskRepo.SaveStepAsync(taskId, capStep, cancellationToken);
                await _audit.LogStepAsync(taskId, current.Id, capStep.Status, capStep.Message, capStructured, cancellationToken);
                break;
            }

            var skill = _skillRegistry.GetSkill(current.SkillId);
            if (skill == null)
            {
                var fail = new StepExecutionResult
                {
                    StepId = current.Id,
                    Status = "failed",
                    Message = $"Skill not found: {current.SkillId}",
                    ErrorCode = TigerClawErrorCodes.SkillNotFound
                };
                stepResults[current.Id] = fail;
                await _taskRepo.SaveStepAsync(taskId, fail, cancellationToken);
                await _audit.LogStepAsync(taskId, current.Id, "failed", fail.Message, null, cancellationToken);
                break;
            }

            var result = await skill.ExecuteAsync(stepInputs, context, cancellationToken);
            var stepIssues = result.Issues.Count > 0 ? result.Issues : Array.Empty<PrerequisiteIssue>();
            var stepInteraction = stepIssues.Count > 0
                ? PrerequisiteInteractionFormatter.Format(stepIssues, result.Message)
                : null;
            var mergedOutput = result.Output;
            if (result.WaitingHuman && (result.ErrorCode != null || stepIssues.Count > 0))
            {
                mergedOutput = new
                {
                    errorCode = result.ErrorCode,
                    issues = stepIssues,
                    interactionMessage = stepInteraction ?? result.Message,
                    detail = result.Output
                };
            }

            var stepResult = new StepExecutionResult
            {
                StepId = current.Id,
                Status = result.WaitingHuman ? "waiting_human" : (result.Success ? "success" : "failed"),
                Message = result.Message,
                Output = mergedOutput,
                ArtifactPath = result.ArtifactPath,
                CompletedAtUtc = DateTime.UtcNow,
                ErrorCode = result.ErrorCode,
                Issues = stepIssues
            };
            stepResults[current.Id] = stepResult;
            await _taskRepo.SaveStepAsync(taskId, stepResult, cancellationToken);
            await _audit.LogStepAsync(taskId, current.Id, stepResult.Status, result.Message, mergedOutput, cancellationToken);

            if (result.ArtifactPath != null) artifacts.Add(result.ArtifactPath);
            if (result.WaitingHuman)
            {
                waitingHuman = true;
                waitingMessage = result.Message ?? "等待用户确认...";
                workflowErrorCode = result.ErrorCode ?? workflowErrorCode;
                workflowIssues = stepIssues.Count > 0 ? stepIssues : workflowIssues;
                workflowInteractionMessage = stepInteraction ?? workflowInteractionMessage ?? PrerequisiteInteractionFormatter.Format(workflowIssues, waitingMessage);
                break;
            }
            if (!result.Success)
            {
                await _taskRepo.UpdateTaskStatusAsync(taskId, "failed", DateTime.UtcNow.ToString("O"), cancellationToken);
                await _audit.LogTaskCompleteAsync(taskId, false, result.Message, false, cancellationToken);
                return new Core.WorkflowExecutionResult
                {
                    Success = false,
                    Message = result.Message,
                    Steps = stepResults.Values.ToList(),
                    Artifacts = artifacts,
                    WaitingHuman = false,
                    ErrorCode = result.ErrorCode,
                    Issues = stepIssues,
                    InteractionMessage = stepInteraction
                };
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
        await _audit.LogTaskCompleteAsync(taskId, success, waitingHuman ? waitingMessage : null, waitingHuman, cancellationToken);

        return new Core.WorkflowExecutionResult
        {
            Success = success,
            Message = waitingHuman ? waitingMessage : null,
            Steps = stepResults.Values.ToList(),
            Artifacts = artifacts,
            WaitingHuman = waitingHuman,
            ErrorCode = waitingHuman ? workflowErrorCode : null,
            Issues = waitingHuman ? workflowIssues : Array.Empty<PrerequisiteIssue>(),
            InteractionMessage = waitingHuman ? (workflowInteractionMessage ?? PrerequisiteInteractionFormatter.Format(workflowIssues, waitingMessage)) : null
        };
    }

    private static IReadOnlyList<PrerequisiteIssue> MapCapabilityDiagnostics(IReadOnlyList<PrerequisiteDiagnostic> diagnostics, string? skillId)
    {
        var list = new List<PrerequisiteIssue>();
        foreach (var d in diagnostics)
        {
            var hint = string.Equals(d.CapabilityId, CapabilityIds.EmailRead, StringComparison.OrdinalIgnoreCase)
                ? "请完成邮箱账号配置：email.default_account_id、email.accounts.{账号id} 下的 host、port、username、authProfile，以及密码（email.accounts.{id}.password 或 email.auth_profiles.{profile}.password），然后重新运行工作流。"
                : $"需要能力「{d.CapabilityId}」。请满足该能力所需的环境或配置后重试。";

            list.Add(new PrerequisiteIssue
            {
                Kind = "capability",
                Key = d.CapabilityId,
                Code = d.Code,
                Message = d.Message,
                InteractionHint = hint
            });
        }

        return list;
    }

    private async Task<(bool ok, string waitingMessage, IReadOnlyList<PrerequisiteIssue> issues)> EnsurePrerequisitesAsync(
        SkillDefinition? definition,
        IReadOnlyDictionary<string, object?> variables,
        TaskExecutionContext context,
        CancellationToken cancellationToken)
    {
        var waitingMessage = string.Empty;
        var none = Array.Empty<PrerequisiteIssue>();

        var prereq = definition?.Prerequisites;
        var requiredResources = prereq?.RequiredResources ?? Array.Empty<SkillRequiredResource>();
        var requiredConfig = new List<string>();
        if (prereq?.RequiredConfig != null) requiredConfig.AddRange(prereq.RequiredConfig);
        if (definition?.Config != null) requiredConfig.AddRange(definition.Config);

        var requiredAuth = prereq?.RequiredAuth ?? Array.Empty<string>();
        var envVars = definition?.Env ?? Array.Empty<string>();

        var needsCheck = requiredResources.Count > 0 || requiredConfig.Count > 0 || requiredAuth.Count > 0 || envVars.Count > 0;
        if (!needsCheck) return (true, string.Empty, none);

        var isInteractive = _options.Value.IsInteractive;
        var resourceValues = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var rr in requiredResources)
        {
            if (!string.Equals(rr.Type, "email_account", StringComparison.OrdinalIgnoreCase))
            {
                waitingMessage = $"未实现的 prerequisites requiredResources.type：{rr.Type}（skill={definition?.Id}）。";
                return (false, waitingMessage, new[]
                {
                    new PrerequisiteIssue
                    {
                        Kind = "resource",
                        Code = TigerClawErrorCodes.UnsupportedResource,
                        Message = waitingMessage,
                        InteractionHint = "该 skill 声明了尚未支持的资源类型，需要扩展运行时。"
                    }
                });
            }

            var accountId = variables.TryGetValue(rr.Key, out var v) ? v?.ToString() : null;
            if (string.IsNullOrWhiteSpace(accountId))
                accountId = await _memory.Preferences.GetAsync("email.default_account_id", context.UserId, cancellationToken);

            if (string.IsNullOrWhiteSpace(accountId))
            {
                if (!isInteractive)
                {
                    waitingMessage = $"未配置：{rr.Key}（邮箱账号 id）。请先设置 preferences：email.default_account_id（或在任务变量中提供 {rr.Key}）。";
                    return (false, waitingMessage, new[]
                    {
                        new PrerequisiteIssue
                        {
                            Kind = "resource",
                            Key = "email.default_account_id",
                            Code = "missing_email_account_id",
                            Message = waitingMessage,
                            InteractionHint = $"请设置 preference「email.default_account_id」，或在任务变量中提供「{rr.Key}」，保存后重新运行工作流。"
                        }
                    });
                }

                accountId = await PromptForEmailAccountIdAsync(context.UserId, cancellationToken);
                if (!string.IsNullOrWhiteSpace(accountId))
                    await _memory.Preferences.UpsertAsync("email.default_account_id", accountId, context.UserId, cancellationToken);
            }

            if (string.IsNullOrWhiteSpace(accountId))
            {
                waitingMessage = $"未获得邮箱账号 id（{rr.Key}），无法继续执行 skill={definition?.Id}。";
                return (false, waitingMessage, new[]
                {
                    new PrerequisiteIssue
                    {
                        Kind = "resource",
                        Key = "email.default_account_id",
                        Code = "missing_email_account_id",
                        Message = waitingMessage,
                        InteractionHint = "请提供邮箱账号 id（例如 default），写入 email.default_account_id 后重试。"
                    }
                });
            }

            resourceValues[rr.Key] = accountId;
        }

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
            return (true, string.Empty, none);

        if (!isInteractive)
        {
            var parts = new List<string>();
            if (missingPreferenceKeys.Count > 0)
                parts.Add("缺少 preferences: " + string.Join(", ", missingPreferenceKeys.Distinct().Take(20)));
            if (missingEnvVars.Count > 0)
                parts.Add("缺少 env: " + string.Join(", ", missingEnvVars.Distinct().Take(20)));

            waitingMessage = $"未满足 skill prerequisites：{definition?.Id ?? "(unknown)"}。请补齐并重试。{(parts.Count > 0 ? " " + string.Join("；", parts) : "")}";
            var issues = BuildIssuesForMissingPreferences(missingPreferenceKeys);
            if (missingEnvVars.Count > 0)
            {
                var merged = issues.Concat(BuildIssuesForMissingEnv(missingEnvVars)).ToList();
                return (false, waitingMessage, merged);
            }

            return (false, waitingMessage, issues);
        }

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

        foreach (var template in requiredConfig.Concat(requiredAuth).Distinct())
        {
            var resolved = ResolveTemplate(template, resourceValues);
            if (resolved == null)
            {
                waitingMessage = $"prerequisites 模板解析失败：{template}";
                return (false, waitingMessage, new[]
                {
                    new PrerequisiteIssue
                    {
                        Kind = "preference",
                        Key = template,
                        Code = "template_unresolved",
                        Message = waitingMessage,
                        InteractionHint = waitingMessage
                    }
                });
            }

            var value = await _memory.Preferences.GetAsync(resolved, context.UserId, cancellationToken);
            if (string.IsNullOrWhiteSpace(value))
            {
                waitingMessage = $"仍缺少 prerequisites preference：{resolved}";
                return (false, waitingMessage, BuildIssuesForMissingPreferences(new[] { resolved }));
            }
        }

        foreach (var env in envVars)
        {
            if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(env)))
            {
                waitingMessage = $"仍缺少 prerequisites env：{env}";
                return (false, waitingMessage, BuildIssuesForMissingEnv(new[] { env }));
            }
        }

        return (true, string.Empty, none);
    }

    private static IReadOnlyList<PrerequisiteIssue> BuildIssuesForMissingPreferences(IEnumerable<string> keys)
    {
        return keys
            .Distinct()
            .Where(k => !string.IsNullOrWhiteSpace(k))
            .Select(k => k.Trim())
            .Select(k => new PrerequisiteIssue
            {
                Kind = "preference",
                Key = k,
                Code = "missing_preference",
                Message = $"缺少偏好项：{k}",
                InteractionHint = HintForPreferenceKey(k)
            })
            .ToArray();
    }

    private static IReadOnlyList<PrerequisiteIssue> BuildIssuesForMissingEnv(IEnumerable<string> envs)
    {
        return envs
            .Distinct()
            .Where(e => !string.IsNullOrWhiteSpace(e))
            .Select(e => e.Trim())
            .Select(e => new PrerequisiteIssue
            {
                Kind = "env",
                Key = e,
                Code = "missing_env",
                Message = $"缺少环境变量：{e}",
                InteractionHint = $"请设置环境变量「{e}」后重新运行工作流。"
            })
            .ToArray();
    }

    private static string HintForPreferenceKey(string key)
    {
        if (key.Contains("email.accounts.", StringComparison.OrdinalIgnoreCase) && key.EndsWith(".host", StringComparison.OrdinalIgnoreCase))
            return "请填写真实可解析的 IMAP 主机名（如 imap.gmail.com），不要使用示例占位域名；写入后重新运行工作流。";
        if (key.Contains("email.accounts.", StringComparison.OrdinalIgnoreCase) && key.EndsWith(".password", StringComparison.OrdinalIgnoreCase))
            return "请填写该邮箱账号的 IMAP 密码或应用专用密码；也可改用 email.auth_profiles.{profile}.password。";
        if (key.Contains("email.accounts.", StringComparison.OrdinalIgnoreCase) && key.EndsWith(".username", StringComparison.OrdinalIgnoreCase))
            return "请填写完整的邮箱登录用户名（通常为邮箱地址）。";
        if (key.Contains("email.accounts.", StringComparison.OrdinalIgnoreCase) && key.EndsWith(".port", StringComparison.OrdinalIgnoreCase))
            return "请填写 IMAP 端口（常见为 993 SSL 或 143 STARTTLS）。";
        if (key.Contains("email.accounts.", StringComparison.OrdinalIgnoreCase) && key.EndsWith(".authProfile", StringComparison.OrdinalIgnoreCase))
            return "请填写 authProfile 名称（与 email.auth_profiles.* 中的配置对应）。";
        return $"请通过 POST /memory/preferences 或 CLI 写入 preference「{key}」，然后重新运行同一工作流。";
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
