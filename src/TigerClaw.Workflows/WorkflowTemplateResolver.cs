namespace TigerClaw.Workflows;

/// <summary>
/// Resolves workflow templates from intent.
/// </summary>
public class WorkflowTemplateResolver : Core.IWorkflowTemplateResolver
{
    private static readonly Dictionary<string, string> IntentToWorkflow = new(StringComparer.OrdinalIgnoreCase)
    {
        ["run_named_workflow"] = "{{workflowId}}",
        ["email_digest"] = "daily_mail_digest",
        ["save_preference"] = "save_user_language_pref",
        ["open_url"] = "open_url_with_human_checkpoint",
        ["taobao_search"] = "taobao_search"
    };

    public Task<string?> ResolveWorkflowIdAsync(string intent, IReadOnlyDictionary<string, object?> parameters, CancellationToken cancellationToken = default)
    {
        if (IntentToWorkflow.TryGetValue(intent, out var template))
        {
            var resolved = template;
            if (parameters.TryGetValue("workflowId", out var wf) && wf is string wfs)
                resolved = wfs;
            return Task.FromResult<string?>(resolved);
        }
        return Task.FromResult<string?>(null);
    }
}
