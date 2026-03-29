using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using TigerClaw.Core;
using TigerClaw.Infrastructure.Loaders;
using TigerClaw.Models;
using TigerClaw.Workflows;

namespace TigerClaw.Cli;

/// <summary>
/// Routes parsed CLI commands to <see cref="IRuntimeFacade"/> and related services.
/// </summary>
public sealed class CliCommandRouter
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true, WriteIndented = false };

    private readonly IServiceProvider _services;

    public CliCommandRouter(IServiceProvider services) => _services = services;

    public async Task<CliCommandResult> RouteAsync(CliCommandRequest req, CancellationToken cancellationToken = default)
    {
        if (req.Help)
            return HelpFor(req);

        return req.Group.ToLowerInvariant() switch
        {
            "" => CliCommandResult.Fail("No command specified.", 1),
            "run" => await RunTaskAsync(req, cancellationToken),
            "workflow" => await WorkflowAsync(req, cancellationToken),
            "skills" => await SkillsAsync(req, cancellationToken),
            "memory" => await MemoryAsync(req, cancellationToken),
            "taobao" => await TaobaoAsync(req, cancellationToken),
            "models" or "configure" or "doctor" or "logs" => StubNotImplemented(req.Group),
            "help" => CliCommandResult.Ok(string.IsNullOrEmpty(req.Action)
                ? CliHelp.Global
                : CliHelp.ForGroup(req.Action)),
            _ => CliCommandResult.Fail($"Unknown group: {req.Group}", 1)
        };
    }

    private static CliCommandResult HelpFor(CliCommandRequest req)
    {
        if (string.IsNullOrEmpty(req.Group))
            return CliCommandResult.Ok(CliHelp.Global);
        return CliCommandResult.Ok(CliHelp.ForGroup(req.Group));
    }

    private static CliCommandResult StubNotImplemented(string group) =>
        CliCommandResult.Fail($"Command group '{group}' is not implemented in TigerClaw V1 CLI.", 1);

    private async Task<CliCommandResult> RunTaskAsync(CliCommandRequest req, CancellationToken cancellationToken)
    {
        var facade = _services.GetRequiredService<IRuntimeFacade>();
        var text = GetRunInputText(req);
        if (string.IsNullOrWhiteSpace(text))
            text = await Console.In.ReadToEndAsync(cancellationToken);

        text = text.Trim();
        if (string.IsNullOrEmpty(text))
            return CliCommandResult.Fail("No task text (and stdin was empty).", 1);

        var sessionId = Guid.NewGuid().ToString("N");
        if (req.Options.TryGetValue("session", out var sid) && !string.IsNullOrEmpty(sid))
            sessionId = sid;

        var taskReq = new TaskRequest
        {
            RequestId = Guid.NewGuid().ToString("N"),
            SessionId = sessionId,
            UserId = "local-user",
            InputText = text,
            Channel = "cli"
        };

        try
        {
            var resp = await facade.RunTaskAsync(taskReq, cancellationToken);
            return TaskResponseToResult(resp);
        }
        catch (Exception ex)
        {
            return CliCommandResult.Fail(ex.Message, 2);
        }
    }

    private static string GetRunInputText(CliCommandRequest req)
    {
        var pos = req.Positional;
        if (pos.Count <= 1) return "";
        return string.Join(" ", pos.Skip(1));
    }

    private async Task<CliCommandResult> WorkflowAsync(CliCommandRequest req, CancellationToken cancellationToken)
    {
        var facade = _services.GetRequiredService<IRuntimeFacade>();
        var loader = _services.GetRequiredService<WorkflowDefinitionLoader>();
        var pos = req.Positional;
        var action = pos.Count > 1 ? pos[1].ToLowerInvariant() : "";

        try
        {
            switch (action)
            {
                case "list":
                {
                    var list = await facade.ListWorkflowsAsync(cancellationToken);
                    if (req.JsonOutput)
                        return CliCommandResult.Ok(data: list.Select(w => new { w.Id, w.Name, w.Description }).ToList());
                    var lines = list.Select(w => $"  {w.Id}: {w.Name}");
                    return CliCommandResult.Ok(string.Join(Environment.NewLine, lines));
                }
                case "show":
                {
                    if (pos.Count < 3)
                        return CliCommandResult.Fail("workflow show requires <id>.", 1);
                    var id = pos[2];
                    var def = await loader.LoadAsync(id, cancellationToken);
                    if (def == null)
                        return CliCommandResult.Fail($"Workflow not found: {id}", 1);
                    if (req.JsonOutput)
                        return CliCommandResult.Ok(data: def);
                    return CliCommandResult.Ok(JsonSerializer.Serialize(def, new JsonSerializerOptions { WriteIndented = true }));
                }
                case "run":
                {
                    if (pos.Count < 3)
                        return CliCommandResult.Fail("workflow run requires <id>.", 1);
                    var wfId = pos[2];
                    var inputs = BuildWorkflowInputs(wfId, pos);
                    var resp = await facade.RunWorkflowAsync(wfId, inputs, cancellationToken: cancellationToken);
                    return TaskResponseToResult(resp);
                }
                case "validate":
                {
                    if (pos.Count < 3)
                        return CliCommandResult.Fail("workflow validate requires <file>.", 1);
                    var path = Path.GetFullPath(pos[2]);
                    if (!File.Exists(path))
                        return CliCommandResult.Fail($"File not found: {path}", 3);

                    var json = await File.ReadAllTextAsync(path, cancellationToken);
                    WorkflowDefinition? def;
                    try
                    {
                        def = JsonSerializer.Deserialize<WorkflowDefinition>(json, JsonOptions);
                    }
                    catch (Exception ex)
                    {
                        return CliCommandResult.Fail($"Invalid JSON: {ex.Message}", 3);
                    }

                    if (def == null)
                        return CliCommandResult.Fail("Workflow definition is null.", 3);

                    var errors = WorkflowValidator.Validate(def);
                    if (errors.Count > 0)
                    {
                        var msg = string.Join("; ", errors);
                        return req.JsonOutput
                            ? CliCommandResult.Fail(msg, 3, data: new { valid = false, errors })
                            : CliCommandResult.Fail($"Validation failed: {msg}", 3);
                    }

                    return req.JsonOutput
                        ? CliCommandResult.Ok(data: new { valid = true, id = def.Id })
                        : CliCommandResult.Ok($"Valid workflow: {def.Id}");
                }
                default:
                    return CliCommandResult.Fail($"Unknown workflow action: {action}. Use: list, show, run, validate.", 1);
            }
        }
        catch (Exception ex)
        {
            return CliCommandResult.Fail(ex.Message, 2);
        }
    }

    private async Task<CliCommandResult> SkillsAsync(CliCommandRequest req, CancellationToken cancellationToken)
    {
        var facade = _services.GetRequiredService<IRuntimeFacade>();
        var registry = _services.GetRequiredService<ISkillRegistry>();
        var factory = _services.GetRequiredService<ITaskContextFactory>();
        var pos = req.Positional;
        var action = pos.Count > 1 ? pos[1].ToLowerInvariant() : "";

        try
        {
            switch (action)
            {
                case "list":
                {
                    var skills = await facade.ListSkillsAsync(cancellationToken);
                    if (req.JsonOutput)
                        return CliCommandResult.Ok(data: skills.Select(s => new { s.Id, s.Name }).ToList());
                    return CliCommandResult.Ok(string.Join(Environment.NewLine, skills.Select(s => $"  {s.Id}: {s.Name}")));
                }
                case "show":
                {
                    if (pos.Count < 3)
                        return CliCommandResult.Fail("skills show requires <id>.", 1);
                    var id = pos[2];
                    var def = registry.GetDefinition(id);
                    if (def == null)
                        return CliCommandResult.Fail($"Skill not found: {id}", 1);
                    if (req.JsonOutput)
                        return CliCommandResult.Ok(data: def);
                    return CliCommandResult.Ok(JsonSerializer.Serialize(def, new JsonSerializerOptions { WriteIndented = true }));
                }
                case "exec":
                {
                    if (pos.Count < 3)
                        return CliCommandResult.Fail("skills exec requires <id>.", 1);
                    var id = pos[2];
                    var skill = registry.GetSkill(id);
                    if (skill == null)
                        return CliCommandResult.Fail($"Skill not found: {id}", 1);

                    IReadOnlyDictionary<string, object?> inputs = new Dictionary<string, object?>();
                    if (req.Options.TryGetValue("inputs", out var inj) && !string.IsNullOrWhiteSpace(inj))
                    {
                        try
                        {
                            var dict = JsonSerializer.Deserialize<Dictionary<string, object?>>(inj, JsonOptions);
                            if (dict != null) inputs = dict;
                        }
                        catch (Exception ex)
                        {
                            return CliCommandResult.Fail($"Invalid --inputs JSON: {ex.Message}", 1);
                        }
                    }

                    var ctx = factory.Create(
                        Guid.NewGuid().ToString("N"),
                        "cli-skill-exec",
                        "local-user",
                        "exec",
                        new Dictionary<string, object?>());

                    var result = await skill.ExecuteAsync(inputs, ctx, cancellationToken);
                    if (req.JsonOutput)
                        return CliCommandResult.Ok(data: new { result.Success, result.Message, result.Output, result.ArtifactPath, result.WaitingHuman });

                    if (!result.Success)
                        return CliCommandResult.Fail(result.Message ?? "Skill failed", 2);

                    var text = result.Output?.ToString() ?? result.Message ?? "Done.";
                    return CliCommandResult.Ok(text);
                }
                default:
                    return CliCommandResult.Fail($"Unknown skills action: {action}. Use: list, show, exec.", 1);
            }
        }
        catch (Exception ex)
        {
            return CliCommandResult.Fail(ex.Message, 2);
        }
    }

    private async Task<CliCommandResult> MemoryAsync(CliCommandRequest req, CancellationToken cancellationToken)
    {
        var memory = _services.GetRequiredService<IMemoryStore>();
        var pos = req.Positional;
        if (pos.Count < 2)
            return CliCommandResult.Fail("memory requires a subcommand (preferences, aliases, procedures, search).", 1);

        var sub = pos[1].ToLowerInvariant();

        try
        {
            if (sub is "preference" or "preferences")
            {
                var action = pos.Count > 2 ? pos[2].ToLowerInvariant() : "";
                if (action == "list")
                {
                    var prefs = await memory.Preferences.ListAllAsync(null, cancellationToken);
                    if (req.JsonOutput)
                        return CliCommandResult.Ok(data: prefs);
                    return CliCommandResult.Ok(string.Join(Environment.NewLine, prefs.Select(p => $"  {p.Key} = {p.Value}")));
                }

                if (action == "set" && pos.Count >= 5)
                {
                    await memory.Preferences.UpsertAsync(pos[3], pos[4], null, cancellationToken);
                    return CliCommandResult.Ok($"Preference set: {pos[3]} = {pos[4]}");
                }

                return CliCommandResult.Fail("memory preference(s): list | set <key> <value>", 1);
            }

            if (sub == "aliases")
            {
                var action = pos.Count > 2 ? pos[2].ToLowerInvariant() : "";
                if (action == "list")
                {
                    var aliases = await memory.Aliases.ListAllAsync(null, cancellationToken);
                    if (req.JsonOutput)
                        return CliCommandResult.Ok(data: aliases);
                    return CliCommandResult.Ok(string.Join(Environment.NewLine, aliases.Select(a => $"  {a.Alias} -> {a.ResolvedValue}")));
                }

                if (action == "set" && pos.Count >= 5)
                {
                    await memory.Aliases.UpsertAsync(pos[3], pos[4], null, cancellationToken);
                    return CliCommandResult.Ok($"Alias set: {pos[3]} -> {pos[4]}");
                }

                return CliCommandResult.Fail("memory aliases: list | set <alias> <resolved>", 1);
            }

            if (sub == "procedures")
            {
                var action = pos.Count > 2 ? pos[2].ToLowerInvariant() : "";
                if (action == "list")
                {
                    var list = await memory.Procedures.ListAllAsync(null, cancellationToken);
                    if (req.JsonOutput)
                        return CliCommandResult.Ok(data: list);
                    return CliCommandResult.Ok(string.Join(Environment.NewLine,
                        list.Select(p => $"  {p.TaskType} @ {p.CreatedAtUtc:o}")));
                }

                return CliCommandResult.Fail("memory procedures: list", 1);
            }

            if (sub == "search")
                return CliCommandResult.Fail("memory search is not implemented in V1 (use procedures list).", 1);

            return CliCommandResult.Fail($"Unknown memory subcommand: {sub}", 1);
        }
        catch (Exception ex)
        {
            return CliCommandResult.Fail(ex.Message, 2);
        }
    }

    private async Task<CliCommandResult> TaobaoAsync(CliCommandRequest req, CancellationToken cancellationToken)
    {
        var facade = _services.GetRequiredService<IRuntimeFacade>();
        var pos = req.Positional;
        if (pos.Count < 2 || !string.Equals(pos[1], "search", StringComparison.OrdinalIgnoreCase))
            return CliCommandResult.Fail("Use: taobao search [keyword]", 1);

        var keyword = pos.Count > 2 ? string.Join(" ", pos.Skip(2)).Trim() : "";
        if (string.IsNullOrEmpty(keyword))
            keyword = "空调";

        try
        {
            var resp = await facade.RunWorkflowAsync("taobao_search",
                new Dictionary<string, object?> { ["keyword"] = keyword }, cancellationToken: cancellationToken);
            return TaskResponseToResult(resp);
        }
        catch (Exception ex)
        {
            return CliCommandResult.Fail(ex.Message, 2);
        }
    }

    private static Dictionary<string, object?> BuildWorkflowInputs(string workflowId, IReadOnlyList<string> positional)
    {
        var inputs = new Dictionary<string, object?>();
        if (positional.Count > 3)
        {
            var tail = string.Join(" ", positional.Skip(3)).Trim();
            if (string.Equals(workflowId, "taobao_search", StringComparison.OrdinalIgnoreCase))
                inputs["keyword"] = tail;
            else if (string.Equals(workflowId, "open_url_with_human_checkpoint", StringComparison.OrdinalIgnoreCase))
                inputs["url"] = tail;
        }
        else if (string.Equals(workflowId, "taobao_search", StringComparison.OrdinalIgnoreCase))
            inputs["keyword"] = "空调";
        else if (string.Equals(workflowId, "open_url_with_human_checkpoint", StringComparison.OrdinalIgnoreCase))
            inputs["url"] = "https://www.example.com";

        return inputs;
    }

    private static CliCommandResult TaskResponseToResult(TaskResponse resp)
    {
        if (resp.WaitingHuman)
        {
            var text = resp.InteractionMessage ?? resp.FinalText ?? resp.Message ?? "请补齐配置或确认后重新运行。";
            return CliCommandResult.Ok(text, data: resp);
        }

        if (!resp.Success)
            return CliCommandResult.Fail(resp.Message ?? "Task failed", 2, data: resp);

        var okText = resp.FinalText ?? "Done.";
        if (resp.Artifacts.Count > 0)
            okText += Environment.NewLine + "Artifacts: " + string.Join(", ", resp.Artifacts);

        return CliCommandResult.Ok(okText, data: resp);
    }
}
