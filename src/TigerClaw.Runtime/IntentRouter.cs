using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using TigerClaw.Models;

namespace TigerClaw.Runtime;

/// <summary>
/// Rule-based intent routing (keywords, regex). Used as fallback when LLM routing is off or fails.
/// </summary>
public sealed class RuleBasedIntentRouter : Core.IIntentRouter
{
    private readonly ILogger<RuleBasedIntentRouter> _logger;

    public RuleBasedIntentRouter(ILogger<RuleBasedIntentRouter> logger) => _logger = logger;

    public Task<Core.RoutingResult> RouteAsync(TaskRequest request, CancellationToken cancellationToken = default)
    {
        var text = request.InputText.Trim();
        var routing = DoRoute(text);
        _logger.LogDebug("Routed intent={Intent} workflowId={WorkflowId}", routing.Intent, routing.WorkflowId);
        return Task.FromResult(routing);
    }

    private static Core.RoutingResult DoRoute(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return new Core.RoutingResult { Intent = "generic_task" };

        var lower = text.ToLowerInvariant();

        if (lower.StartsWith("/workflow run ") || lower.StartsWith("/run "))
        {
            var name = text.Split(' ', StringSplitOptions.RemoveEmptyEntries).LastOrDefault() ?? "";
            return new Core.RoutingResult { Intent = "run_named_workflow", WorkflowId = name, Parameters = new Dictionary<string, object?> { ["workflowId"] = name } };
        }
        if (lower == "/workflow list" || lower == "/workflows list")
            return new Core.RoutingResult { Intent = "list_workflows" };
        if (lower == "/skills list" || lower == "/skill list")
            return new Core.RoutingResult { Intent = "list_skills" };
        if (lower.StartsWith("/memory aliases") || lower.Contains("memory aliases"))
            return new Core.RoutingResult { Intent = "list_aliases" };
        if (lower.StartsWith("/memory preference set "))
        {
            var parts = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 4)
                return new Core.RoutingResult { Intent = "save_preference", Parameters = new Dictionary<string, object?> { ["key"] = parts[3], ["value"] = parts.Length > 4 ? parts[4] : "" } };
        }

        if (ContainsAny(lower, "邮件", "mail", "email", "未读", "unread", "摘要", "digest"))
            return new Core.RoutingResult { Intent = "email_digest", WorkflowId = "daily_mail_digest" };
        if (ContainsAny(lower, "读取", "read", "打开文件", "open file"))
            return new Core.RoutingResult { Intent = "file_read" };
        if (ContainsAny(lower, "写入", "write", "保存到文件"))
            return new Core.RoutingResult { Intent = "file_write" };
        if (ContainsAny(lower, "语言", "language", "偏好", "preference"))
            return new Core.RoutingResult { Intent = "save_preference" };
        if (ContainsAny(lower, "打开", "open", "url", "网页"))
        {
            var parameters = new Dictionary<string, object?>();
            TryExtractUrlForOpenWorkflow(text, parameters);
            return new Core.RoutingResult { Intent = "open_url", Parameters = parameters };
        }

        return new Core.RoutingResult { Intent = "generic_task" };
    }

    /// <summary>
    /// Fills <paramref name="parameters"/> with <c>url</c> for <c>open_url_with_human_checkpoint</c>
    /// (extract from text or neutral default). Required because workflow uses <c>{{url}}</c>.
    /// </summary>
    private static void TryExtractUrlForOpenWorkflow(string text, Dictionary<string, object?> parameters)
    {
        var m = Regex.Match(text, @"https?://[^\s""'<>]+", RegexOptions.IgnoreCase);
        if (m.Success)
        {
            parameters["url"] = m.Value;
            return;
        }

        m = Regex.Match(text, @"\bwww\.[^\s""'<>]+", RegexOptions.IgnoreCase);
        if (m.Success)
        {
            var u = m.Value.TrimEnd('。', '.', ',', ')', '）', '，');
            parameters["url"] = u.StartsWith("http", StringComparison.OrdinalIgnoreCase) ? u : "https://" + u;
            return;
        }

        // No URL in natural language — still provide a valid default so {{url}} resolves.
        parameters["url"] = "https://www.example.com";
    }

    private static bool ContainsAny(string text, params string[] keywords)
        => keywords.Any(k => text.Contains(k, StringComparison.OrdinalIgnoreCase));
}
