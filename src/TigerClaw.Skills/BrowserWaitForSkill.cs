using Microsoft.Extensions.Logging;
using TigerClaw.Models;

namespace TigerClaw.Skills;

/// <summary>
/// Waits for an element to appear.
/// </summary>
public class BrowserWaitForSkill : Core.ISkill
{
    public string Id => "browser.wait_for";
    private readonly BrowserAutomationSession _session;
    private readonly ILogger<BrowserWaitForSkill> _logger;

    public BrowserWaitForSkill(BrowserAutomationSession session, ILogger<BrowserWaitForSkill> logger)
    {
        _session = session;
        _logger = logger;
    }

    public async Task<SkillExecutionResult> ExecuteAsync(IReadOnlyDictionary<string, object?> inputs, TaskExecutionContext context, CancellationToken cancellationToken = default)
    {
        var selector = inputs.TryGetValue("selector", out var s) ? s?.ToString() : null;
        var timeoutMs = inputs.TryGetValue("timeoutMs", out var tm) && int.TryParse(tm?.ToString(), out var timeout) ? timeout : 15000;
        if (string.IsNullOrWhiteSpace(selector))
            return new SkillExecutionResult { Success = false, Message = "Missing required input: selector" };

        try
        {
            var page = await _session.GetPageAsync(headless: false, cancellationToken);
            await page.WaitForSelectorAsync(selector, new() { Timeout = timeoutMs });
            _logger.LogDebug("browser.wait_for selector={Selector} taskId={TaskId}", selector, context.TaskId);
            return new SkillExecutionResult { Success = true, Output = new { selector, timeoutMs } };
        }
        catch (Exception ex)
        {
            return new SkillExecutionResult { Success = false, Message = ex.Message };
        }
    }
}
